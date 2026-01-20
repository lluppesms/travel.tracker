using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface IHighPointService
{
    Task<IEnumerable<HighPoint>> GetAllHighPointsAsync();
    Task<HighPoint?> GetHighPointByIdAsync(int id);
    Task<IEnumerable<HighPoint>> GetHighPointsByStateAsync(string state);
    Task<IEnumerable<HighPoint>> GetVisitedHighPointsAsync(int userId);
}
