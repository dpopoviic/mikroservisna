using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.Choreography;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.Worker.Services;

public sealed class CancellationCompensatedChoreographyConsumerHostedService : BackgroundService
{
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly WorkerOptions _workerOptions;
    private readonly ILogger<CancellationCompensatedChoreographyConsumerHostedService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public CancellationCompensatedChoreographyConsumerHostedService(
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<WorkerOptions> workerOptions,
        ILogger<CancellationCompensatedChoreographyConsumerHostedService> logger)
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
            queue: SagaQueues.ChoreographyRegistrationCancellationCompensated,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleCancellationCompensatedAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: SagaQueues.ChoreographyRegistrationCancellationCompensated,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Choreography CancellationCompensated consumer started. Listening on queue: {Queue}",
            SagaQueues.ChoreographyRegistrationCancellationCompensated);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Choreography CancellationCompensated consumer stopped.");
        }
    }

    private async Task HandleCancellationCompensatedAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_channel is null) return;

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<RegistrationCancellationCompensatedEvent>(body);

            if (evt is null)
            {
                _logger.LogWarning("Received invalid RegistrationCancellationCompensated payload. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            try
            {
                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] Worker: RegistrationCancellationCompensated received. EventId={EventId}",
                    evt.CorrelationId, evt.EventId);

                var emailContent = new
                {
                    CorrelationId = evt.CorrelationId,
                    RegistrationId = evt.RegistrationId,
                    UserId = evt.UserId,
                    EventId = evt.EventId,
                    Subject = "Registration Cancellation Failed — Registration Restored",
                    Body = $"Your registration for event #{evt.EventId} could not be cancelled at this time. Your registration has been restored and remains confirmed.",
                    Timestamp = evt.Timestamp
                };

                var fileName = Path.Combine(
                    _workerOptions.OutboxFolder,
                    $"choreography-compensation-{evt.CorrelationId}.json");

                await File.WriteAllTextAsync(
                    fileName,
                    JsonSerializer.Serialize(emailContent),
                    cancellationToken);

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] Compensation email file written: {Filename}",
                    evt.CorrelationId, fileName);

                var sentEvent = new CancellationCompensationEmailSentEvent
                {
                    CorrelationId = evt.CorrelationId,
                    RegistrationId = evt.RegistrationId,
                    UserId = evt.UserId,
                    EventId = evt.EventId,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(sentEvent);
                var eventBody = Encoding.UTF8.GetBytes(json);

                var props = new BasicProperties
                {
                    Persistent = true,
                    Type = nameof(CancellationCompensationEmailSentEvent),
                    ContentType = "application/json"
                };

                await _channel.QueueDeclareAsync(
                    SagaQueues.ChoreographyCancellationCompensationEmailSent,
                    durable: true, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: cancellationToken);

                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: SagaQueues.ChoreographyCancellationCompensationEmailSent,
                    mandatory: false,
                    basicProperties: props,
                    body: eventBody,
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "[CorrelationId={CorrelationId}] CancellationCompensationEmailSent published to {Queue}",
                    evt.CorrelationId, SagaQueues.ChoreographyCancellationCompensationEmailSent);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[CorrelationId={CorrelationId}] Failed to process RegistrationCancellationCompensated. DeliveryTag={DeliveryTag}",
                    evt.CorrelationId, ea.DeliveryTag);

                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in CancellationCompensated consumer. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
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
