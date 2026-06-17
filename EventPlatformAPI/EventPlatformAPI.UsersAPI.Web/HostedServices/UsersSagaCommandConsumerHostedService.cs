using EventPlatformAPI.Messages.Saga;
using EventPlatformAPI.Messages.Saga.SagaMessages;
using EventPlatformAPI.UsersAPI.Application.Commands;
using EventPlatformAPI.UsersAPI.Application.Interfaces;
using EventPlatformAPI.UsersAPI.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventPlatformAPI.UsersAPI.Web.HostedServices;

public sealed class UsersSagaCommandConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UsersSagaCommandConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    private static readonly string[] CommandQueues =
    [
        SagaQueues.ConfirmRegistration,
        SagaQueues.CancelRegistration
    ];

    public UsersSagaCommandConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<UsersSagaCommandConsumerHostedService> logger)
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

        foreach (var queue in CommandQueues)
        {
            await _channel.QueueDeclareAsync(
                queue: queue, durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, ea) => await HandleAsync(ea, stoppingToken);

            await _channel.BasicConsumeAsync(
                queue: queue, autoAck: false, consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation("UsersAPI saga consumer listening on queue: {Queue}", queue);
        }

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { }
    }

    private async Task HandleAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        if (_channel is null) return;

        var body = Encoding.UTF8.GetString(ea.Body.ToArray());
        var queue = ea.RoutingKey;

        _logger.LogInformation("[UsersAPI] Received saga command from queue: {Queue}", queue);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

            if (queue == SagaQueues.ConfirmRegistration)
                await HandleConfirmAsync(body, dispatcher, ct);
            else if (queue == SagaQueues.CancelRegistration)
                await HandleCancelAsync(body, dispatcher, ct);

            await _channel.BasicAckAsync(ea.DeliveryTag, false, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UsersAPI] Error processing saga command from queue {Queue}", queue);
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct);
        }
    }

    private async Task HandleConfirmAsync(string body, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var cmd = Deserialize<ConfirmRegistrationSagaCommand>(body);

        _logger.LogInformation(
            "[CorrelationId={CorrelationId}] UsersAPI: ConfirmRegistration. UserId={UserId}, EventId={EventId}",
            cmd.CorrelationId, cmd.UserId, cmd.EventId);

        var applicationCommand = new ConfirmRegistrationCommand(
            cmd.UserId, cmd.EventId, cmd.CorrelationId);

        await dispatcher.Dispatch(applicationCommand, ct);
    }

    private async Task HandleCancelAsync(string body, ICommandDispatcher dispatcher, CancellationToken ct)
    {
        var cmd = Deserialize<CancelRegistrationSagaCommand>(body);

        _logger.LogInformation(
            "[CorrelationId={CorrelationId}] UsersAPI: CancelRegistration. UserId={UserId}, EventId={EventId}, Reason={Reason}",
            cmd.CorrelationId, cmd.UserId, cmd.EventId, cmd.Reason);

        var applicationCommand = new CancelRegistrationCommand(
            cmd.UserId, cmd.EventId, cmd.CorrelationId);

        await dispatcher.Dispatch(applicationCommand, ct);
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
