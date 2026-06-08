using System.Text;
using System.Text.Json;
using EventPlatformAPI.Messages.Commands;
using EventPlatformAPI.Messages.IntegrationEvents;
using EventPlatformAPI.SagaOrcgestrator.Data;
using EventPlatformAPI.SagaOrcgestrator.Models;
using EventPlatformAPI.SagaOrcgestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventPlatformAPI.SagaOrcgestrator.HostedServices;

public class PublishEventSagaOrchestratorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PublishEventSagaOrchestratorHostedService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    public PublishEventSagaOrchestratorHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<PublishEventSagaOrchestratorHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        var queues = new[]
        {
            _options.StartEventPublicationSagaCommandQueue,
            _options.LocationValidatedEventQueue,
            _options.LocationValidationFailedEventQueue,
            _options.LecturersValidatedEventQueue,
            _options.LecturersValidationFailedEventQueue,
            _options.EventCreatedEventQueue,
            _options.EventCreationFailedEventQueue,
            _options.EmailSentEventQueue,
            _options.EmailFailedEventQueue,
            _options.EventCancelledEventQueue
        };

        foreach (var queue in queues)
        {
            await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        }

        await _channel.BasicQosAsync(0, _options.PrefetchCount, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, ea) => HandleMessageAsync(ea, stoppingToken);

        foreach (var queue in queues)
        {
            await _channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        }

        _logger.LogInformation("Publish event saga orchestrator started.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        var messageId = ea.BasicProperties.MessageId;
        if (string.IsNullOrWhiteSpace(messageId))
        {
            await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
            return;
        }

        var messageType = ea.BasicProperties.Type ?? string.Empty;
        var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SagaOrchestratorDbContext>();

            if (await db.ProcessedMessages.AnyAsync(x => x.EventId == messageId, cancellationToken))
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
                return;
            }

            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

            switch (messageType)
            {
                case nameof(StartEventPublicationSagaCommand):
                    await HandleStartEventPublicationSagaCommandAsync(db, payload, cancellationToken);
                    break;
                case nameof(LocationValidatedEvent):
                    await HandleLocationValidatedAsync(db, payload, cancellationToken);
                    break;
                case nameof(LocationValidationFailedEvent):
                    await HandleLocationValidationFailedAsync(db, payload, cancellationToken);
                    break;
                case nameof(LecturersValidatedEvent):
                    await HandleLecturersValidatedAsync(db, payload, cancellationToken);
                    break;
                case nameof(LecturersValidationFailedEvent):
                    await HandleLecturersValidationFailedAsync(db, payload, cancellationToken);
                    break;
                case nameof(EventCreatedEvent):
                    await HandleEventCreatedAsync(db, payload, cancellationToken);
                    break;
                case nameof(EventCreationFailedEvent):
                    await HandleEventCreationFailedAsync(db, payload, cancellationToken);
                    break;
                case nameof(EmailSentEvent):
                    await HandleEmailSentAsync(db, payload, cancellationToken);
                    break;
                case nameof(EmailFailedEvent):
                    await HandleEmailFailedAsync(db, payload, cancellationToken);
                    break;
                case nameof(EventCancelledEvent):
                    await HandleEventCancelledAsync(db, payload, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unknown message type {MessageType}", messageType);
                    break;
            }

            db.ProcessedMessages.Add(new ProcessedMessage
            {
                EventId = messageId,
                EventType = messageType,
                ProcessedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageType} with MessageId {MessageId}", messageType, messageId);
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false, cancellationToken);
        }
    }

    private async Task HandleStartEventPublicationSagaCommandAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var command = JsonSerializer.Deserialize<StartEventPublicationSagaCommand>(payload);
        if (command is null)
        {
            return;
        }

        var existing = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == command.CorrelationId, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var saga = new PublishEventSaga
        {
            CorrelationId = command.CorrelationId,
            Status = PublishEventSagaStatus.Started,
            CreatedAt = DateTime.UtcNow,
            PayloadJson = payload
        };

        db.PublishEventSagas.Add(saga);

        EnqueueCommand(db, _options.ValidateLocationCommandQueue, new ValidateLocationCommand
        {
            CorrelationId = command.CorrelationId,
            LocationId = command.LocationId
        });
    }

    private async Task HandleLocationValidatedAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<LocationValidatedEvent>(payload);
        if (evt is null)
        {
            return;
        }

        var saga = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == evt.CorrelationId, cancellationToken);
        if (saga is null || saga.Status != PublishEventSagaStatus.Started)
        {
            return;
        }

        Transition(saga, PublishEventSagaStatus.LocationValidated);

        var publishEvent = JsonSerializer.Deserialize<StartEventPublicationSagaCommand>(saga.PayloadJson);
        if (publishEvent is null)
        {
            return;
        }

        EnqueueCommand(db, _options.ValidateLecturersCommandQueue, new ValidateLecturersCommand
        {
            CorrelationId = saga.CorrelationId,
            LecturerIds = publishEvent.LecturerIds
        });
    }

    private async Task HandleLocationValidationFailedAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<LocationValidationFailedEvent>(payload);
        if (evt is null)
        {
            return;
        }

        var saga = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == evt.CorrelationId, cancellationToken);
        if (saga is null || saga.Status is PublishEventSagaStatus.Failed or PublishEventSagaStatus.Compensated or PublishEventSagaStatus.Completed)
        {
            return;
        }

        Transition(saga, PublishEventSagaStatus.Failed, evt.Reason, completed: true);
    }

    private async Task HandleLecturersValidatedAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<LecturersValidatedEvent>(payload);
        if (evt is null)
        {
            return;
        }

        var saga = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == evt.CorrelationId, cancellationToken);
        if (saga is null || saga.Status != PublishEventSagaStatus.LocationValidated)
        {
            return;
        }

        Transition(saga, PublishEventSagaStatus.LecturersValidated);

        var publishEvent = JsonSerializer.Deserialize<StartEventPublicationSagaCommand>(saga.PayloadJson);
        if (publishEvent is null)
        {
            return;
        }

        EnqueueCommand(db, _options.CreateEventCommandQueue, new CreateEventCommand
        {
            CorrelationId = saga.CorrelationId,
            Name = publishEvent.Name,
            Agenda = publishEvent.Agenda,
            DateTime = publishEvent.DateTime,
            DurationInHours = publishEvent.DurationInHours,
            Price = publishEvent.Price,
            TypeId = publishEvent.TypeId,
            LocationId = publishEvent.LocationId,
            LecturerIds = publishEvent.LecturerIds,
            OrganizerEmail = publishEvent.OrganizerEmail
        });
    }

    private async Task HandleLecturersValidationFailedAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<LecturersValidationFailedEvent>(payload);
        if (evt is null)
        {
            return;
        }

        var saga = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == evt.CorrelationId, cancellationToken);
        if (saga is null || saga.Status is PublishEventSagaStatus.Failed or PublishEventSagaStatus.Compensated or PublishEventSagaStatus.Completed)
        {
            return;
        }

        Transition(saga, PublishEventSagaStatus.Failed, evt.Reason, completed: true);
    }

    private async Task HandleEventCreatedAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<EventCreatedEvent>(payload);
        if (evt is null)
        {
            return;
        }

        var saga = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == evt.CorrelationId, cancellationToken);
        if (saga is null || saga.Status != PublishEventSagaStatus.LecturersValidated)
        {
            return;
        }

        saga.EventId = evt.EventId;
        Transition(saga, PublishEventSagaStatus.EventCreated);

        var publishEvent = JsonSerializer.Deserialize<StartEventPublicationSagaCommand>(saga.PayloadJson);
        if (publishEvent is null)
        {
            return;
        }

        EnqueueCommand(db, _options.SendEventNotificationCommandQueue, new SendEventNotificationCommand
        {
            CorrelationId = saga.CorrelationId,
            EventId = evt.EventId,
            To = string.IsNullOrWhiteSpace(publishEvent.OrganizerEmail) ? "org@example.com" : publishEvent.OrganizerEmail,
            Subject = $"Event published: {publishEvent.Name}",
            Body = $"Event {publishEvent.Name} is published for {publishEvent.DateTime:dd.MM.yyyy HH:mm}."
        });
    }

    private async Task HandleEventCreationFailedAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<EventCreationFailedEvent>(payload);
        if (evt is null)
        {
            return;
        }

        var saga = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == evt.CorrelationId, cancellationToken);
        if (saga is null || saga.Status is PublishEventSagaStatus.Failed or PublishEventSagaStatus.Compensated or PublishEventSagaStatus.Completed)
        {
            return;
        }

        Transition(saga, PublishEventSagaStatus.Failed, evt.Reason, completed: true);
    }

    private async Task HandleEmailSentAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<EmailSentEvent>(payload);
        if (evt is null)
        {
            return;
        }

        var saga = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == evt.CorrelationId, cancellationToken);
        if (saga is null || saga.Status != PublishEventSagaStatus.EventCreated)
        {
            return;
        }

        Transition(saga, PublishEventSagaStatus.EmailSent);
        Transition(saga, PublishEventSagaStatus.Completed, completed: true);
    }

    private async Task HandleEmailFailedAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<EmailFailedEvent>(payload);
        if (evt is null)
        {
            return;
        }

        var saga = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == evt.CorrelationId, cancellationToken);
        if (saga is null || saga.Status is PublishEventSagaStatus.Compensated or PublishEventSagaStatus.Completed)
        {
            return;
        }

        if (saga.Status != PublishEventSagaStatus.EventCreated)
        {
            Transition(saga, PublishEventSagaStatus.Failed, evt.Reason, completed: true);
            return;
        }

        Transition(saga, PublishEventSagaStatus.Compensating, evt.Reason);

        _logger.LogWarning("Compensation triggered for saga {CorrelationId}. EventId={EventId}", saga.CorrelationId, saga.EventId);

        EnqueueCommand(db, _options.CancelEventCommandQueue, new CancelEventCommand
        {
            CorrelationId = saga.CorrelationId,
            EventId = saga.EventId ?? evt.EventId,
            Reason = evt.Reason
        });
    }

    private async Task HandleEventCancelledAsync(SagaOrchestratorDbContext db, string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<EventCancelledEvent>(payload);
        if (evt is null)
        {
            return;
        }

        var saga = await db.PublishEventSagas.FirstOrDefaultAsync(x => x.CorrelationId == evt.CorrelationId, cancellationToken);
        if (saga is null || saga.Status != PublishEventSagaStatus.Compensating)
        {
            return;
        }

        Transition(saga, PublishEventSagaStatus.Compensated, completed: true);
    }

    private void Transition(PublishEventSaga saga, string nextStatus, string? reason = null, bool completed = false)
    {
        _logger.LogInformation("Saga {CorrelationId} transition: {FromStatus} -> {ToStatus}", saga.CorrelationId, saga.Status, nextStatus);
        saga.Status = nextStatus;
        saga.FailureReason = reason ?? saga.FailureReason;
        if (completed)
        {
            saga.CompletedAt = DateTime.UtcNow;
        }
    }

    private void EnqueueCommand<T>(SagaOrchestratorDbContext db, string destination, T command)
    {
        var payload = JsonSerializer.Serialize(command);
        db.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Destination = destination,
            Type = typeof(T).Name,
            Payload = payload,
            CreatedAtUtc = DateTime.UtcNow,
            IsPublished = false
        });

        _logger.LogInformation("Command queued in outbox: {CommandType} -> {Destination}", typeof(T).Name, destination);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}