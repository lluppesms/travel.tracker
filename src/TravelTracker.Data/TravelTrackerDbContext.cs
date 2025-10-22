using Microsoft.EntityFrameworkCore;
using TravelTracker.Data.Models;
using System.Text.Json;

namespace TravelTracker.Data;

public class TravelTrackerDbContext : DbContext
{
    public TravelTrackerDbContext(DbContextOptions<TravelTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<NationalPark> NationalParks { get; set; }
    public DbSet<LocationType> LocationTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntraIdUserId).IsUnique();
            entity.HasIndex(e => e.Email);
        });

        // Configure Location entity
        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.State);
            entity.HasIndex(e => e.StartDate);
            
            // Handle Tags list as JSON
            entity.Property(e => e.TagsJson)
                .HasDefaultValue("[]");
                
            entity.Ignore(e => e.Tags);
        });

        // Configure NationalPark entity
        modelBuilder.Entity<NationalPark>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.State);
            entity.HasIndex(e => e.Name);
        });

        // Configure LocationType entity
        modelBuilder.Entity<LocationType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
            
            // Seed initial location types
            entity.HasData(
                new LocationType { Id = 1, Name = "National Park", Description = "US National Park" },
                new LocationType { Id = 2, Name = "Hotel", Description = "Hotel or lodging" },
                new LocationType { Id = 3, Name = "Restaurant", Description = "Restaurant or dining" },
                new LocationType { Id = 4, Name = "Museum", Description = "Museum or cultural site" },
                new LocationType { Id = 5, Name = "Beach", Description = "Beach or coastal area" },
                new LocationType { Id = 6, Name = "City", Description = "City or town" },
                new LocationType { Id = 7, Name = "Other", Description = "Other location type" }
            );
        });
    }

    public override int SaveChanges()
    {
        UpdateLocationTags();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateLocationTags();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateLocationTags()
    {
        var entries = ChangeTracker.Entries<Location>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Entity.Tags != null && entry.Entity.Tags.Any())
            {
                entry.Entity.TagsJson = JsonSerializer.Serialize(entry.Entity.Tags);
            }
            else if (entry.Entity.Tags != null && !entry.Entity.Tags.Any())
            {
                entry.Entity.TagsJson = "[]";
            }
        }
    }
}
