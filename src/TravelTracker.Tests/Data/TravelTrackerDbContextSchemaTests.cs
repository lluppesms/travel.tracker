using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TravelTracker.Data;
using TravelTracker.Data.Models;

namespace TravelTracker.Tests.Data;

public class TravelTrackerDbContextSchemaTests
{
    [Fact]
    public void Model_Maps_All_Entities_To_Travel_Schema()
    {
        var options = new DbContextOptionsBuilder<TravelTrackerDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TravelTrackerSchemaTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var context = new TravelTrackerDbContext(options);

        AssertEntitySchema<User>(context);
        AssertEntitySchema<Location>(context);
        AssertEntitySchema<LocationType>(context);
        AssertEntitySchema<Destination>(context);
        AssertEntitySchema<DestinationType>(context);
    }

    private static void AssertEntitySchema<TEntity>(TravelTrackerDbContext context)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));

        Assert.NotNull(entityType);
        Assert.Equal(DatabaseSchema.Name, entityType!.GetSchema());
    }
}
