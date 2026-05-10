using System.Text;
using System.Text.Json;
using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using EventPlatformAPI.Messages.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventPlatformAPI.EventsAPI.Services;

public sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqConsumerHostedService(IServiceScopeFactory scopeFactory, IOptions<RabbitMqOptions> options, ILogger<RabbitMqConsumerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting RabbitMQ consumer for queue {Queue}", _options.Queue);

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            exchange: _options.Exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _options.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(
                    queue: _options.Queue,
                    exchange: _options.Exchange,
                    routingKey: _options.RoutingKey,
                    cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: _options.PrefetchCount, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);


        await _channel.BasicConsumeAsync(
            queue: _options.Queue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Consumer listening on queue {Queue}", _options.Queue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) { }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            var eventType = ea.BasicProperties.Type ?? string.Empty;

            // Parse message id
            var messageId = ea.BasicProperties.MessageId ?? string.Empty;

            if (string.IsNullOrWhiteSpace(messageId))
            {
                _logger.LogWarning("Received message without MessageId. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            var alreadyProcessed = await db.ProcessedMessages.AnyAsync(x => x.EventId == messageId, cancellationToken);
            if (alreadyProcessed)
            {
                _logger.LogInformation("Message {MessageId} already processed, acking.", messageId);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            if (eventType == nameof(LocationCreatedEvent) || eventType == nameof(LocationUpdatedEvent) || eventType == nameof(LocationDeletedEvent))
            {
                using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                if (eventType == nameof(LocationDeletedEvent))
                {
                    var evt = JsonSerializer.Deserialize<LocationDeletedEvent>(body);
                    if (evt is not null)
                    {
                        var existing = await db.LocationSnapshots.FirstOrDefaultAsync(x => x.ExternalId == evt.LocationId, cancellationToken);
                        if (existing is not null)
                        {
                            db.LocationSnapshots.Remove(existing);
                        }
                    }
                }
                else if (eventType == nameof(LocationCreatedEvent) || eventType == nameof(LocationUpdatedEvent))
                {
                    var evt = JsonSerializer.Deserialize<LocationCreatedEvent>(body);
                    if (evt is not null)
                    {
                        var existing = await db.LocationSnapshots.FirstOrDefaultAsync(x => x.ExternalId == evt.LocationId, cancellationToken);
                        if (existing is null)
                        {
                            db.LocationSnapshots.Add(new LocationSnapshot
                            {
                                ExternalId = evt.LocationId,
                                Name = evt.Name,
                                Address = evt.Address,
                                Capacity = evt.Capacity,
                                UpdatedAtUtc = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            existing.Name = evt.Name;
                            existing.Address = evt.Address;
                            existing.Capacity = evt.Capacity;
                            existing.UpdatedAtUtc = DateTime.UtcNow;
                        }
                    }
                }

                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    EventId = messageId,
                    EventType = eventType,
                    ProcessedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            if (eventType == nameof(LecturerCreatedEvent) || eventType == nameof(LecturerUpdatedEvent) || eventType == nameof(LecturerDeletedEvent))
            {
                using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

                if (eventType == nameof(LecturerDeletedEvent))
                {
                    var evt = JsonSerializer.Deserialize<LecturerDeletedEvent>(body);
                    if (evt is not null)
                    {
                        var existing = await db.LecturerSnapshots.FirstOrDefaultAsync(x => x.ExternalId == evt.LecturerId, cancellationToken);
                        if (existing is not null)
                        {
                            db.LecturerSnapshots.Remove(existing);
                        }
                    }
                }
                else if (eventType == nameof(LecturerCreatedEvent) || eventType == nameof(LecturerUpdatedEvent))
                {
                    var evt = JsonSerializer.Deserialize<LecturerCreatedEvent>(body);
                    if (evt is not null)
                    {
                        var existing = await db.LecturerSnapshots.FirstOrDefaultAsync(x => x.ExternalId == evt.LecturerId, cancellationToken);
                        if (existing is null)
                        {
                            db.LecturerSnapshots.Add(new LecturerSnapshot
                            {
                                ExternalId = evt.LecturerId,
                                FirstName = evt.FirstName,
                                LastName = evt.LastName,
                                Title = evt.Title,
                                UpdatedAtUtc = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            existing.FirstName = evt.FirstName;
                            existing.LastName = evt.LastName;
                            existing.Title = evt.Title;
                            existing.UpdatedAtUtc = DateTime.UtcNow;
                        }
                    }
                }

                db.ProcessedMessages.Add(new ProcessedMessage
                {
                    EventId = messageId,
                    EventType = eventType,
                    ProcessedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            _logger.LogWarning("Unknown event type {EventType} received. DeliveryTag: {DeliveryTag}", eventType, ea.DeliveryTag);
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing RabbitMQ message. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
            if (_channel is not null)
            {
                try
                {
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);

                }
                catch { }
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();

        base.Dispose();
    }
}
