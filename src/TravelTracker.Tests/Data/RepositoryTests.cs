using Microsoft.EntityFrameworkCore;
using TravelTracker.Data;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;

namespace TravelTracker.Tests.Data;

public class RepositoryTests
{
    [Fact]
    public async Task DestinationRepositories_FilterAndFindRecords()
    {
        await using var context = CreateContext();
        var nationalPark = new DestinationType { Id = 1, Name = "National Park" };
        var statePark = new DestinationType { Id = 2, Name = "State Park" };
        context.DestinationTypes.AddRange(nationalPark, statePark);
        context.Destinations.AddRange(
            new Destination { Id = 1, DestinationTypeId = 1, Name = "Yellowstone", State = "WY", DestinationType = nationalPark },
            new Destination { Id = 2, DestinationTypeId = 2, Name = "Redwood", State = "CA", DestinationType = statePark });
        await context.SaveChangesAsync();
        var destinations = new DestinationRepository(context);
        var types = new DestinationTypeRepository(context);

        Assert.Equal(2, (await destinations.GetAllAsync()).Count());
        Assert.Equal("Yellowstone", (await destinations.GetByIdAsync(1))!.Name);
        Assert.Single(await destinations.GetByStateAsync("CA"));
        Assert.Single(await destinations.GetByDestinationTypeIdAsync(1));
        Assert.Single(await destinations.GetByDestinationTypeNameAsync("National Park"));
        Assert.Equal(2, (await types.GetAllAsync()).Count());
        Assert.Equal("State Park", (await types.GetByIdAsync(2))!.Name);
        Assert.Null(await types.GetByNameAsync("Missing"));
    }

    [Fact]
    public async Task UserRepository_CanQueryCreateAndUpdateUsers()
    {
        await using var context = CreateContext();
        var repository = new UserRepository(context);
        var user = await repository.CreateAsync(new User { Id = 1, Username = "Ada", Email = "ada@example.com", EntraIdUserId = "entra", ApiKey = "key" });

        Assert.Equal(user, await repository.GetByIdAsync(1));
        Assert.Equal(user, await repository.GetByEntraIdAsync("entra"));
        Assert.Equal(user, await repository.GetByApiKeyAsync("key"));

        user.Email = "updated@example.com";
        await repository.UpdateAsync(user);

        Assert.Equal("updated@example.com", (await repository.GetByIdAsync(1))!.Email);
        Assert.Null(await repository.GetByIdAsync(99));
    }

    [Fact]
    public async Task LocationRepository_FiltersDeserializesAndSearches()
    {
        await using var context = CreateContext();
        context.Locations.AddRange(
            Location(1, 7, "Yellowstone", "WY", new DateTime(2024, 1, 10), "[\"park\",\"trip\"]"),
            Location(2, 7, "Redwood", "CA", new DateTime(2024, 2, 10), "invalid-json"),
            Location(3, 8, "Yellowstone", "WY", new DateTime(2024, 3, 10), "[]"));
        await context.SaveChangesAsync();
        var repository = new LocationRepository(context);

        var all = (await repository.GetAllByUserIdAsync(7)).ToList();
        Assert.Equal(2, all.Count);
        Assert.Equal(["park", "trip"], all[0].Tags);
        Assert.Empty(all[1].Tags);
        Assert.Single(await repository.GetByStateAsync(7, "CA"));
        Assert.Single(await repository.GetByDateRangeAsync(7, new DateTime(2024, 1, 1), new DateTime(2024, 1, 31)));
        Assert.Equal("Yellowstone", (await repository.GetByIdAsync(1, 7))!.Name);
        Assert.Null(await repository.GetByIdAsync(1, 8));

        var search = await repository.SearchForAssistantAsync(7, "Yellowstone", 10);
        Assert.Single(search);
        Assert.Equal("Yellowstone", search[0].Name);
    }

    [Fact]
    public async Task LocationRepository_FindsDuplicatesAndHandlesCrud()
    {
        await using var context = CreateContext();
        var repository = new LocationRepository(context);
        var created = await repository.CreateAsync(Location(1, 7, "Visit", "CA", new DateTime(2024, 5, 10), "[]"));

        Assert.NotEqual(default, created.CreatedDate);
        Assert.NotNull(await repository.FindDuplicateAsync(7, "Visit", new DateTime(2024, 5, 10, 15, 0, 0), "", "CA"));
        Assert.Null(await repository.FindDuplicateAsync(7, "Visit", new DateTime(2024, 5, 11), null, null));

        created.Name = "Updated";
        Assert.NotNull(await repository.UpdateAsync(created));
        Assert.Equal("Updated", (await repository.GetByIdAsync(1, 7))!.Name);
        Assert.Null(await repository.UpdateAsync(Location(99, 7, "Missing", "CA", DateTime.UtcNow, "[]")));

        await repository.DeleteAsync(1, 99);
        Assert.NotNull(await repository.GetByIdAsync(1, 7));
        await repository.DeleteAsync(1, 7);
        Assert.Null(await repository.GetByIdAsync(1, 7));

        await repository.CreateAsync(Location(2, 7, "One", "CA", DateTime.UtcNow, "[]"));
        await repository.CreateAsync(Location(3, 7, "Two", "CA", DateTime.UtcNow, "[]"));
        await repository.DeleteAllByUserIdAsync(7);
        Assert.Empty(await repository.GetAllByUserIdAsync(7));
    }

    private static Location Location(int id, int userId, string name, string state, DateTime startDate, string tagsJson) => new()
    {
        Id = id,
        UserId = userId,
        Name = name,
        State = state,
        StartDate = startDate,
        LocationType = "National Park",
        City = "City",
        TagsJson = tagsJson
    };

    private static TravelTrackerDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<TravelTrackerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}