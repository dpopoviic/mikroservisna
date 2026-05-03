using EventPlatformAPI.EventsAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.EventsAPI.Data;

public class EventsDbContext : DbContext
{
    public EventsDbContext(DbContextOptions<EventsDbContext> options) : base(options)
    {
    }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<EventLecturer> EventLecturers => Set<EventLecturer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.ToTable("EventTypes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(1000);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Agenda).HasMaxLength(4000);
            entity.Property(x => x.DurationInHours).HasPrecision(5, 2);
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.Type)
                .WithMany(x => x.Events)
                .HasForeignKey(x => x.TypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventLecturer>(entity =>
        {
            entity.ToTable("EventLecturers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Theme).IsRequired().HasMaxLength(300);
            entity.HasIndex(x => new { x.EventId, x.LecturerId, x.DateTime }).IsUnique();

            entity.HasOne(x => x.Event)
                .WithMany(x => x.EventLecturers)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
