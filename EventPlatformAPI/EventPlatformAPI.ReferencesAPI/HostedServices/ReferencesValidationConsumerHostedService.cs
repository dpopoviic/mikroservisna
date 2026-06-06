using System.Text;
using System.Text.Json;
using EventPlatformAPI.Messages.Requests;
using EventPlatformAPI.ReferencesAPI.Data;
using EventPlatformAPI.ReferencesAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventPlatformAPI.ReferencesAPI.HostedServices;

public sealed class ReferencesValidationConsumerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReferencesValidationConsumerHostedService> _logger;
    private readonly RabbitMqOptions _options;

    private IConnection? _connection;
    private IChannel? _channel;

    public ReferencesValidationConsumerHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<ReferencesValidationConsumerHostedService> logger)
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

        await _channel.QueueDeclareAsync(
            queue: _options.ValidationRequestQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleRequestAsync(ea, stoppingToken);

        await _channel.BasicConsumeAsync(
            queue: _options.ValidationRequestQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Validation consumer listening on queue {Queue}", _options.ValidationRequestQueue);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleRequestAsync(BasicDeliverEventArgs ea, CancellationToken cancellationToken)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var request = JsonSerializer.Deserialize<ValidateReferencesRequest>(body);
            if (request is null)
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReferenceDbContext>();

            var locationExists = !request.LocationId.HasValue || request.LocationId.Value <= 0
                ? true
                : await db.Locations.AnyAsync(x => x.Id == request.LocationId.Value, cancellationToken);
            var lecturerIds = request.LecturerIds ?? [];
            var missingLecturers = lecturerIds.Count == 0
                ? []
                : await db.Lecturers
                    .Where(x => lecturerIds.Contains(x.Id))
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

            var missingLecturerIds = lecturerIds.Except(missingLecturers).ToList();
            var isValid = locationExists && missingLecturerIds.Count == 0;
            var reason = isValid
                ? null
                : !locationExists
                    ? $"Location {request.LocationId} does not exist."
                    : $"Some lecturers do not exist: {string.Join(", ", missingLecturerIds)}";

            var response = new ValidateReferencesResponse
            {
                CorrelationId = request.CorrelationId,
                IsValid = isValid,
                Reason = reason
            };

            if (!string.IsNullOrWhiteSpace(ea.BasicProperties.ReplyTo))
            {
                var responseJson = JsonSerializer.Serialize(response);
                var responseBody = Encoding.UTF8.GetBytes(responseJson);

                var properties = new BasicProperties
                {
                    Persistent = true,
                    CorrelationId = ea.BasicProperties.CorrelationId,
                    ContentType = "application/json"
                };

                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: ea.BasicProperties.ReplyTo,
                    mandatory: false,
                    basicProperties: properties,
                    body: responseBody,
                    cancellationToken: cancellationToken);
            }
            else
            {
                _logger.LogWarning("Validation request missing ReplyTo. CorrelationId={CorrelationId}", request.CorrelationId);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing validation request. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
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
