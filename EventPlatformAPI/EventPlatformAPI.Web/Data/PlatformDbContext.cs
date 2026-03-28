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

                entity.HasData(
                    new EventType { Id = 1, Name = "Konferencija", Description = "Stručna konferencija" },
                    new EventType { Id = 2, Name = "Seminar", Description = "Edukativni seminar" },
                    new EventType { Id = 3, Name = "Radionica", Description = "Praktična radionica" });
            });

            modelBuilder.Entity<Location>(entity =>
            {
                entity.ToTable("Locations");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Address).IsRequired().HasMaxLength(300);

                entity.HasData(
                    new Location { Id = 1, Name = "Amfiteatar A", Address = "Bulevar kralja Aleksandra 73", Capacity = 200 },
                    new Location { Id = 2, Name = "Sala 101", Address = "Kraljice Marije 16", Capacity = 80 });
            });

            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.ToTable("Lecturers");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Title).HasMaxLength(100);
                entity.Property(e => e.Field).HasMaxLength(200);

                entity.HasData(
                    new Lecturer { Id = 1, FirstName = "Milan", LastName = "Petrović", Title = "Prof. dr", Field = "Softversko inženjerstvo" },
                    new Lecturer { Id = 2, FirstName = "Jelena", LastName = "Jovanović", Title = "Doc. dr", Field = "Baze podataka" });
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

                entity.HasData(
                    new Event
                    {
                        Id = 1,
                        Name = "Savremene .NET tehnologije",
                        DateTime = new DateTime(2026, 6, 10, 9, 0, 0),
                        DurationInHours = 6.00m,
                        Price = 3500.00m,
                        Agenda = "Pregled novina u .NET platformi",
                        TypeId = 1,
                        LocationId = 1
                    },
                    new Event
                    {
                        Id = 2,
                        Name = "Uvod u mikroservise",
                        DateTime = new DateTime(2026, 6, 20, 10, 0, 0),
                        DurationInHours = 4.00m,
                        Price = 2500.00m,
                        Agenda = "Osnovni principi mikroservisne arhitekture",
                        TypeId = 2,
                        LocationId = 2
                    });
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

                entity.HasData(
                    new EventLecturer
                    {
                        Id = 1,
                        EventId = 1,
                        LecturerId = 1,
                        DateTime = new DateTime(2026, 6, 10, 9, 0, 0),
                        Theme = "Arhitektura modernih .NET aplikacija"
                    },
                    new EventLecturer
                    {
                        Id = 2,
                        EventId = 1,
                        LecturerId = 1,
                        DateTime = new DateTime(2026, 6, 10, 10, 0, 0),
                        Theme = "Performanse i optimizacija"
                    },
                    new EventLecturer
                    {
                        Id = 3,
                        EventId = 2,
                        LecturerId = 2,
                        DateTime = new DateTime(2026, 6, 20, 10, 0, 0),
                        Theme = "Modelovanje servisa i baza"
                    });
            });
        }
    }
}
