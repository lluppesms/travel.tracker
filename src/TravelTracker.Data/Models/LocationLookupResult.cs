namespace TravelTracker.Data.Models;

public class LocationLookupResult
{
    public bool Success { get; set; }

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string ZipCode { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;
}
