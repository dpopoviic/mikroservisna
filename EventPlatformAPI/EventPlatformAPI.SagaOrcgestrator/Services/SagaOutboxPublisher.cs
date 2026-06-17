using EventPlatformAPI.SagaOrcgestrator.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.SagaOrcgestrator.Services
{
    public sealed class SagaOutboxPublisher : ISagaOutboxPublisher, IAsyncDisposable
    {
        private readonly SagaRabbitMqOptions _options;
        private readonly ILogger<SagaOutboxPublisher> _logger;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private IConnection? _connection;
        private IChannel? _channel;
        private bool _started;

        public SagaOutboxPublisher(
            IOptions<SagaRabbitMqOptions> options,
            ILogger<SagaOutboxPublisher> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task PublishAsync(
            string destination, Guid messageId, string payload,
            string messageType, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct);

            var body = Encoding.UTF8.GetBytes(payload);
            var props = new BasicProperties
            {
                Persistent = true,
                MessageId = messageId.ToString(),
                Type = messageType,
                ContentType = "application/json"
            };

            await _channel!.QueueDeclareAsync(
                destination, durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: ct);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: destination,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            _logger.LogInformation(
                "Saga outbox published MessageId={MessageId} Type={Type} → {Destination}",
                messageId, messageType, destination);
        }

        private async Task EnsureInitializedAsync(CancellationToken ct)
        {
            if (_started) return;

            await _initLock.WaitAsync(ct);
            try
            {
                if (_started) return;

                var factory = new ConnectionFactory
                {
                    HostName = _options.HostName,
                    Port = _options.Port,
                    UserName = _options.UserName,
                    Password = _options.Password
                };

                _connection = await factory.CreateConnectionAsync(ct);
                _channel = await _connection.CreateChannelAsync(cancellationToken: ct);
                _started = true;

                _logger.LogInformation("SagaOutboxPublisher: RabbitMQ connection initialized.");
            }
            finally { _initLock.Release(); }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_channel is not null) await _channel.DisposeAsync();
                if (_connection is not null) await _connection.DisposeAsync();
            }
            catch { }
            finally { _initLock.Dispose(); }
        }
    }
}
