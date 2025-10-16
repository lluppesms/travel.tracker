using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TravelTracker.Data.Configuration;
using TravelTracker.Data.Models;

namespace TravelTracker.Data.Repositories;

public class NationalParkRepository : INationalParkRepository
{
    private readonly Container _container;

    public NationalParkRepository(CosmosClient cosmosClient, IOptions<CosmosDbSettings> settings)
    {
        var database = cosmosClient.GetDatabase(settings.Value.DatabaseName);
        _container = database.GetContainer(settings.Value.NationalParksContainerName);
    }

    public async Task<IEnumerable<NationalPark>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT * FROM c");
        var iterator = _container.GetItemQueryIterator<NationalPark>(query);

        var results = new List<NationalPark>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }

    public async Task<NationalPark?> GetByIdAsync(string id, string state)
    {
        try
        {
            var response = await _container.ReadItemAsync<NationalPark>(id, new PartitionKey(state));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<IEnumerable<NationalPark>> GetByStateAsync(string state)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.state = @state")
            .WithParameter("@state", state);

        var iterator = _container.GetItemQueryIterator<NationalPark>(query, requestOptions: new QueryRequestOptions
        {
            PartitionKey = new PartitionKey(state)
        });

        var results = new List<NationalPark>();
        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }

        return results;
    }
}
