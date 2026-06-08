using EventPlatformAPI.SagaOrcgestrator.Data;
using EventPlatformAPI.SagaOrcgestrator.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventPlatformAPI.SagaOrcgestrator.HostedServices;

public class OutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;
    private readonly TimeSpan _pollPeriod = TimeSpan.FromSeconds(3);
    private readonly int _batchSize = 10;

    public OutboxDispatcherHostedService(IServiceScopeFactory scopeFactory, ILogger<OutboxDispatcherHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Saga outbox dispatcher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SagaOrchestratorDbContext>();
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
                    var items = await db.OutboxMessages.Where(x => publishedIds.Contains(x.Id)).ToListAsync(stoppingToken);
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
                _logger.LogError(ex, "Unexpected error in saga outbox dispatcher.");
            }

            await Task.Delay(_pollPeriod, stoppingToken);
        }

        _logger.LogInformation("Saga outbox dispatcher stopped.");
    }
}