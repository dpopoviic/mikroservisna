using System.Text;
using System.Text.Json;
using EventPlatformAPI.Messages.Commands;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventPlatformAPI.EventsAPI.Services;

public interface ISagaTriggerService
{
    Task<Guid> TriggerPublishEventAsync(StartEventPublicationSagaCommand command, CancellationToken ct = default);
}

public class SagaTriggerService : ISagaTriggerService, IAsyncDisposable
{
    private readonly ILogger<SagaTriggerService> _logger;
    private readonly RabbitMqOptions _rabbitMqOptions;


    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string StartEventPublicationSagaCommandQueue = "saga.start-event-publication.command";

    public SagaTriggerService(ILogger<SagaTriggerService> logger, IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _logger = logger;
        _rabbitMqOptions = rabbitMqOptions.Value;

    }

    public async Task<Guid> TriggerPublishEventAsync(StartEventPublicationSagaCommand command, CancellationToken ct = default)
    {
        await EnsureChannelAsync(ct);

        var messageId = Guid.NewGuid();
        var payload = JsonSerializer.Serialize(command);
        var body = Encoding.UTF8.GetBytes(payload);

        var props = new BasicProperties
        {
            Persistent = true,
            MessageId = messageId.ToString(),
            Type = nameof(StartEventPublicationSagaCommand),
            ContentType = "application/json"
        };

        await _channel!.QueueDeclareAsync(
            StartEventPublicationSagaCommandQueue, 
            durable: true,
            exclusive: false, 
            autoDelete: false,
            cancellationToken: ct);

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: StartEventPublicationSagaCommandQueue,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct);

        _logger.LogInformation(
            "[EVENTSAPI] StartEventPublicationSagaCommand sent to saga. CorrelationId={CorrelationId} MessageId={MessageId}",
            command.CorrelationId, messageId);

        return command.CorrelationId;
    }

    private async Task EnsureChannelAsync(CancellationToken ct)
    {
        if (_channel is not null) return;

        await _lock.WaitAsync(ct);
        try
        {
            if (_channel is not null) return;

            var factory = new ConnectionFactory
            {
                HostName = _rabbitMqOptions.HostName,
                Port = _rabbitMqOptions.Port,
                UserName = _rabbitMqOptions.UserName,
                Password = _rabbitMqOptions.Password
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        _lock.Dispose();
    }
}