using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.Choreography;
using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Domains.Outbox;
using EventPlatformAPI.UsersAPI.Infrastructure.Data;
using EventPlatformAPI.UsersAPI.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.UsersAPI.Web.HostedServices;

public sealed class ChoreographyEventConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChoreographyEventConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    private static readonly string[] EventQueues =
    [
        SagaQueues.ChoreographyEventSeatReleaseFailed
    ];

    public ChoreographyEventConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<ChoreographyEventConsumerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
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

        await _channel.BasicQosAsync(0, 1, false, stoppingToken);

        foreach (var queue in EventQueues)
        {
            await _channel.QueueDeclareAsync(
                queue: queue, durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                queue: queue, autoAck: false, consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("UsersAPI choreography consumer listening on queue: {Queue}", queue);
        }

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
    }

    private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (_channel is null) return;

        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
        var queue = ea.RoutingKey;

        _logger.LogInformation("[UsersAPI] Received choreography event from queue: {Queue}", queue);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserWriteRepository>();

            if (queue == SagaQueues.ChoreographyEventSeatReleaseFailed)
                await HandleEventSeatReleaseFailedAsync(body, db, userRepository, ct);

            await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UsersAPI] Error processing choreography event from queue {Queue}", queue);
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
        }
    }

    private async Task HandleEventSeatReleaseFailedAsync(
        string body,
        UsersDbContext db,
        IUserWriteRepository userRepository,
        CancellationToken ct)
    {
        var evt = Deserialize<EventSeatReleaseFailedEvent>(body);

        _logger.LogInformation(
            "[CorrelationId={CorrelationId}] UsersAPI: EventSeatReleaseFailed received. EventId={EventId}, Reason={Reason}",
            evt.CorrelationId, evt.EventId, evt.Reason);

        var registration = await db.Registrations
            .FirstOrDefaultAsync(r => r.EventId == evt.EventId, ct);

        if (registration is null)
        {
            _logger.LogWarning(
                "[CorrelationId={CorrelationId}] No registration found for EventId={EventId}. Cannot compensate.",
                evt.CorrelationId, evt.EventId);
            return;
        }

        var aggregate = await userRepository.LoadAsync(registration.UserId, ct);
        if (aggregate is null)
        {
            _logger.LogWarning(
                "[CorrelationId={CorrelationId}] User {UserId} not found for EventId={EventId}. Cannot compensate.",
                evt.CorrelationId, registration.UserId, evt.EventId);
            return;
        }

        aggregate.RestoreCancelledRegistration(evt.EventId, evt.CorrelationId);
        await userRepository.SaveAsync(aggregate, ct);

        db.ChoreographyProcessStates.Add(new ChoreographyProcessState
        {
            CorrelationId = evt.CorrelationId,
            EventName = nameof(EventSeatReleaseFailedEvent),
            ServiceName = "UsersAPI",
            Status = "Compensated",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "[CorrelationId={CorrelationId}] Compensation completed: registration restored for EventId={EventId}.",
            evt.CorrelationId, evt.EventId);
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json)
           ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}.");

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
