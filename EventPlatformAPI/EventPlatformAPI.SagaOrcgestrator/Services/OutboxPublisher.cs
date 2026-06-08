using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventPlatformAPI.SagaOrcgestrator.Services;

public interface IOutboxPublisher
{
    Task PublishAsync(string destination, Guid messageId, string payload, string type, CancellationToken cancellationToken = default);
}

public class OutboxPublisher : IOutboxPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public OutboxPublisher(IOptions<RabbitMqOptions> options, ILogger<OutboxPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(string destination, Guid messageId, string payload, string type, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (_channel is null)
        {
            throw new InvalidOperationException("RabbitMQ channel is not initialized.");
        }

        await _channel.QueueDeclareAsync(destination, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(payload);
        var props = new BasicProperties
        {
            Persistent = true,
            MessageId = messageId.ToString(),
            Type = type,
            ContentType = "application/json"
        };

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: destination,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Published command/event {Type} to queue {Destination}. MessageId={MessageId}", type, destination, messageId);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is not null)
            {
                return;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_channel is not null)
            {
                await _channel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }
        catch
        {
        }
        finally
        {
            _initLock.Dispose();
        }
    }
}