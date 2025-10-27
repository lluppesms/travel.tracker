using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface IDataExportService
{
    Task<Stream> ExportToJsonAsync(int userId);
    Task<Stream> ExportToCsvAsync(int userId);
}
