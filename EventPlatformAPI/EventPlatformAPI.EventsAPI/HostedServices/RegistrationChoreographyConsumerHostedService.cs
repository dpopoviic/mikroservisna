using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using EventPlatformAPI.EventsAPI.Services;
using EventPlatformAPI.Messages.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.EventsAPI.HostedServices;

public sealed class RegistrationChoreographyConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegistrationChoreographyConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string RegistrationValidatedQueue = "registration.validated.event";
    private const string RegistrationValidationFailedQueue = "registration.validation-failed.event";
    private const string SeatReservedReplyQueue = "registration.seat-reserved.event";
    private const string CapacityExceededQueue = "registration.capacity-exceeded.event";
    private const string RegistrationEmailSentQueue = "registration.email-sent.event";
    private const string RegistrationEmailFailedQueue = "registration.email-failed.event";

    public RegistrationChoreographyConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<RegistrationChoreographyConsumerHostedService> logger)
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

        foreach (var queue in new[]
        {
            RegistrationValidatedQueue,
            RegistrationValidationFailedQueue,
            CapacityExceededQueue,
            RegistrationEmailSentQueue,
            RegistrationEmailFailedQueue
        })
        {
            await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false,
                autoDelete: false, cancellationToken: stoppingToken);
        }

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);

        foreach (var queue in new[]
        {
            RegistrationValidatedQueue,
            RegistrationValidationFailedQueue,
            CapacityExceededQueue,
            RegistrationEmailSentQueue,
            RegistrationEmailFailedQueue
        })
        {
            await _channel.BasicConsumeAsync(queue, autoAck: false, consumer: consumer,
                cancellationToken: stoppingToken);
        }

        _logger.LogInformation("[EVENTSAPI] Registration choreography consumer started.");

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (_channel is null) return;

        var messageId = ea.BasicProperties.MessageId ?? string.Empty;
        var messageType = ea.BasicProperties.Type ?? string.Empty;
        var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            // Idempotency check
            if (!string.IsNullOrEmpty(messageId) &&
                await db.ProcessedMessages.AnyAsync(x => x.EventId == messageId, ct))
            {
                _logger.LogInformation("[EVENTSAPI] Choreography message {MessageId} already processed. Acking.", messageId);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                return;
            }

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            switch (messageType)
            {
                case nameof(RegistrationValidatedEvent):
                    await HandleRegistrationValidatedAsync(db, payload, ct);
                    break;

                case nameof(RegistrationValidationFailedEvent):
                    await HandleRegistrationValidationFailedAsync(db, payload, ct);
                    break;

                case nameof(CapacityExceededEvent):
                    await HandleCapacityExceededAsync(db, payload, ct);
                    break;

                case nameof(RegistrationEmailSentEvent):
                    await HandleRegistrationEmailSentAsync(db, payload, ct);
                    break;

                case nameof(RegistrationEmailFailedEvent):
                    await HandleRegistrationEmailFailedAsync(db, payload, ct);
                    break;

                default:
                    _logger.LogWarning("[EVENTSAPI] Unknown choreography message type: {Type}", messageType);
                    break;
            }

            if (!string.IsNullOrEmpty(messageId))
            {
                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    EventId = messageId,
                    EventType = messageType,
                    ProcessedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EVENTSAPI] Failed to process choreography message {Type} MessageId={MessageId}",
                messageType, messageId);
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
        }
    }

    private async Task HandleRegistrationValidatedAsync(EventsDbContext db, string payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<RegistrationValidatedEvent>(payload);
        if (evt is null) return;

        _logger.LogInformation("[EVENTSAPI] RegistrationValidatedEvent. RegistrationId={Id} CorrelationId={Cid}",
            evt.RegistrationId, evt.CorrelationId);

        LogSagaEvent(db, evt.CorrelationId, nameof(RegistrationValidatedEvent), payload);

        var registration = await db.Registrations.FindAsync(new object[] { evt.RegistrationId }, ct);
        if (registration is null)
        {
            _logger.LogWarning("[EVENTSAPI] Registration {Id} not found.", evt.RegistrationId);
            return;
        }

        var @event = await db.Events
            .Include(e => e.Type)
            .FirstOrDefaultAsync(e => e.Id == evt.EventId, ct);

        if (@event is null)
        {
            _logger.LogWarning("[EVENTSAPI] Event {EventId} not found during seat check.", evt.EventId);
            return;
        }

        var locationSnapshot = await db.LocationSnapshots
            .FirstOrDefaultAsync(l => l.ExternalId == @event.LocationId, ct);

        var capacity = locationSnapshot?.Capacity ?? 0;

        var confirmedCount = await db.Registrations
            .CountAsync(r =>
                r.EventId == evt.EventId &&
                r.Id != registration.Id &&
                (r.Status == RegistrationStatus.SeatReserved || r.Status == RegistrationStatus.Completed),
                ct);

        var sagaState = await db.RegistrationSagaStates
            .FirstOrDefaultAsync(s => s.CorrelationId == evt.CorrelationId, ct);

        if (confirmedCount < capacity)
        {
            registration.Status = RegistrationStatus.SeatReserved;

            if (sagaState is not null)
                sagaState.CurrentState = "SeatReserved";

            var seatReserved = new SeatReservedEvent
            {
                CorrelationId = evt.CorrelationId,
                OccurredAt = DateTime.UtcNow,
                RegistrationId = evt.RegistrationId,
                EventId = evt.EventId
            };
            EnqueueOutbox(db, SeatReservedReplyQueue, seatReserved);

            _logger.LogInformation("[EVENTSAPI] Seat reserved for Registration {Id}.", evt.RegistrationId);
        }
        else
        {
            registration.Status = RegistrationStatus.Failed;

            if (sagaState is not null)
            {
                sagaState.CurrentState = "Failed";
                sagaState.FailureReason = $"No available capacity. Used {confirmedCount}/{capacity}.";
                sagaState.CompletedAt = DateTime.UtcNow;
            }

            var capacityExceeded = new CapacityExceededEvent
            {
                CorrelationId = evt.CorrelationId,
                OccurredAt = DateTime.UtcNow,
                RegistrationId = evt.RegistrationId,
                EventId = evt.EventId,
                FailureReason = $"No available capacity. Used {confirmedCount}/{capacity}."
            };
            EnqueueOutbox(db, "registration.capacity-exceeded.event", capacityExceeded);

            _logger.LogWarning("[EVENTSAPI] Capacity exceeded for Registration {Id}.", evt.RegistrationId);
        }
    }

    private async Task HandleRegistrationValidationFailedAsync(EventsDbContext db, string payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<RegistrationValidationFailedEvent>(payload);
        if (evt is null) return;

        _logger.LogWarning("[EVENTSAPI] RegistrationValidationFailedEvent. RegistrationId={Id} Reason={Reason}",
            evt.RegistrationId, evt.FailureReason);

        LogSagaEvent(db, evt.CorrelationId, nameof(RegistrationValidationFailedEvent), payload);

        var registration = await db.Registrations.FindAsync(new object[] { evt.RegistrationId }, ct);
        if (registration is not null) registration.Status = RegistrationStatus.Failed;

        var sagaState = await db.RegistrationSagaStates
            .FirstOrDefaultAsync(s => s.CorrelationId == evt.CorrelationId, ct);
        if (sagaState is not null)
        {
            sagaState.CurrentState = "Failed";
            sagaState.FailureReason = evt.FailureReason;
            sagaState.CompletedAt = DateTime.UtcNow;
        }
    }

    private async Task HandleCapacityExceededAsync(EventsDbContext db, string payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<CapacityExceededEvent>(payload);
        if (evt is null) return;

        _logger.LogWarning("[EVENTSAPI] CapacityExceededEvent received. RegistrationId={Id}", evt.RegistrationId);
        LogSagaEvent(db, evt.CorrelationId, nameof(CapacityExceededEvent), payload);

    }

    private async Task HandleRegistrationEmailSentAsync(EventsDbContext db, string payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<RegistrationEmailSentEvent>(payload);
        if (evt is null) return;

        _logger.LogInformation("[EVENTSAPI] RegistrationEmailSentEvent. RegistrationId={Id}", evt.RegistrationId);
        LogSagaEvent(db, evt.CorrelationId, nameof(RegistrationEmailSentEvent), payload);

        var registration = await db.Registrations.FindAsync(new object[] { evt.RegistrationId }, ct);
        if (registration is not null) registration.Status = RegistrationStatus.Completed;

        var sagaState = await db.RegistrationSagaStates
            .FirstOrDefaultAsync(s => s.CorrelationId == evt.CorrelationId, ct);
        if (sagaState is not null)
        {
            sagaState.CurrentState = "Completed";
            sagaState.CompletedAt = DateTime.UtcNow;
        }

        _logger.LogInformation("[EVENTSAPI] Registration {Id} completed successfully.", evt.RegistrationId);
    }

    private async Task HandleRegistrationEmailFailedAsync(EventsDbContext db, string payload, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<RegistrationEmailFailedEvent>(payload);
        if (evt is null) return;

        _logger.LogWarning("[EVENTSAPI] RegistrationEmailFailedEvent. RegistrationId={Id} Reason={Reason}",
            evt.RegistrationId, evt.FailureReason);

        LogSagaEvent(db, evt.CorrelationId, nameof(RegistrationEmailFailedEvent), payload);

        var registration = await db.Registrations.FindAsync(new object[] { evt.RegistrationId }, ct);
        if (registration is not null) registration.Status = RegistrationStatus.Cancelled;

        var sagaState = await db.RegistrationSagaStates
            .FirstOrDefaultAsync(s => s.CorrelationId == evt.CorrelationId, ct);
        if (sagaState is not null)
        {
            sagaState.CurrentState = "Cancelled";
            sagaState.FailureReason = evt.FailureReason;
            sagaState.CompletedAt = DateTime.UtcNow;
        }

        // Compensation: publish RegistrationCancelledEvent so downstream can react
        var cancelledEvent = new RegistrationCancelledEvent
        {
            CorrelationId = evt.CorrelationId,
            OccurredAt = DateTime.UtcNow,
            RegistrationId = evt.RegistrationId,
            EventId = evt.EventId,
            Reason = evt.FailureReason
        };
        EnqueueOutbox(db, "registration.cancelled.event", cancelledEvent);

        _logger.LogInformation("[EVENTSAPI] Compensation published RegistrationCancelledEvent for Registration {Id}.",
            evt.RegistrationId);
    }

    private static void EnqueueOutbox<T>(EventsDbContext db, string destination, T payload)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Destination = destination,
            Type = typeof(T).Name,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAtUtc = DateTime.UtcNow,
            IsPublished = false
        });
    }

    private static void LogSagaEvent(EventsDbContext db, Guid correlationId, string eventName, string payload)
    {
        db.SagaEventLogs.Add(new SagaEventLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            EventName = eventName,
            Payload = payload,
            CreatedAt = DateTime.UtcNow
        });
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
