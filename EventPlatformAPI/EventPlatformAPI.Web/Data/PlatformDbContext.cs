using EventPlatformAPI.Web.Domains;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.Web.Data
{
    public class PlatformDbContext : DbContext
    {
        public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options)
        {
        }

        protected PlatformDbContext()
        {
        }

        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventType> EventTypes => Set<EventType>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<Lecturer> Lecturers => Set<Lecturer>();
        public DbSet<EventLecturer> EventLecturers => Set<EventLecturer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EventType>(entity =>
            {
                entity.ToTable("Types");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(300);
            });

            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.ToTable("Lecturers");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).HasMaxLength(100);
                entity.Property(e => e.Field).HasMaxLength(200);
            });

            modelBuilder.Entity<Event>(entity =>
            {
                entity.ToTable("Events");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Agenda).HasMaxLength(4000);
                entity.Property(e => e.DurationInHours).HasPrecision(5, 2);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Type)
                    .WithMany(t => t.Events)
                    .HasForeignKey(e => e.TypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.Events)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<EventLecturer>(entity =>
            {
                entity.ToTable("EventLecturers");

                entity.HasKey(el => el.Id);
                entity.HasIndex(el => new { el.EventId, el.LecturerId, el.DateTime }).IsUnique();
                entity.Property(el => el.Theme).IsRequired().HasMaxLength(300);

                entity.HasOne(el => el.Event)
                    .WithMany(e => e.EventLecturers)
                    .HasForeignKey(el => el.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(el => el.Lecturer)
                    .WithMany(l => l.EventLecturers)
                    .HasForeignKey(el => el.LecturerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
