using Newtonsoft.Json;

namespace TravelTracker.Data.Models;

public class User
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonProperty("type")]
    public string Type { get; set; } = "user";
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;
    [JsonProperty("entraUserId")]
    public string EntraIdUserId { get; set; } = string.Empty;
    [JsonProperty("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [JsonProperty("lastLoginDate")]
    public DateTime? LastLoginDate { get; set; }
}
