using Newtonsoft.Json;

namespace TravelTracker.Data.Models;

public class NationalPark
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("type")]
    public string Type { get; set; } = "nationalpark";
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;
    [JsonProperty("latitude")]
    public double Latitude { get; set; }
    [JsonProperty("longitude")]
    public double Longitude { get; set; }
    [JsonProperty("description")]
    public string Description { get; set; } = string.Empty;
}
