namespace TravelTracker.Data.Models;

public class Location
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "location";
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Rating { get; set; }
    public string Comments { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
