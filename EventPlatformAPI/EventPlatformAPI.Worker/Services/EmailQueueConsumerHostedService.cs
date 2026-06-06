using System.Text;
using System.Text.Json;
using EventPlatformAPI.Messages.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventPlatformAPI.Worker.Services;

public sealed class EmailQueueConsumerHostedService : BackgroundService
{
    private readonly RabbitMqOptions _rabbitMqOptions;
    private readonly WorkerOptions _workerOptions;
    private readonly ILogger<EmailQueueConsumerHostedService> _logger;

    private IConnection? _connection;
    private IChannel? _channel;             

    private readonly Queue<DateTime> _sentTimestamps = new();
    private readonly SemaphoreSlim _throttleLock = new(1, 1); 
    private const int MaxEmailsPerMinute = 10;
    private const int ThrottleWindowSeconds = 60;

    public EmailQueueConsumerHostedService(
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IOptions<WorkerOptions> workerOptions,
        ILogger<EmailQueueConsumerHostedService> logger)
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

        await _channel.QueueDeclareAsync(
            queue: _rabbitMqOptions.EmailRequestQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: _rabbitMqOptions.EmailDlqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleEmailAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: _rabbitMqOptions.EmailRequestQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Email consumer started. Listening on queue: {Queue}", _rabbitMqOptions.EmailRequestQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Email consumer stopped.");
        }
    }

    private async Task HandleEmailAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_channel is null)
            return;

        try
        {
            await ApplyThrottleAsync(cancellationToken);

            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var emailMessage = JsonSerializer.Deserialize<EmailRequestMessage>(body);

            if (emailMessage is null)
            {
                _logger.LogWarning("Received invalid email message payload. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            try
            {
                var filename = Path.Combine(_workerOptions.OutboxFolder, $"email-{emailMessage.MessageId}.txt");
                var content = $"To: {emailMessage.To}\r\n" +
                              $"Subject: {emailMessage.Subject}\r\n" +
                              $"Body: {emailMessage.Body}\r\n" +
                              $"Timestamp: {emailMessage.EnqueuedAt:O}\r\n";

                await File.WriteAllTextAsync(filename, content, cancellationToken);

                _logger.LogInformation("Email file written: {Filename}", filename);

                await _throttleLock.WaitAsync(cancellationToken);
                try
                {
                    _sentTimestamps.Enqueue(DateTime.UtcNow);
                }
                finally
                {
                    _throttleLock.Release();
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                var retries = GetRetryCount(ea.BasicProperties);
                _logger.LogWarning(ex, "Failed to write email file. Retries: {Retries}, MessageId: {MessageId}", retries, emailMessage.MessageId);

                if (retries < 10)
                {
                    await RepublishWithRetryAsync(emailMessage, retries + 1, cancellationToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                }
                else
                {
                    await SendToDlqAsync(emailMessage, cancellationToken);
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in email consumer. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
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

    private async Task ApplyThrottleAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            TimeSpan waitTime = TimeSpan.Zero;

            await _throttleLock.WaitAsync(cancellationToken);
            try
            {
                var cutoff = DateTime.UtcNow.AddSeconds(-ThrottleWindowSeconds);

                while (_sentTimestamps.Count > 0 && _sentTimestamps.Peek() < cutoff)
                    _sentTimestamps.Dequeue();

                if (_sentTimestamps.Count < MaxEmailsPerMinute)
                    return; 

                var oldestTimestamp = _sentTimestamps.Peek();
                waitTime = oldestTimestamp.AddSeconds(ThrottleWindowSeconds) - DateTime.UtcNow;
            }
            finally
            {
                _throttleLock.Release();
            }

            if (waitTime > TimeSpan.Zero)
            {
                _logger.LogInformation("Throttling applied. Waiting {WaitMs}ms before processing next email.", (long)waitTime.TotalMilliseconds);
                await Task.Delay(waitTime, cancellationToken); 
            }
        }
    }

    private int GetRetryCount(IReadOnlyBasicProperties? properties)
    {
        if (properties?.Headers is null)
            return 0;

        if (properties.Headers.TryGetValue("x-retries", out var retriesObj))
        {
            if (retriesObj is int retries)
                return retries;

            if (retriesObj is byte[] retriesBytes)
                return BitConverter.ToInt32(retriesBytes, 0);
        }

        return 0;
    }

    private async Task RepublishWithRetryAsync(EmailRequestMessage emailMessage, int retryCount, CancellationToken cancellationToken)
    {
        if (_channel is null)
            return;

        await Task.Delay(2000, cancellationToken);

        var json = JsonSerializer.Serialize(emailMessage);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true,
            Headers = new Dictionary<string, object?> { ["x-retries"] = retryCount }
        };

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _rabbitMqOptions.EmailRequestQueue,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Email message republished with retry count {RetryCount}. MessageId: {MessageId}", retryCount, emailMessage.MessageId);
    }

    private async Task SendToDlqAsync(EmailRequestMessage emailMessage, CancellationToken cancellationToken)
    {
        if (_channel is null)
            return;

        var json = JsonSerializer.Serialize(emailMessage);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _rabbitMqOptions.EmailDlqQueue,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogError("Email message sent to DLQ after max retries. MessageId: {MessageId}", emailMessage.MessageId);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}