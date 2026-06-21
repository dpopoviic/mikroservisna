using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.Choreography;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.Worker.Services;

public sealed class EventSeatReleasedChoreographyConsumerHostedService : BackgroundService
{
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly WorkerOptions _workerOptions;
    private readonly ILogger<EventSeatReleasedChoreographyConsumerHostedService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public EventSeatReleasedChoreographyConsumerHostedService(
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<WorkerOptions> workerOptions,
        ILogger<EventSeatReleasedChoreographyConsumerHostedService> logger)
    {
        _rabbitMqOptions = rabbitMqOptions.Value;
        _workerOptions = workerOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_workerOptions.OutboxFolder);

        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqOptions.HostName,
            Port = _rabbitMqOptions.Port,
            UserName = _rabbitMqOptions.UserName,
            Password = _rabbitMqOptions.Password
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: SagaQueues.ChoreographyEventSeatReleased,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleEventSeatReleasedAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: SagaQueues.ChoreographyEventSeatReleased,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Choreography EventSeatReleased consumer started. Listening on queue: {Queue}", SagaQueues.ChoreographyEventSeatReleased);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Choreography EventSeatReleased consumer stopped.");
        }
    }

    private async Task HandleEventSeatReleasedAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_channel is null) return;

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<EventSeatReleasedEvent>(body);

            if (evt is null)
            {
                _logger.LogWarning("Received invalid EventSeatReleased payload. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            try
            {
                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] Worker: EventSeatReleased received. EventId={EventId}",
                    evt.CorrelationId, evt.EventId);

                var emailContent = new
                {
                    CorrelationId = evt.CorrelationId,
                    RegistrationId = evt.RegistrationId,
                    EventId = evt.EventId,
                    Subject = "Registration Cancellation Confirmed",
                    Body = $"Your registration for event #{evt.EventId} has been successfully cancelled.",
                    Timestamp = evt.Timestamp
                };

                var fileName = Path.Combine(
                    _workerOptions.OutboxFolder,
                    $"choreography-cancellation-{evt.CorrelationId}.json");

                await File.WriteAllTextAsync(
                    fileName,
                    JsonSerializer.Serialize(emailContent),
                    cancellationToken);

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] Cancellation email file written: {Filename}",
                    evt.CorrelationId, fileName);

                var sentEvent = new CancellationEmailSentEvent
                {
                    CorrelationId = evt.CorrelationId,
                    RegistrationId = evt.RegistrationId,
                    UserId = Guid.Empty,
                    EventId = evt.EventId,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(sentEvent);
                var eventBody = Encoding.UTF8.GetBytes(json);

                var props = new BasicProperties
                {
                    Persistent = true,
                    Type = nameof(CancellationEmailSentEvent),
                    ContentType = "application/json"
                };

                await _channel.QueueDeclareAsync(
                    SagaQueues.ChoreographyCancellationEmailSent,
                    durable: true, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: cancellationToken);

                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: SagaQueues.ChoreographyCancellationEmailSent,
                    mandatory: false,
                    basicProperties: props,
                    body: eventBody,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] CancellationEmailSent published to {Queue}",
                    evt.CorrelationId, SagaQueues.ChoreographyCancellationEmailSent);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[CorrelationId={CorrelationId}] Failed to process EventSeatReleased. DeliveryTag={DeliveryTag}",
                    evt.CorrelationId, ea.DeliveryTag);

                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in EventSeatReleased consumer. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
            if (_channel is not null)
            {
                try
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
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
