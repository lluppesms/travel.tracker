using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TravelTracker.Data.Configuration;
using TravelTracker.Data.Models;

namespace TravelTracker.Data.Repositories;

public class LocationRepository : ILocationRepository
{
    private readonly Container _container;

    public LocationRepository(CosmosClient cosmosClient, IOptions<CosmosDbSettings> settings)
    {
        var database = cosmosClient.GetDatabase(settings.Value.DatabaseName);
        _container = database.GetContainer(settings.Value.LocationsContainerName);
    }

    public async Task<Location?> GetByIdAsync(string id, string userId)
    {
        try
        {
            var response = await _container.ReadItemAsync<Location>(id, new PartitionKey(userId));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Location>> GetAllByUserIdAsync(string userId)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
            .WithParameter("@userId", userId);

        var iterator = _container.GetItemQueryIterator<Location>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId)
        });

        var results = new List<Location>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<IEnumerable<Location>> GetByDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE c.userId = @userId AND c.startDate >= @startDate AND c.startDate <= @endDate")
            .WithParameter("@userId", userId)
            .WithParameter("@startDate", startDate)
            .WithParameter("@endDate", endDate);

        var iterator = _container.GetItemQueryIterator<Location>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId)
        });

        var results = new List<Location>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<IEnumerable<Location>> GetByStateAsync(string userId, string state)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId AND c.state = @state")
            .WithParameter("@userId", userId)
            .WithParameter("@state", state);

        var iterator = _container.GetItemQueryIterator<Location>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(userId)
        });

        var results = new List<Location>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<Location> CreateAsync(Location location)
    {
        location.CreatedDate = DateTime.UtcNow;
        location.ModifiedDate = DateTime.UtcNow;
        var response = await _container.CreateItemAsync(location, new PartitionKey(location.UserId));
        return response.Resource;
    }

    public async Task<Location> UpdateAsync(Location location)
    {
        location.ModifiedDate = DateTime.UtcNow;
        var response = await _container.ReplaceItemAsync(location, location.Id, new PartitionKey(location.UserId));
        return response.Resource;
    }

    public async Task DeleteAsync(string id, string userId)
    {
        await _container.DeleteItemAsync<Location>(id, new PartitionKey(userId));
    }
}
