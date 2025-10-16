using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface INationalParkService
{
    Task<IEnumerable<NationalPark>> GetAllParksAsync();
    Task<NationalPark?> GetParkByIdAsync(string id, string state);
    Task<IEnumerable<NationalPark>> GetParksByStateAsync(string state);
    Task<IEnumerable<NationalPark>> GetVisitedParksAsync(string userId);
}
