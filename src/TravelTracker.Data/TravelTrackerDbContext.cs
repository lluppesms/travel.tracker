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
    public DbSet<AssistantAction> AssistantActions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(DatabaseSchema.Name);

        modelBuilder.Entity<AssistantAction>(entity =>
        {
            entity.Property(action => action.CanonicalIdempotencyKey).HasColumnType("char(64)");
            entity.Property(action => action.PayloadHashSha256).HasColumnType("binary(32)");
            entity.Property(action => action.RowVersion).IsRowVersion();
            entity.HasIndex(action => new
                {
                    action.UserId,
                    action.ThreadId,
                    action.CanonicalIdempotencyKey
                })
                .IsUnique();
            entity.HasIndex(action => new { action.UserId, action.State, action.ExpiresAt });
            entity.HasIndex(action => new { action.State, action.RetainUntilDate });
        });

        modelBuilder.Entity<Location>(entity =>
        {
            entity.HasIndex(location => location.AssistantActionId)
                .IsUnique()
                .HasFilter("[AssistantActionId] IS NOT NULL");
            entity.HasIndex(location => new { location.UserId, location.StartDate, location.Name });
            entity.HasOne(location => location.AssistantAction)
                .WithOne(action => action.Location)
                .HasForeignKey<Location>(location => location.AssistantActionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
