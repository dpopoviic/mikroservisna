using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using EventPlatformAPI.EventsAPI.Services;
using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.EventsApiMessages;
using EventPlatformAPI.Messages.Saga.SagaMessages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.EventsAPI.HostedServices
{
    public sealed class EventsSagaCommandConsumerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EventsSagaCommandConsumerHostedService> _logger;
        private readonly RabbitMqOptions _options;

        private IConnection? _connection;
        private IChannel? _channel;

        private static readonly string[] CommandQueues =
        [
            SagaQueues.ReserveEventSeat,
            SagaQueues.ReleaseEventSeat
        ];

        public EventsSagaCommandConsumerHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<RabbitMqOptions> options,
            ILogger<EventsSagaCommandConsumerHostedService> logger)
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

            foreach (var queue in CommandQueues)
            {
                await _channel.QueueDeclareAsync(
                    queue: queue, durable: true, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (_, ea) => await HandleAsync(ea, stoppingToken);

                await _channel.BasicConsumeAsync(
                    queue: queue, autoAck: false, consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("EventsAPI saga consumer listening on queue: {Queue}", queue);
            }

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }

        private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken ct)
        {
            if (_channel is null) return;

            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var queue = ea.RoutingKey;

            _logger.LogInformation("[EventsAPI] Received saga command from queue: {Queue}", queue);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

                if (queue == SagaQueues.ReserveEventSeat)
                    await HandleReserveAsync(body, db, ct);
                else if (queue == SagaQueues.ReleaseEventSeat)
                    await HandleReleaseAsync(body, db, ct);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EventsAPI] Error processing saga command from queue {Queue}", queue);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
            }
        }

        private async Task HandleReserveAsync(string body, EventsDbContext db, CancellationToken ct)
        {
            var cmd = Deserialize<ReserveEventSeatCommand>(body);

            _logger.LogInformation(
                "[CorrelationId={CorrelationId}] EventsAPI: ReserveEventSeatCommand. EventId={EventId}",
                cmd.CorrelationId, cmd.EventId);

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            try
            {
                var evt = await db.Events.FirstOrDefaultAsync(e => e.Id == cmd.EventId.GetHashCode(), ct)
                       ?? await db.Events.FirstOrDefaultAsync(e => true, ct); 

                var eventEntity = await db.Events
                    .FirstOrDefaultAsync(e => e.Id == cmd.EventId, ct);

                if (eventEntity is null)
                {
                    _logger.LogWarning(
                        "[CorrelationId={CorrelationId}] Event {EventId} not found.",
                        cmd.CorrelationId, cmd.EventId);

                    await EnqueueOutboxAsync(db, cmd.CorrelationId,
                        SagaQueues.EventSeatReservationFailed,
                        new EventSeatReservationFailedEvent
                        {
                            CorrelationId = cmd.CorrelationId,
                            EventId = cmd.EventId,
                            Reason = $"Event {cmd.EventId} not found.",
                            Timestamp = DateTime.UtcNow
                        }, ct);

                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return;
                }

                if (eventEntity.AvailableSeats <= 0)
                {
                    _logger.LogWarning(
                        "[CorrelationId={CorrelationId}] Event {EventId} has no available seats.",
                        cmd.CorrelationId, cmd.EventId);

                    await EnqueueOutboxAsync(db, cmd.CorrelationId,
                        SagaQueues.EventSeatReservationFailed,
                        new EventSeatReservationFailedEvent
                        {
                            CorrelationId = cmd.CorrelationId,
                            EventId = cmd.EventId,
                            Reason = "No available seats.",
                            Timestamp = DateTime.UtcNow
                        }, ct);

                    await db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                    return;
                }

                eventEntity.AvailableSeats--;
                db.Events.Update(eventEntity);

                await EnqueueOutboxAsync(db, cmd.CorrelationId,
                    SagaQueues.EventSeatReserved,
                    new EventSeatReservedEvent
                    {
                        CorrelationId = cmd.CorrelationId,
                        EventId = cmd.EventId,
                        Timestamp = DateTime.UtcNow
                    }, ct);

                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] Seat reserved for Event {EventId}. Remaining: {Seats}",
                    cmd.CorrelationId, cmd.EventId, eventEntity.AvailableSeats);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);

                _logger.LogError(ex,
                    "[CorrelationId={CorrelationId}] Unexpected error reserving seat.",
                    cmd.CorrelationId);

                await using var tx2 = await db.Database.BeginTransactionAsync(ct); //**
                await EnqueueOutboxAsync(db, cmd.CorrelationId,
                    SagaQueues.EventSeatReservationFailed,
                    new EventSeatReservationFailedEvent
                    {
                        CorrelationId = cmd.CorrelationId,
                        EventId = cmd.EventId,
                        Reason = ex.Message,
                        Timestamp = DateTime.UtcNow
                    }, ct);
                await db.SaveChangesAsync(ct);
                await tx2.CommitAsync(ct);
            }
        }

        private async Task HandleReleaseAsync(string body, EventsDbContext db, CancellationToken ct)
        {
            var cmd = Deserialize<ReleaseEventSeatCommand>(body);

            _logger.LogInformation(
                "[CorrelationId={CorrelationId}] EventsAPI: ReleaseEventSeatCommand. EventId={EventId}",
                cmd.CorrelationId, cmd.EventId);

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var eventEntity = await db.Events
                .FirstOrDefaultAsync(e => e.Id == cmd.EventId, ct);

            if (eventEntity is not null)
            {
                eventEntity.AvailableSeats++;
                db.Events.Update(eventEntity);

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] Seat released for Event {EventId}. Available: {Seats}",
                    cmd.CorrelationId, cmd.EventId, eventEntity.AvailableSeats);
            }
            else
            {
                _logger.LogWarning(
                    "[CorrelationId={CorrelationId}] ReleaseEventSeat: Event {EventId} not found – ignoring.",
                    cmd.CorrelationId, cmd.EventId);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
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
}
