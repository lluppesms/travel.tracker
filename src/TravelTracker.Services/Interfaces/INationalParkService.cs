using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface INationalParkService
{
    Task<IEnumerable<NationalPark>> GetAllParksAsync();
    Task<NationalPark?> GetParkByIdAsync(int id, string state);
    Task<IEnumerable<NationalPark>> GetParksByStateAsync(string state);
    Task<IEnumerable<NationalPark>> GetVisitedParksAsync(int userId);
}
