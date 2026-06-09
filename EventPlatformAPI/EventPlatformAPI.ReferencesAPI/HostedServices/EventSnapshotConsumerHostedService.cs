using EventPlatformAPI.Messages.IntegrationEvents;
using EventPlatformAPI.ReferencesAPI.Data;
using EventPlatformAPI.ReferencesAPI.Models;
using EventPlatformAPI.ReferencesAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.ReferencesAPI.HostedServices;

/// <summary>
/// Keeps ReferencesAPI EventSnapshot table up to date by consuming
/// EventPublishedSnapshotEvent messages from EventsAPI.
/// Required for the Registration choreography saga validation step.
/// </summary>
public sealed class EventSnapshotConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventSnapshotConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string InboundQueue = "references.event-snapshot.event";

    public EventSnapshotConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<EventSnapshotConsumerHostedService> logger)
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

        await _channel.QueueDeclareAsync(InboundQueue, durable: true,
            exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(InboundQueue, autoAck: false,
            consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("[REFERENCESAPI] EventSnapshotConsumer started.");

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (_channel is null) return;

        var payload = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();

            var evt = JsonSerializer.Deserialize<EventPublishedSnapshotEvent>(payload);
            if (evt is null)
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
                return;
            }

            var existing = await db.EventSnapshots
                .FirstOrDefaultAsync(s => s.ExternalEventId == evt.EventId, ct);

            if (existing is null)
            {
                db.EventSnapshots.Add(new EventSnapshot
                {
                    ExternalEventId = evt.EventId,
                    IsPublished = evt.IsPublished,
                    EventDate = evt.EventDate,
                    UpdatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                existing.IsPublished = evt.IsPublished;
                existing.EventDate = evt.EventDate;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
            await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);

            _logger.LogInformation("[REFERENCESAPI] EventSnapshot updated for EventId={EventId}", evt.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REFERENCESAPI] Failed to update EventSnapshot.");
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
