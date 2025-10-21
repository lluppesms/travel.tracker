namespace TravelTracker.Data.Configuration;

public class CosmosDbSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string UsersContainerName { get; set; } = "users";
    public string LocationsContainerName { get; set; } = "locations";
    public string NationalParksContainerName { get; set; } = "nationalparks";
}
