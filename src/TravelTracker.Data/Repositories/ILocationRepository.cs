namespace TravelTracker.Data.Repositories;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(int id, int userId);
    Task<IEnumerable<Location>> GetAllByUserIdAsync(int userId);
    Task<IEnumerable<Location>> GetByDateRangeAsync(int userId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Location>> GetByStateAsync(int userId, string state);
    Task<Location> CreateAsync(Location location, CancellationToken cancellationToken = default);
    Task<Location?> UpdateAsync(Location location);
    Task DeleteAsync(int id, int userId);
    Task DeleteAllByUserIdAsync(int userId);
    Task<IReadOnlyList<Location>> SearchForAssistantAsync(
        int userId,
        string query,
        int limit,
        CancellationToken cancellationToken = default);
    Task<Location?> FindDuplicateAsync(
        int userId,
        string name,
        DateTime visitDate,
        string? city,
        string? state,
        CancellationToken cancellationToken = default);
}
