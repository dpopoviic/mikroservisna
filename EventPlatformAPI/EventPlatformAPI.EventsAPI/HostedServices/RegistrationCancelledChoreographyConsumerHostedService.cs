using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using EventPlatformAPI.EventsAPI.Services;
using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.Choreography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.EventsAPI.HostedServices;

public sealed class RegistrationCancelledChoreographyConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegistrationCancelledChoreographyConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    private static readonly string[] EventQueues =
    [
        SagaQueues.ChoreographyRegistrationCancelled
    ];

    public RegistrationCancelledChoreographyConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<RegistrationCancelledChoreographyConsumerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
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

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        foreach (var queue in EventQueues)
        {
            await _channel.QueueDeclareAsync(
                queue: queue, durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                queue: queue, autoAck: false, consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("EventsAPI choreography consumer listening on queue: {Queue}", queue);
        }

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
    }

    private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (_channel is null) return;

        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
        var queue = ea.RoutingKey;

        _logger.LogInformation("[EventsAPI] Received choreography event from queue: {Queue}", queue);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            if (queue == SagaQueues.ChoreographyRegistrationCancelled)
                await HandleRegistrationCancelledAsync(body, db, ct);

            await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EventsAPI] Error processing choreography event from queue {Queue}", queue);
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
        }
    }

    private async Task HandleRegistrationCancelledAsync(string body, EventsDbContext db, CancellationToken ct)
    {
        var evt = Deserialize<RegistrationCancelledChoreographyEvent>(body);

        _logger.LogInformation(
            "[CorrelationId={CorrelationId}] EventsAPI: RegistrationCancelledChoreographyEvent received. EventId={EventId}",
            evt.CorrelationId, evt.EventId);

        var idempotencyKey = $"{evt.CorrelationId}:{nameof(RegistrationCancelledChoreographyEvent)}";
        var alreadyProcessed = await db.ProcessedMessages
            .AnyAsync(m => m.EventId == idempotencyKey, ct);

        if (alreadyProcessed)
        {
            _logger.LogWarning(
                "[CorrelationId={CorrelationId}] RegistrationCancelled already processed (EventId={EventId}), skipping.",
                evt.CorrelationId, evt.EventId);
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var eventEntity = await db.Events
                .FirstOrDefaultAsync(e => e.Id == evt.EventId, ct);

            if (eventEntity is null)
            {
                _logger.LogWarning(
                    "[CorrelationId={CorrelationId}] Event {EventId} not found.",
                    evt.CorrelationId, evt.EventId);

                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    EventId = idempotencyKey,
                    EventType = nameof(RegistrationCancelledChoreographyEvent),
                    ProcessedAtUtc = DateTime.UtcNow
                });

                await EnqueueOutboxAsync(db, evt.CorrelationId,
                    SagaQueues.ChoreographyEventSeatReleaseFailed,
                    new EventSeatReleaseFailedEvent
                    {
                        CorrelationId = evt.CorrelationId,
                        RegistrationId = evt.RegistrationId,
                        EventId = evt.EventId,
                        Reason = $"Event {evt.EventId} not found.",
                        Timestamp = DateTime.UtcNow
                    }, ct);

                db.ChoreographyProcessStates.Add(new ChoreographyProcessState
                {
                    CorrelationId = evt.CorrelationId,
                    EventName = nameof(RegistrationCancelledChoreographyEvent),
                    ServiceName = "EventsAPI",
                    Status = "Failed",
                    CreatedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return;
            }

            eventEntity.AvailableSeats++;
            db.Events.Update(eventEntity);

            await EnqueueOutboxAsync(db, evt.CorrelationId,
                SagaQueues.ChoreographyEventSeatReleased,
                new EventSeatReleasedEvent
                {
                    CorrelationId = evt.CorrelationId,
                    RegistrationId = evt.RegistrationId,
                    EventId = evt.EventId,
                    Timestamp = DateTime.UtcNow
                }, ct);

            db.ProcessedMessages.Add(new ProcessedMessage
            {
                EventId = idempotencyKey,
                EventType = nameof(RegistrationCancelledChoreographyEvent),
                ProcessedAtUtc = DateTime.UtcNow
            });

            db.ChoreographyProcessStates.Add(new ChoreographyProcessState
            {
                CorrelationId = evt.CorrelationId,
                EventName = nameof(RegistrationCancelledChoreographyEvent),
                ServiceName = "EventsAPI",
                Status = "Processed",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation(
                "[CorrelationId={CorrelationId}] Seat released for Event {EventId}. Available: {Seats}",
                evt.CorrelationId, evt.EventId, eventEntity.AvailableSeats);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);

            _logger.LogError(ex,
                "[CorrelationId={CorrelationId}] Unexpected error releasing seat for Event {EventId}.",
                evt.CorrelationId, evt.EventId);

            await using var tx2 = await db.Database.BeginTransactionAsync(ct);
            await EnqueueOutboxAsync(db, evt.CorrelationId,
                SagaQueues.ChoreographyEventSeatReleaseFailed,
                new EventSeatReleaseFailedEvent
                {
                    CorrelationId = evt.CorrelationId,
                    RegistrationId = evt.RegistrationId,
                    EventId = evt.EventId,
                    Reason = ex.Message,
                    Timestamp = DateTime.UtcNow
                }, ct);

            db.ProcessedMessages.Add(new ProcessedMessage
            {
                EventId = idempotencyKey,
                EventType = nameof(RegistrationCancelledChoreographyEvent),
                ProcessedAtUtc = DateTime.UtcNow
            });

            db.ChoreographyProcessStates.Add(new ChoreographyProcessState
            {
                CorrelationId = evt.CorrelationId,
                EventName = nameof(RegistrationCancelledChoreographyEvent),
                ServiceName = "EventsAPI",
                Status = "Failed",
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync(ct);
            await tx2.CommitAsync(ct);
        }
    }

    private static async Task EnqueueOutboxAsync<TMsg>(
        EventsDbContext db, Guid correlationId,
        string destination, TMsg message, CancellationToken ct) where TMsg : notnull
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            MessageId = Guid.NewGuid(),
            Destination = destination,
            Type = typeof(TMsg).Name,
            Payload = JsonSerializer.Serialize(message),
            CorrelationId = correlationId,
            CreatedAtUtc = DateTime.UtcNow,
            IsPublished = false
        });

        await db.SaveChangesAsync(ct);
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json)
           ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}.");

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
