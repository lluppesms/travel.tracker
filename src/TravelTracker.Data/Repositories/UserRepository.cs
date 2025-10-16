using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TravelTracker.Data.Configuration;
using TravelTracker.Data.Models;
using CosmosUser = Microsoft.Azure.Cosmos.User;

namespace TravelTracker.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly Container _container;

    public UserRepository(CosmosClient cosmosClient, IOptions<CosmosDbSettings> settings)
    {
        var database = cosmosClient.GetDatabase(settings.Value.DatabaseName);
        _container = database.GetContainer(settings.Value.UsersContainerName);
    }

    public async Task<Models.User?> GetByIdAsync(string id)
    {
        try
        {
            var response = await _container.ReadItemAsync<Models.User>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Models.User?> GetByEntraIdAsync(string entraIdUserId)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.entraIdUserId = @entraIdUserId")
                .WithParameter("@entraIdUserId", entraIdUserId);

            var iterator = _container.GetItemQueryIterator<Models.User>(query);

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetByEntraIdAsync: {ex.Message}");
        }
        return null;
    }

    public async Task<Models.User> CreateAsync(Models.User user)
    {
        try
        {
            user.CreatedDate = DateTime.UtcNow;
            var response = await _container.CreateItemAsync(user, new PartitionKey(user.Id));
            return response.Resource;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetByEntraIdAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<Models.User> UpdateAsync(Models.User user)
    {
        var response = await _container.ReplaceItemAsync(user, user.Id, new PartitionKey(user.Id));
        return response.Resource;
    }
}
