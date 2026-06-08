using EventPlatformAPI.SagaOrcgestrator.Models;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.SagaOrcgestrator.Data;

public class SagaOrchestratorDbContext : DbContext
{
    public SagaOrchestratorDbContext(DbContextOptions<SagaOrchestratorDbContext> options) : base(options)
    {
    }

    public DbSet<PublishEventSaga> PublishEventSagas => Set<PublishEventSaga>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PublishEventSaga>(entity =>
        {
            entity.ToTable("PublishEventSagas");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(50);
            entity.Property(x => x.PayloadJson).IsRequired();
            entity.Property(x => x.FailureReason).HasMaxLength(1000);
            entity.HasIndex(x => x.CorrelationId).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Destination).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Type).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Payload).IsRequired();
            entity.HasIndex(x => new { x.IsPublished, x.CreatedAtUtc });
        });

        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.ToTable("ProcessedMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventId).IsRequired();
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(200);
            entity.HasIndex(x => x.EventId).IsUnique();
        });
    }
}