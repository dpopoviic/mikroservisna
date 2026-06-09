using EventPlatformAPI.Messages.IntegrationEvents;
using EventPlatformAPI.ReferencesAPI.Data;
using EventPlatformAPI.ReferencesAPI.Models;
using EventPlatformAPI.ReferencesAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.ReferencesAPI.HostedServices;

public sealed class RegistrationRequestedConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegistrationRequestedConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string InboundQueue = "registration.requested.event";
    private const string ValidatedQueue = "registration.validated.event";
    private const string ValidationFailedQueue = "registration.validation-failed.event";

    public RegistrationRequestedConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<RegistrationRequestedConsumerHostedService> logger)
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

        await _channel.QueueDeclareAsync(InboundQueue, durable: true,
            exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(ValidatedQueue, durable: true,
            exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(ValidationFailedQueue, durable: true,
            exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(InboundQueue, autoAck: false,
            consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("[REFERENCESAPI] RegistrationRequestedConsumer started.");

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (_channel is null) return;

        var messageId = ea.BasicProperties.MessageId ?? string.Empty;
        var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

            var evt = JsonSerializer.Deserialize<RegistrationRequestedEvent>(payload);
            if (evt is null)
            {
                _logger.LogWarning("[REFERENCESAPI] Could not deserialize RegistrationRequestedEvent.");
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                return;
            }

            _logger.LogInformation(
                "[REFERENCESAPI] RegistrationRequestedEvent received. RegistrationId={Id} EventId={EventId} CorrelationId={Cid}",
                evt.RegistrationId, evt.EventId, evt.CorrelationId);

            var snapshot = await db.EventSnapshots
                .FirstOrDefaultAsync(s => s.ExternalEventId == evt.EventId, ct);

            string? failureReason = null;

            if (snapshot is null)
            {
                failureReason = $"Event {evt.EventId} does not exist or has not been published yet.";
            }
            else if (!snapshot.IsPublished)
            {
                failureReason = $"Event {evt.EventId} is not published.";
            }
            else if (snapshot.EventDate <= DateTime.UtcNow)
            {
                failureReason = $"Event {evt.EventId} date has already passed ({snapshot.EventDate:O}).";
            }

            if (failureReason is not null)
            {
                _logger.LogWarning("[REFERENCESAPI] Validation failed. Reason: {Reason}", failureReason);
                await EnqueueAsync(outboxRepo, ValidationFailedQueue, new RegistrationValidationFailedEvent
                {
                    CorrelationId = evt.CorrelationId,
                    OccurredAt = DateTime.UtcNow,
                    RegistrationId = evt.RegistrationId,
                    EventId = evt.EventId,
                    FailureReason = failureReason
                }, ct);
            }
            else
            {
                _logger.LogInformation("[REFERENCESAPI] Validation passed for RegistrationId={Id}.", evt.RegistrationId);
                await EnqueueAsync(outboxRepo, ValidatedQueue, new RegistrationValidatedEvent
                {
                    CorrelationId = evt.CorrelationId,
                    OccurredAt = DateTime.UtcNow,
                    RegistrationId = evt.RegistrationId,
                    EventId = evt.EventId
                }, ct);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REFERENCESAPI] Failed to process RegistrationRequestedEvent. MessageId={MessageId}", messageId);
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
        }
    }

    private static async Task EnqueueAsync<T>(IOutboxRepository outbox, string destination, T payload, CancellationToken ct)
    {
        await outbox.AddAsync(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Destination = destination,
            Type = typeof(T).Name,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow,
            IsPublished = false
        }, ct);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
