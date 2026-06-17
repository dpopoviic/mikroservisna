using EventPlatformAPI.UsersAPI.Domains.Entities;
using EventPlatformAPI.UsersAPI.Domains.Outbox;
using EventPlatformAPI.UsersAPI.Infrastructure.ReadModels;
using EventPlatformAPI.UsersAPI.Infrastructure.Snapshots;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.UsersAPI.Infrastructure.Data
{
    public class UsersDbContext : DbContext
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

        public DbSet<EventStoreRecord> EventStore { get; set; }
        public DbSet<Snapshot> Snapshots { get; set; }

        public DbSet<UserReadModel> Users { get; set; }
        public DbSet<RegistrationReadModel> Registrations { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        public DbSet<ChoreographyProcessState> ChoreographyProcessStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventStoreRecord>(e =>
            {
                e.ToTable("EventStore");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.AggregateId).IsRequired();
                e.Property(x => x.AggregateType).HasMaxLength(200).IsRequired();
                e.Property(x => x.EventType).HasMaxLength(200).IsRequired();
                e.Property(x => x.EventData).IsRequired();
                e.Property(x => x.Version).IsRequired();
                e.Property(x => x.CorrelationId).IsRequired();
                e.Property(x => x.OccurredOn).IsRequired();

                e.HasIndex(x => new { x.AggregateId, x.Version }).IsUnique();
                e.HasIndex(x => x.AggregateId);
            });

            modelBuilder.Entity<Snapshot>(e =>
            {
                e.ToTable("Snapshots");
                e.HasKey(x => x.AggregateId);  
                e.Property(x => x.AggregateType).HasMaxLength(200).IsRequired();
                e.Property(x => x.SnapshotData).IsRequired();
                e.Property(x => x.Version).IsRequired();
                e.Property(x => x.CreatedAt).IsRequired();
            });

            modelBuilder.Entity<UserReadModel>(e =>
            {
                e.ToTable("Users");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.FirstName).HasMaxLength(200).IsRequired();
                e.Property(x => x.LastName).HasMaxLength(200).IsRequired();
                e.Property(x => x.Email).HasMaxLength(500).IsRequired();
                e.HasIndex(x => x.Email).IsUnique();
            });

            modelBuilder.Entity<RegistrationReadModel>(e =>
            {
                e.ToTable("Registrations");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.UserId).IsRequired();
                e.Property(x => x.EventId).IsRequired();
                e.Property(x => x.Status).HasMaxLength(50).IsRequired();
                e.Property(x => x.UserFirstName).HasMaxLength(200);
                e.Property(x => x.UserLastName).HasMaxLength(200);
                e.Property(x => x.UserEmail).HasMaxLength(500);

                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.EventId);
                e.HasIndex(x => new { x.UserId, x.EventId });
            });

            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.ToTable("OutboxMessages");
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Type).IsRequired().HasMaxLength(256);
                entity.Property(o => o.Destination).IsRequired().HasMaxLength(256);
                entity.Property(o => o.Payload).IsRequired();
                entity.HasIndex(o => new { o.IsPublished, o.CreatedAt });
            });

            modelBuilder.Entity<ChoreographyProcessState>(entity =>
            {
                entity.ToTable("ChoreographyProcessStates");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.CorrelationId).IsRequired();
                entity.Property(x => x.EventName).HasMaxLength(256).IsRequired();
                entity.Property(x => x.ServiceName).HasMaxLength(128).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(64).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();
                entity.HasIndex(x => x.CorrelationId);
            });
        }

    }
}
