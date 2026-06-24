using Microsoft.EntityFrameworkCore;
using TravelTracker.Data.Models;

namespace TravelTracker.Data;

public class TravelTrackerDbContext : DbContext
{
    public TravelTrackerDbContext(DbContextOptions<TravelTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<LocationType> LocationTypes { get; set; }
    public DbSet<Destination> Destinations { get; set; }
    public DbSet<DestinationType> DestinationTypes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DatabaseSchema.Name);
    }
}
