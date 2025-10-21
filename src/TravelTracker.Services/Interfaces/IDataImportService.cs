using TravelTracker.Data.Models;

namespace TravelTracker.Services.Interfaces;

public interface IDataImportService
{
    Task<ImportResult> ImportFromJsonAsync(Stream jsonStream, string userId);
    Task<ImportResult> ImportFromCsvAsync(Stream csvStream, string userId);
    Task<ValidationResult> ValidateJsonAsync(Stream jsonStream);
    Task<ValidationResult> ValidateCsvAsync(Stream csvStream);
}

public class ImportResult
{
    public bool Success { get; set; }
    public int TotalRecords { get; set; }
    public int ImportedRecords { get; set; }
    public int FailedRecords { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public int RecordCount { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
