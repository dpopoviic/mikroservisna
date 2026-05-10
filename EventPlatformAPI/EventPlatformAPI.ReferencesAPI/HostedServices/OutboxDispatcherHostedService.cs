using EventPlatformAPI.ReferencesAPI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EventPlatformAPI.ReferencesAPI.HostedServices
{
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
                    var repo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                    var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqPublisher>();

                    var pending = await repo.GetUnpublishedAsync(_batchSize, stoppingToken);
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
                        await repo.MarkPublishedAsync(publishedIds, stoppingToken);
                    }
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
}
