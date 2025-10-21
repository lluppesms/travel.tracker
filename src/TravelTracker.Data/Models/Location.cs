using Newtonsoft.Json;

namespace TravelTracker.Data.Models;

public class Location
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonProperty("type")]
    public string Type { get; set; } = "location";
    [JsonProperty("userId")]
    public string UserId { get; set; } = string.Empty;
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    [JsonProperty("locationType")]
    public string LocationType { get; set; } = string.Empty;
    [JsonProperty("address")]
    public string Address { get; set; } = string.Empty;
    [JsonProperty("city")]
    public string City { get; set; } = string.Empty;
    [JsonProperty("state")]
    public string State { get; set; } = string.Empty;
    [JsonProperty("zipCode")]
    public string ZipCode { get; set; } = string.Empty;
    [JsonProperty("latitude")]
    public double Latitude { get; set; }
    [JsonProperty("longitude")]
    public double Longitude { get; set; }
    [JsonProperty("startDate")]
    public DateTime StartDate { get; set; }
    [JsonProperty("endDate")]
    public DateTime? EndDate { get; set; }
    [JsonProperty("rating")]
    public int Rating { get; set; }
    [JsonProperty("cComments")]
    public string Comments { get; set; } = string.Empty;
    [JsonProperty("tags")]
    public List<string> Tags { get; set; } = new();
    [JsonProperty("createdDate")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    [JsonProperty("modifiedDate")]
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
