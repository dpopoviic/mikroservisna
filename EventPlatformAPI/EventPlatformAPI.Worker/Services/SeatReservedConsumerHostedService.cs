using EventPlatformAPI.Messages.IntegrationEvents;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.Worker.Services;

public sealed class SeatReservedConsumerHostedService : BackgroundService
{
    private readonly RabbitMqOptions _options;
    private readonly WorkerOptions _workerOptions;
    private readonly ILogger<SeatReservedConsumerHostedService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string InboundQueue = "registration.seat-reserved.event";
    private const string EmailSentQueue = "registration.email-sent.event";
    private const string EmailFailedQueue = "registration.email-failed.event";

    public SeatReservedConsumerHostedService(
        IOptions<RabbitMqOptions> options,
        IOptions<WorkerOptions> workerOptions,
        ILogger<SeatReservedConsumerHostedService> logger)
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

        foreach (var queue in new[] { InboundQueue, EmailSentQueue, EmailFailedQueue })
        {
            await _channel.QueueDeclareAsync(queue, durable: true,
                exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        }

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(InboundQueue, autoAck: false,
            consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("[WORKER] SeatReservedConsumer started.");

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
    }

    private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (_channel is null) return;

        var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            var evt = JsonSerializer.Deserialize<SeatReservedEvent>(payload);
            if (evt is null)
            {
                _logger.LogWarning("[WORKER] Could not deserialize SeatReservedEvent.");
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                return;
            }

            _logger.LogInformation(
                "[WORKER] SeatReservedEvent received. RegistrationId={Id} EventId={EventId} CorrelationId={Cid}",
                evt.RegistrationId, evt.EventId, evt.CorrelationId);

            var filename = Path.Combine(
                _workerOptions.OutboxFolder,
                $"registration-confirmation-{evt.CorrelationId}.txt");

            var content =
                $"To: participant\r\n" +
                $"Subject: Registration Confirmation\r\n" +
                $"Body: Your registration for Event {evt.EventId} (Registration {evt.RegistrationId}) " +
                $"has been confirmed.\r\n" +
                $"Timestamp: {DateTime.UtcNow:O}\r\n" +
                $"CorrelationId: {evt.CorrelationId}\r\n";

            await File.WriteAllTextAsync(filename, content, ct);

            _logger.LogInformation("[WORKER] Registration confirmation email written: {Filename}", filename);

            await PublishAsync(EmailSentQueue, new RegistrationEmailSentEvent
            {
                CorrelationId = evt.CorrelationId,
                OccurredAt = DateTime.UtcNow,
                RegistrationId = evt.RegistrationId,
                EventId = evt.EventId
            }, ct);

            await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WORKER] Failed to process SeatReservedEvent.");

            try
            {
                var evt = JsonSerializer.Deserialize<SeatReservedEvent>(payload);
                if (evt is not null)
                {
                    await PublishAsync(EmailFailedQueue, new RegistrationEmailFailedEvent
                    {
                        CorrelationId = evt.CorrelationId,
                        OccurredAt = DateTime.UtcNow,
                        RegistrationId = evt.RegistrationId,
                        EventId = evt.EventId,
                        FailureReason = ex.Message
                    }, ct);
                }
            }
            catch { }

            await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
        }
    }

    private async Task PublishAsync<T>(string queue, T message, CancellationToken ct)
    {
        if (_channel is null) return;

        await _channel.QueueDeclareAsync(queue, durable: true,
            exclusive: false, autoDelete: false, cancellationToken: ct);

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

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
