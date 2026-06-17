using System.Text;
using System.Text.Json;
using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.SagaMessages;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventPlatformAPI.Worker.Services;

public sealed class SagaEmailCommandConsumerHostedService : BackgroundService
{
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly WorkerOptions _workerOptions;
    private readonly ILogger<SagaEmailCommandConsumerHostedService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    public SagaEmailCommandConsumerHostedService(
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<WorkerOptions> workerOptions,
        ILogger<SagaEmailCommandConsumerHostedService> logger)
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
            queue: SagaQueues.SendRegistrationEmail,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleSagaEmailCommandAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: SagaQueues.SendRegistrationEmail,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Saga email command consumer started. Listening on queue: {Queue}", SagaQueues.SendRegistrationEmail);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Saga email command consumer stopped.");
        }
    }

    private async Task HandleSagaEmailCommandAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_channel is null)
            return;

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var command = JsonSerializer.Deserialize<SendRegistrationEmailCommand>(body);

            if (command is null)
            {
                _logger.LogWarning("Received invalid saga email command payload. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            try
            {
                var filename = Path.Combine(
                    _workerOptions.OutboxFolder,
                    $"saga-email-{command.CorrelationId}.txt");

                var content = $"To: {command.UserEmail}\r\n" +
                              $"FullName: {command.UserFullName}\r\n" +
                              $"Subject: Registration Confirmation for Event #{command.EventId}\r\n" +
                              $"Body: Dear {command.UserFullName}, your registration for event #{command.EventId} has been confirmed.\r\n" +
                              $"Timestamp: {command.Timestamp:O}\r\n" +
                              $"CorrelationId: {command.CorrelationId}\r\n";

                await File.WriteAllTextAsync(filename, content, cancellationToken);

                _logger.LogInformation(
                    "Saga email file written: {Filename} (CorrelationId={CorrelationId})",
                    filename, command.CorrelationId);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to write saga email file. CorrelationId={CorrelationId}, DeliveryTag={DeliveryTag}",
                    command.CorrelationId, ea.DeliveryTag);

                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in saga email command consumer. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
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
