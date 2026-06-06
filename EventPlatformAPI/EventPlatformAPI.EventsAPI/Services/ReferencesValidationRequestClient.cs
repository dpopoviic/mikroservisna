using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using EventPlatformAPI.Messages.Requests;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventPlatformAPI.EventsAPI.Services;

public sealed class ReferencesValidationRequestClient : IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<ReferencesValidationRequestClient> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ValidateReferencesResponse>> _pending = new();

    private IConnection? _connection;
    private IChannel? _publishChannel;
    private IChannel? _consumerChannel;
    private bool _started;

    public ReferencesValidationRequestClient(IOptions<RabbitMqOptions> options, ILogger<ReferencesValidationRequestClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ValidateReferencesResponse> SendValidateRequestAsync(ValidateReferencesRequest request, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        await EnsureStartedAsync(cancellationToken);

        if (_publishChannel is null)
        {
            throw new InvalidOperationException("RabbitMQ publish channel is not initialized.");
        }

        request.CorrelationId = Guid.NewGuid();
        var correlationId = request.CorrelationId.ToString("N");
        var tcs = new TaskCompletionSource<ValidateReferencesResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pending.TryAdd(correlationId, tcs))
        {
            throw new InvalidOperationException("Unable to register pending validation request.");
        }

        try
        {
            var json = JsonSerializer.Serialize(request);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                CorrelationId = correlationId,
                ReplyTo = _options.ValidationReplyQueue,
                ContentType = "application/json"
            };

            await _publishChannel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _options.ValidationRequestQueue,
                mandatory: false,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);

            return await tcs.Task.WaitAsync(timeout, cancellationToken);
        }
        catch
        {
            _pending.TryRemove(correlationId, out _);
            throw;
        }
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_started)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_started)
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
            _publishChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            _consumerChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _consumerChannel.QueueDeclareAsync(
                queue: _options.ValidationReplyQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
            consumer.ReceivedAsync += HandleReplyAsync;

            await _consumerChannel.BasicConsumeAsync(
                queue: _options.ValidationReplyQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: cancellationToken);

            _started = true;
            _logger.LogInformation("Reply consumer started on queue {Queue}", _options.ValidationReplyQueue);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task HandleReplyAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_consumerChannel is null)
        {
            return;
        }

        try
        {
            var correlationId = ea.BasicProperties?.CorrelationId;
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                await _consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var response = JsonSerializer.Deserialize<ValidateReferencesResponse>(body);
            if (response is null)
            {
                await _consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            if (_pending.TryRemove(correlationId, out var tcs))
            {
                tcs.TrySetResult(response);
            }

            await _consumerChannel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing validation reply");
            try
            {
                await _consumerChannel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
            catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_consumerChannel is not null)
            {
                await _consumerChannel.DisposeAsync();
            }

            if (_publishChannel is not null)
            {
                await _publishChannel.DisposeAsync();
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }
        catch { }
        finally
        {
            _initLock.Dispose();
        }
    }
}
