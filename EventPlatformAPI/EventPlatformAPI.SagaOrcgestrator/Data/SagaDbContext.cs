using EventPlatformAPI.SagaOrcgestrator.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.SagaOrcgestrator.Data
{
    public class SagaDbContext : DbContext
    {
        public SagaDbContext(DbContextOptions<SagaDbContext> options) : base(options) { }

        public DbSet<RegistrationSagaState> RegistrationSagaStates => Set<RegistrationSagaState>();
        public DbSet<SagaOutboxMessage> SagaOutboxMessages => Set<SagaOutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RegistrationSagaState>(e =>
            {
                e.ToTable("RegistrationSagaStates");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.CorrelationId).IsRequired();
                e.Property(x => x.UserId).IsRequired();
                e.Property(x => x.EventId).IsRequired();
                e.Property(x => x.UserEmail).HasMaxLength(500);
                e.Property(x => x.UserFirstName).HasMaxLength(200);
                e.Property(x => x.UserLastName).HasMaxLength(200);
                e.Property(x => x.Status).HasConversion<string>().HasMaxLength(100).IsRequired();
                e.Property(x => x.FailureReason).HasMaxLength(2000);
                e.HasIndex(x => x.CorrelationId).IsUnique();
            });

            modelBuilder.Entity<SagaOutboxMessage>(e =>
            {
                e.ToTable("SagaOutboxMessages");
                e.HasKey(x => x.Id);
                e.Property(x => x.MessageId).IsRequired();
                e.Property(x => x.CorrelationId).IsRequired();
                e.Property(x => x.Type).HasMaxLength(200).IsRequired();
                e.Property(x => x.Destination).HasMaxLength(200).IsRequired();
                e.Property(x => x.Payload).IsRequired();
                e.HasIndex(x => new { x.IsPublished, x.CreatedAt });
            });
        }
    }
}
