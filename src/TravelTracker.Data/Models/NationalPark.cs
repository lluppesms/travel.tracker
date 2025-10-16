namespace TravelTracker.Data.Models;

public class NationalPark
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "nationalpark";
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; } = string.Empty;
}
