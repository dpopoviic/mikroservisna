using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using EventPlatformAPI.EventsAPI.Services;
using EventPlatformAPI.Messages.Commands;
using EventPlatformAPI.Messages.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.EventsAPI.HostedServices
{
    public sealed class SagaCommandConsumerHostedService : BackgroundService
    {
        private readonly ILogger<SagaCommandConsumerHostedService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitMqOptions _rabbitMqOptions;

        private IConnection? _connection;
        private IChannel? _channel;

        public SagaCommandConsumerHostedService(
            ILogger<SagaCommandConsumerHostedService> logger,
            IServiceScopeFactory scopeFactory,
            IOptions<RabbitMqOptions> rabbitMqOptions)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _rabbitMqOptions = rabbitMqOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqOptions.HostName,
                Port = _rabbitMqOptions.Port,
                UserName = _rabbitMqOptions.UserName,
                Password = _rabbitMqOptions.Password
            };
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                "events.create-event.command", 
                durable: true, 
                exclusive: false, 
                autoDelete: false, 
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                "events.cancel-event.command",
                durable: true, exclusive: false, 
                autoDelete: false, 
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(0, 1, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                "events.create-event.command", 
                autoAck: false, 
                consumer: consumer, 
                cancellationToken: stoppingToken);

            await _channel.BasicConsumeAsync(
                "events.cancel-event.command", 
                autoAck: false, 
                consumer: consumer, 
                cancellationToken: stoppingToken);

            _logger.LogInformation("[EVENTSAPI] Saga command consumer started.");

            try 
            { 
                await Task.Delay(Timeout.Infinite, stoppingToken); 
            }
            catch (OperationCanceledException) 
            { 

            }
        }
        private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
        {
            if (_channel is null)
            {
                _logger.LogError("RabbitMQ channel is not initialized.");
                return;
            }

            var messageId = ea.BasicProperties.MessageId ?? string.Empty;
            var messageType = ea.BasicProperties.Type ?? string.Empty;
            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();

                //idempotent cek
                if (!string.IsNullOrEmpty(messageId) && await db.ProcessedMessages.AnyAsync(x => x.EventId == messageId, stoppingToken))
                {
                    _logger.LogInformation("Message with ID {MessageId} has already been processed. Acknowledging and skipping.", messageId);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
                    return;
                }
                await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

                switch (messageType)
                {
                    case nameof(CreateEventCommand):
                        await HandleCreateEventAsync(db, payload, stoppingToken);
                        break;

                    case nameof(CancelEventCommand):
                        await HandleCancelEventAsync(db, payload, stoppingToken);
                        break;
                }
                //Dodati da se promeni IsPublished u true
                if (!string.IsNullOrEmpty(messageId))
                {
                    db.ProcessedMessages.Add(new ProcessedMessage
                    {
                        EventId = messageId,
                        EventType = messageType,
                        ProcessedAtUtc = DateTime.UtcNow
                    });
                }

                await db.SaveChangesAsync(stoppingToken);
                await tx.CommitAsync(stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EVENTSAPI] Failed to process command {Type} MessageId={MessageId}", messageType, messageId);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
            }
        }
        private async Task HandleCreateEventAsync(EventsDbContext db, string payload, CancellationToken stoppingToken)
        {
            var cmd  = JsonSerializer.Deserialize<CreateEventCommand>(payload);
            if(cmd is null)
            {
                _logger.LogError("Failed to deserialize CreateEventCommand from payload: {Payload}", payload);
                return;
            }

            _logger.LogInformation("Processing CreateEventCommand with CorrelationId={CorrelationId}", cmd.CorrelationId);

            //provera idempotentnosti na nivou komande
            var e = new Event
            {
                Name = cmd.Name,
                Agenda = cmd.Agenda,
                DateTime = cmd.DateTime,
                DurationInHours = cmd.DurationInHours,
                Price = cmd.Price,
                TypeId = cmd.TypeId,
                LocationId = cmd.LocationId,
                CorrelationId = cmd.CorrelationId //dodati u model i migraciju
            };

            db.Events.Update(e);
            await db.SaveChangesAsync(stoppingToken);

            //** 
            //foreach (var lecturerId in cmd.LecturerIds)
            //{
            //    db.EventLecturers.Add(new EventLecturer
            //    {
            //        EventId = e.Id,
            //        LecturerId = lecturerId
            //    });
            //}
            //await db.SaveChangesAsync(stoppingToken);

            var replyEvent = new EventCreatedEvent
            {
                EventId = e.Id,
                CorrelationId = cmd.CorrelationId
            };

            EnqueueOutboxMessage(db, "safa.event-created.event", replyEvent);
            _logger.LogInformation("Enqueued EventCreatedEvent for EventId={EventId} with CorrelationId={CorrelationId}", e.Id, cmd.CorrelationId);
        }
        private async Task HandleCancelEventAsync(EventsDbContext db, string payload, CancellationToken stoppingToken)
        {
            var cmd = JsonSerializer.Deserialize<CancelEventCommand>(payload);
            if(cmd is null)
            {
                _logger.LogError("Failed to deserialize CancelEventCommand from payload: {Payload}", payload);
                return;
            }   
            
            _logger.LogInformation("Processing CancelEventCommand for EventId={EventId} with CorrelationId={CorrelationId}", cmd.EventId, cmd.CorrelationId);

            var e  = await db.Events.FindAsync(
                new object[] { cmd.EventId }, 
                cancellationToken: stoppingToken
                );
            if (e is not null)
            {
                //e.IsDeleted = true;

                db.Events.Update(e);    
                await db.SaveChangesAsync(stoppingToken);
            }
            var replyEvent = new EventCancelledEvent
            {
                EventId = cmd.EventId,
                CorrelationId = cmd.CorrelationId
            };

            EnqueueOutboxMessage(db, "safa.event-cancelled.event", replyEvent);
            _logger.LogInformation("Enqueued EventCancelledEvent for EventId={EventId} with CorrelationId={CorrelationId}", cmd.EventId, cmd.CorrelationId);
        }
        private void EnqueueOutboxMessage<T>(EventsDbContext db, string destination, T payload)
        {
            db.OutboxMessages.Add(new OutboxMessage
            {
                Destination = destination,
                MessageId = Guid.NewGuid(),
                Type = typeof(T).Name,
                Payload = JsonSerializer.Serialize(payload),
                CreatedAtUtc = DateTime.UtcNow,
                IsPublished = false
            });
        }
        
        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
