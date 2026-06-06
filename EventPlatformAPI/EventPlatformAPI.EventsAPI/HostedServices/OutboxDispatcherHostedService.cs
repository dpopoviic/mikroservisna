using System.Text;
using EventPlatformAPI.EventsAPI.Data;
using EventPlatformAPI.EventsAPI.Models;
using EventPlatformAPI.EventsAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EventPlatformAPI.EventsAPI.HostedServices;

public class OutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;
    private readonly TimeSpan _pollPeriod = TimeSpan.FromSeconds(5);
    private readonly int _batchSize = 5;

    public OutboxDispatcherHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox dispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EventsDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();

                var pending = await db.OutboxMessages
                    .Where(x => !x.IsPublished)
                    .OrderBy(x => x.CreatedAtUtc)
                    .Take(_batchSize)
                    .ToListAsync(stoppingToken);

                if (pending.Count == 0)
                {
                    await Task.Delay(_pollPeriod, stoppingToken);
                    continue;
                }

                var publishedIds = new List<long>();

                foreach (var msg in pending)
                {
                    try
                    {
                        await publisher.PublishAsync(msg.Destination, msg.MessageId, msg.Payload, msg.Type, stoppingToken);
                        publishedIds.Add(msg.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish outbox message {OutboxMessageId}", msg.Id);
                    }
                }

                if (publishedIds.Count > 0)
                {
                    var now = DateTime.UtcNow;
                    var items = await db.OutboxMessages
                        .Where(x => publishedIds.Contains(x.Id))
                        .ToListAsync(stoppingToken);

                    foreach (var it in items)
                    {
                        it.IsPublished = true;
                        it.PublishedAtUtc = now;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected outbox dispatcher error");
            }

            await Task.Delay(_pollPeriod, stoppingToken);
        }

        _logger.LogInformation("Outbox dispatcher stopped.");
    }
}


public interface IOutboxPublisher
{
    Task PublishAsync(string destination, Guid messageId, string payload, string type, CancellationToken cancellationToken = default);
}

public class OutboxPublisher : IOutboxPublisher, IAsyncDisposable
{
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly RabbitMqOptions _options;          
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;
    private bool _started;

    public OutboxPublisher(IOptions<RabbitMqOptions> options, ILogger<OutboxPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task PublishAsync(string destination, Guid messageId, string payload, string type, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel is not initialized.");

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

        _logger.LogInformation("Message published to queue {Destination}. MessageId: {MessageId}", destination, messageId);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_started)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_started)
                return;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("RabbitMQ connection initialized for OutboxPublisher.");
            _started = true;
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
                await _channel.DisposeAsync();

            if (_connection is not null)
                await _connection.DisposeAsync();
        }
        catch { }
        finally
        {
            _initLock.Dispose();
        }
    }
}