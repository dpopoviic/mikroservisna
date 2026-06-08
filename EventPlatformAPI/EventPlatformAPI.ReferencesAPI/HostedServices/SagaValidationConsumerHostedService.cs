
using EventPlatformAPI.Messages.Commands;
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

namespace EventPlatformAPI.ReferencesAPI.HostedServices
{
    public sealed class SagaValidationConsumerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaValidationConsumerHostedService> _logger;
        private readonly RabbitMqOptions _options;

        private IConnection? _connection;
        private IChannel? _channel;

        public SagaValidationConsumerHostedService(IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<SagaValidationConsumerHostedService> logger)
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

            await _channel.QueueDeclareAsync(
                "references.validate-location.command",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.QueueDeclareAsync(
                "references.validate-lecturers.command",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);

            await _channel.BasicQosAsync(0, 1, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (_, ea) => HandleMessageAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                "references.validate-location.command",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);
            await _channel.BasicConsumeAsync(
                "references.validate-lecturers.command",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("[REFERENCESAPI] Saga validation consumer started.");

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
            if(_channel == null)
            {
                _logger.LogError("Channel is not initialized.");
                return;
            }

            var messageType = ea.BasicProperties.Type ?? string.Empty;
            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();
                var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                switch (messageType)
                {
                    case nameof(ValidateLocationCommand):
                        await HandleValidateLocationAsync(db, outboxRepo, payload, stoppingToken);
                        break;
                    case nameof(ValidateLecturersCommand):
                        await HandleValidateLecturersAsync(db, outboxRepo, payload, stoppingToken);
                        break;
                    default:
                        _logger.LogWarning("[REFERENCESAPI] Unknown command type: {Type}", messageType);
                        break;
                }
                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REFERENCESAPI] Failed to handle command {Type}", messageType);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
            }
        }
        private async Task HandleValidateLocationAsync(ReferenceDbContext db, IOutboxRepository outboxRepo, string payload, CancellationToken stoppingToken)
        {
            var cmd = JsonSerializer.Deserialize<ValidateLocationCommand>(payload);
            if(cmd == null)
                {
                    _logger.LogError("[REFERENCESAPI] Failed to deserialize ValidateLocationCommand.");
                    return;
            }   
            _logger.LogInformation("[REFERENCESAPI] Validating location with ID {LocationId} for correlation {CorrelationId}", cmd.LocationId, cmd.CorrelationId);
            var exists = await db.Locations.AnyAsync(l => l.Id == cmd.LocationId, stoppingToken);
            if (exists)
            {
                await EnqueueAsync(outboxRepo, "saga.location-validated.event", new LocationValidatedEvent
                {
                    CorrelationId = cmd.CorrelationId,
                    LocationId = cmd.LocationId
                }, stoppingToken);
                _logger.LogInformation("[REFERENCESAPI] Location with ID {LocationId} is valid.", cmd.LocationId);
            }
            else
            {
                await EnqueueAsync(outboxRepo, "saga.location-validation-failed.event", new LocationValidationFailedEvent
                {
                    CorrelationId = cmd.CorrelationId,
                    LocationId = cmd.LocationId,
                    Reason = $"Location {cmd.LocationId} does not exist."
                }, stoppingToken);

                _logger.LogWarning("[REFERENCESAPI] Location {LocationId} NOT found. CorrelationId={CorrelationId}", cmd.LocationId, cmd.CorrelationId);
            }

        }
        private async Task HandleValidateLecturersAsync(ReferenceDbContext db, IOutboxRepository outbox, string payload, CancellationToken ct)
        {
            var cmd = JsonSerializer.Deserialize<ValidateLecturersCommand>(payload);
            if (cmd is null) return;

            _logger.LogInformation("[REFERENCESAPI] ValidateLecturersCommand. CorrelationId={CorrelationId}", cmd.CorrelationId);

            var foundIds = await db.Lecturers
                .Where(x => cmd.LecturerIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            var missingIds = cmd.LecturerIds.Except(foundIds).ToList();

            if (missingIds.Count == 0)
            {
                await EnqueueAsync(outbox, "saga.lecturers-validated.event", new LecturersValidatedEvent
                {
                    CorrelationId = cmd.CorrelationId,
                    LecturerIds = cmd.LecturerIds
                }, ct);
                _logger.LogInformation("[REFERENCESAPI] All lecturers validated. CorrelationId={CorrelationId}", cmd.CorrelationId);
            }
            else
            {
                await EnqueueAsync(outbox, "saga.lecturers-validation-failed.event", new LecturersValidationFailedEvent
                {
                    CorrelationId = cmd.CorrelationId,
                    InvalidLecturerIds = missingIds,
                    Reason = $"Lecturers not found: {string.Join(", ", missingIds)}"
                }, ct);
                _logger.LogWarning("[REFERENCESAPI] Missing lecturers {Ids}. CorrelationId={CorrelationId}", string.Join(",", missingIds), cmd.CorrelationId);
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
}
