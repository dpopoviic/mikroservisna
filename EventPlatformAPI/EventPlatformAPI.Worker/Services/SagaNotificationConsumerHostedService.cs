
using EventPlatformAPI.Messages.Commands;
using EventPlatformAPI.Messages.IntegrationEvents;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace EventPlatformAPI.Worker.Services
{
    public sealed class SagaNotificationConsumerHostedService : BackgroundService
    {
        private readonly RabbitMqOptions _options;
        private readonly WorkerOptions _workerOptions;
        private readonly ILogger<SagaNotificationConsumerHostedService> _logger;

        private IConnection? _connection;
        private IChannel? _channel;

        public SagaNotificationConsumerHostedService(
       IOptions<RabbitMqOptions> options,
       IOptions<WorkerOptions> workerOptions,
       ILogger<SagaNotificationConsumerHostedService> logger)
        {
            _options = options.Value;
            _workerOptions = workerOptions.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Directory.CreateDirectory(_workerOptions.OutboxFolder);
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
                "worker.send-event-notification.command",
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);
            
            await _channel.BasicQosAsync(0, 1, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (_, ea) => HandleAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                "worker.send-event-notification.command",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("[WORKER] Saga notification consumer started.");

            try 
            { 
                await Task.Delay(Timeout.Infinite, stoppingToken); 
            }
            catch (OperationCanceledException) 
            { 
            }
        }

        private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken stoppingToken)
        {
            if (_channel is null) return;

            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                var cmd = JsonSerializer.Deserialize<SendEventNotificationCommand>(payload);
                if(cmd is null)
                {
                    _logger.LogWarning("[WORKER] Received invalid command payload.");
                    await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    return;
                }

                _logger.LogInformation("[WORKER] SendEventNotificationCommand received. EventId={EventId} CorrelationId={CorrelationId} To={To}", cmd.EventId, cmd.CorrelationId, cmd.To);

                //write email 
                var filename= Path.Combine(_workerOptions.OutboxFolder, $"email-{cmd.CorrelationId}.txt");
                var content = $"To: {cmd.To}\r\nSubject: {cmd.Subject}\r\nBody: {cmd.Body}\r\nTimestamp: {DateTime.UtcNow:O}\r\n";
                await File.WriteAllTextAsync(filename, content, stoppingToken);

                _logger.LogInformation("[WORKER] Email file written: {Filename}", filename);

                await PublishReplyAsync("saga.email-sent.event", new EmailSentEvent
                {
                    CorrelationId = cmd.CorrelationId,
                    EventId = cmd.EventId
                }, stoppingToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WORKER] Failed to process SendEventNotificationCommand.");

                try
                {
                    //**
                    var cmd = JsonSerializer.Deserialize<SendEventNotificationCommand>(payload);
                    if (cmd is not null)
                    {
                        await PublishReplyAsync("saga.email-failed.event", new EmailFailedEvent
                        {
                            CorrelationId = cmd.CorrelationId,
                            EventId = cmd.EventId,
                            Reason = ex.Message
                        }, stoppingToken);
                    }
                }
                catch { }

                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
            }

        }

        private async Task PublishReplyAsync<T>(string queue, T message, CancellationToken ct)
        {
            if (_channel is null) return;

            await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            var props = new BasicProperties
            {
                Persistent = true,
                MessageId = Guid.NewGuid().ToString(),
                Type = typeof(T).Name,
                ContentType = "application/json"
            };

            await _channel.BasicPublishAsync(string.Empty, queue, false, props, body, ct);
            _logger.LogInformation("[WORKER] Published {Type} to {Queue}", typeof(T).Name, queue);
        }

    }
}
