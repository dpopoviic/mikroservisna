using EventPlatformAPI.UsersAPI.Infrastructure.Data;
using EventPlatformAPI.UsersAPI.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.UsersAPI.Web.HostedServices;

public class OutboxDispatcherHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatcherHostedService> _logger;
    private static readonly TimeSpan PollPeriod = TimeSpan.FromSeconds(5);
    private const int BatchSize = 10;

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
                var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();

                var pending = await db.OutboxMessages
                    .Where(x => !x.IsPublished)
                    .OrderBy(x => x.CreatedAt)
                    .Take(BatchSize)
                    .ToListAsync(stoppingToken);

                if (pending.Count == 0)
                {
                    await Task.Delay(PollPeriod, stoppingToken);
                    continue;
                }

                foreach (var msg in pending)
                {
                    try
                    {
                        await publisher.PublishAsync(msg.Destination, msg.CorrelationId, msg.Payload, msg.Type, stoppingToken);
                        msg.IsPublished = true;
                        msg.ProcessedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        msg.Error = ex.Message;
                        _logger.LogWarning(ex, "Failed to publish outbox message {OutboxMessageId}", msg.Id);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected outbox dispatcher error");
            }

            await Task.Delay(PollPeriod, stoppingToken);
        }

        _logger.LogInformation("Outbox dispatcher stopped.");
    }
}
