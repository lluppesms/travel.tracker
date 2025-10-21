using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TravelTracker.Data.Configuration;

/*
 {
    "id": "xxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "type": "user",
    "username": "First Last",
    "email": "somebody@somedomain.com",
    "entraUserId": "xxxxx-xxxx-xxxx-xxxx-xxxxxxxxxx",
    "createdDate": "2025-10-21T15:46:49.9061126Z",
    "lastLoginDate": "2025-10-21T15:46:38.9781994Z"
 }
*/
namespace TravelTracker.Data.Repositories;

public class UserRepository : IUserRepository
{
    private readonly Container _container;

    public UserRepository(CosmosClient cosmosClient, IOptions<CosmosDbSettings> settings)
    {
        //var database = cosmosClient.GetDatabase(settings.Value.DatabaseName);
        //_container = database.GetContainer(settings.Value.UsersContainerName);

        var endpoint = settings.Value.Endpoint;
        var connectionString = settings.Value.ConnectionString;

        // var accountName = string.IsNullOrEmpty(connectionString) ? connectionString?[..connectionString.IndexOf("AccountKey")].Replace("AccountEndpoint=https://", "").Replace(".documents.azure.com:443/;", "").Replace("/;", "") : endpoint;
        var accountName = string.IsNullOrEmpty(connectionString) ? endpoint : connectionString?[..connectionString.IndexOf("AccountKey")];
        accountName = accountName.Replace("https://", "").Replace(".documents.azure.com:443/", "").Replace("/;", "").Replace(";", "");

        var databaseName = settings.Value.DatabaseName;
        var usersContainer = settings.Value.UsersContainerName;
        var locationsContainer = settings.Value.LocationsContainerName;
        var nationalParksContainer = settings.Value.NationalParksContainerName;

        if (!string.IsNullOrEmpty(connectionString)) { Console.Write($"CosmosDbService.Init: Using Account: {accountName} Database: {databaseName}"); }
        if (!string.IsNullOrEmpty(endpoint)) { Console.Write($"CosmosDbService.Init: Using Endpoint: {accountName} Database: {databaseName}"); }

        cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName).GetAwaiter().GetResult();
        var database = cosmosClient.GetDatabase(databaseName);

        var userContainerReponse = database.CreateContainerIfNotExistsAsync(usersContainer, "/id").GetAwaiter().GetResult();
        _container = database.GetContainer(usersContainer);

        database.CreateContainerIfNotExistsAsync(locationsContainer, "/userid").GetAwaiter().GetResult();
        var _locationsContainer = database.GetContainer(locationsContainer);

        database.CreateContainerIfNotExistsAsync(nationalParksContainer, "/state").GetAwaiter().GetResult();
        var _nationalParksContainer = database.GetContainer(nationalParksContainer);

        Console.Write("CosmosDbService.Init: Complete!");
    }

    public async Task EnsureDatabaseSetupAsync()
    {
        try
        {
            ContainerProperties containerProperties = await _container.ReadContainerAsync();
            Console.WriteLine($"Container exists: {containerProperties.Id}");

            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            var countIterator = _container.GetItemQueryIterator<int>(countQuery);
            var countResponse = await countIterator.ReadNextAsync();
            Console.WriteLine($"Container contains {countResponse.FirstOrDefault()} items");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database setup error: {ex.Message}");
            throw;
        }
    }

    public async Task<Models.User?> GetByIdAsync(string id)
    {
        try
        {
            await EnsureDatabaseSetupAsync();

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
            await EnsureDatabaseSetupAsync();

            // IMPORTANT: The JSON property is "entraUserId" per the sample document and JsonProperty attribute
            // so the query must use c.entraUserId (NOT c.entraIdUserId).
            var query = new QueryDefinition("SELECT * FROM c WHERE c.entraUserId = @entraUserId")
                .WithParameter("@entraUserId", entraIdUserId);

            var options = new QueryRequestOptions
            {
                MaxItemCount = 1 // we only need the first match
            };
    
            using var iterator = _container.GetItemQueryIterator<Models.User>(query, requestOptions: options);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }
        }
        catch (CosmosException cex)
        {
            Console.WriteLine($"Cosmos query error (GetByEntraIdAsync): {cex.StatusCode} {cex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"General error in GetByEntraIdAsync: {ex.Message}");
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
            Console.WriteLine($"Error in CreateAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<Models.User> UpdateAsync(Models.User user)
    {
        var response = await _container.ReplaceItemAsync(user, user.Id, new PartitionKey(user.Id));
        return response.Resource;
    }
}
