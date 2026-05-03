using EventPlatformAPI.ReferencesAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EventPlatformAPI.ReferencesAPI.Data;

public class ReferenceDbContext : DbContext
{
    public ReferenceDbContext(DbContextOptions<ReferenceDbContext> options) : base(options)
    {
    }

    public DbSet<Lecturer> Lecturers => Set<Lecturer>();
    public DbSet<Location> Locations => Set<Location>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lecturer>(entity =>
        {
            entity.ToTable("Lecturers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.LastName).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Title).HasMaxLength(100);
            entity.Property(x => x.Field).HasMaxLength(200);

            entity.HasData(
                new Lecturer { Id = 1, FirstName = "Milan", LastName = "Petrovi?", Title = "Prof. dr", Field = "Softversko inženjerstvo" },
                new Lecturer { Id = 2, FirstName = "Jelena", LastName = "Jovanovi?", Title = "Doc. dr", Field = "Baze podataka" });
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("Locations");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Address).IsRequired().HasMaxLength(300);

            entity.HasData(
                new Location { Id = 1, Name = "Amfiteatar A", Address = "Bulevar kralja Aleksandra 73", Capacity = 200 },
                new Location { Id = 2, Name = "Sala 101", Address = "Kraljice Marije 16", Capacity = 80 });
        });
    }
}
