using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.EventsApiMessages;
using EventPlatformAPI.Messages.Saga.UserApiMessages;
using EventPlatformAPI.SagaOrcgestrator.Interfaces;
using EventPlatformAPI.SagaOrcgestrator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.SagaOrcgestrator.HostedServices
{
    public sealed class SagaConsumerHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaConsumerHostedService> _logger;
        private readonly SagaRabbitMqOptions _options;

        private IConnection? _connection;
        private IChannel? _channel;

        private static readonly string[] EventQueues =
       [
            SagaQueues.RegistrationRequested,
            SagaQueues.EventSeatReserved,
            SagaQueues.EventSeatReservationFailed,
            SagaQueues.RegistrationConfirmed,
            SagaQueues.RegistrationConfirmationFailed,
            SagaQueues.RegistrationEmailSent,
            SagaQueues.RegistrationEmailFailed
       ];

        public SagaConsumerHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<SagaRabbitMqOptions> options,
            ILogger<SagaConsumerHostedService> logger)
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

                _logger.LogInformation("Saga consumer listening on queue: {Queue}", queue);
            }

            try { await Task.Delay(Timeout.Infinite, stoppingToken); }
            catch (OperationCanceledException) { }
        }
        private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken ct)
        {
            if (_channel is null) return;

            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var messageType = ea.BasicProperties.Type ?? string.Empty;
            var queue = ea.RoutingKey;

            _logger.LogInformation(
                "Saga received message Type={Type} Queue={Queue}", messageType, queue);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var coordinator = scope.ServiceProvider
                    .GetRequiredService<IRegistrationSagaCoordinator>();

                await DispatchAsync(coordinator, queue, body, ct);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing saga message. Queue={Queue} Type={Type}", queue, messageType);

                // Nack without requeue to prevent infinite loop;
                // consider a DLQ in production.
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
            }
        }
        private static Task DispatchAsync(
            IRegistrationSagaCoordinator coordinator,
            string queue,
            string body,
            CancellationToken ct)
        {
            return queue switch
            {
                SagaQueues.RegistrationRequested =>
                    coordinator.HandleRegistrationRequestedAsync(
                        Deserialize<RegistrationRequestedEvent>(body), ct),

                SagaQueues.EventSeatReserved =>
                    coordinator.HandleEventSeatReservedAsync(
                        Deserialize<EventSeatReservedEvent>(body), ct),

                SagaQueues.EventSeatReservationFailed =>
                    coordinator.HandleEventSeatReservationFailedAsync(
                        Deserialize<EventSeatReservationFailedEvent>(body), ct),

                SagaQueues.RegistrationConfirmed =>
                    coordinator.HandleRegistrationConfirmedAsync(
                        Deserialize<RegistrationConfirmedEvent>(body), ct),

                SagaQueues.RegistrationConfirmationFailed =>
                    coordinator.HandleRegistrationConfirmationFailedAsync(
                        Deserialize<RegistrationConfirmationFailedEvent>(body), ct),

                SagaQueues.RegistrationEmailSent =>
                    coordinator.HandleRegistrationEmailSentAsync(
                        Deserialize<RegistrationEmailSentEvent>(body), ct),

                SagaQueues.RegistrationEmailFailed =>
                    coordinator.HandleRegistrationEmailFailedAsync(
                        Deserialize<RegistrationEmailFailedEvent>(body), ct),

                _ => Task.CompletedTask
            };
        }
        private static T Deserialize<T>(string json)
            => JsonSerializer.Deserialize<T>(json)
               ?? throw new InvalidOperationException(
                   $"Failed to deserialize {typeof(T).Name} from JSON.");

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
