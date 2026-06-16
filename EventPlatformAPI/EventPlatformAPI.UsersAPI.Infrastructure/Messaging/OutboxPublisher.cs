using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace EventPlatformAPI.UsersAPI.Infrastructure.Messaging
{
    public sealed class OutboxPublisher : IOutboxPublisher, IAsyncDisposable
    {
        private readonly RabbitMqOptions _options;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private IConnection? _connection;
        private IChannel? _channel;
        private bool _started;

        public OutboxPublisher(IOptions<RabbitMqOptions> options)
        {
            _options = options.Value;
        }

        public async Task PublishAsync(
            string destination, Guid messageId, string payload,
            string type, CancellationToken ct)
        {
            await EnsureInitializedAsync(ct);

            await _channel!.QueueDeclareAsync(
                destination, durable: true, exclusive: false, autoDelete: false,
                arguments: null, cancellationToken: ct);

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
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: ct);
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
