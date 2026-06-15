using EventPlatformAPI.SagaOrcgestrator.Data;
using EventPlatformAPI.SagaOrcgestrator.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPlatformAPI.SagaOrcgestrator.HostedServices
{
    public class SagaOutboxDispatcherBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SagaOutboxDispatcherBackgroundService> _logger;
        private readonly TimeSpan _pollPeriod = TimeSpan.FromSeconds(5);
        private const int BatchSize = 10;

        public SagaOutboxDispatcherBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SagaOutboxDispatcherBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SagaOutboxDispatcher started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<SagaDbContext>();
                    var publisher = scope.ServiceProvider.GetRequiredService<ISagaOutboxPublisher>();

                    var pending = await db.SagaOutboxMessages
                        .Where(x => !x.IsPublished)
                        .OrderBy(x => x.CreatedAt)
                        .Take(BatchSize)
                        .ToListAsync(stoppingToken);

                    if (pending.Count == 0)
                    {
                        await Task.Delay(_pollPeriod, stoppingToken);
                        continue;
                    }

                    foreach (var msg in pending)
                    {
                        try
                        {
                            await publisher.PublishAsync(
                                msg.Destination, msg.MessageId,
                                msg.Payload, msg.Type, stoppingToken);

                            msg.IsPublished = true;
                            msg.ProcessedAt = DateTime.UtcNow;

                            _logger.LogInformation(
                                "[CorrelationId={CorrelationId}] Outbox published: {Type} → {Destination}",
                                msg.CorrelationId, msg.Type, msg.Destination);
                        }
                        catch (Exception ex)
                        {
                            msg.Error = ex.Message;
                            _logger.LogWarning(ex,
                                "[CorrelationId={CorrelationId}] Failed to publish outbox message {Id}",
                                msg.CorrelationId, msg.Id);
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected saga outbox dispatcher error.");
                }

                await Task.Delay(_pollPeriod, stoppingToken);
            }

            _logger.LogInformation("SagaOutboxDispatcher stopped.");
        }
    }
}
