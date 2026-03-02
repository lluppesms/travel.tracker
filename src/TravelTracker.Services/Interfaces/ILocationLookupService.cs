using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface ILocationLookupService
{
    bool IsConfigured { get; }
    Task<LocationLookupResult> LookupLocationAsync(string name, string address, string city, string state, string zipCode);
}
