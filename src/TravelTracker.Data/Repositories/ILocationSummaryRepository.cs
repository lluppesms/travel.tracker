namespace TravelTracker.Data.Repositories;

/// <summary>
/// Reads the pre-computed travel summary (visited locations, states, national parks, etc.)
/// used to ground the travel assistant with authoritative data before each chat turn.
/// </summary>
public interface ILocationSummaryRepository
{
    /// <summary>
    /// Executes <c>[Travel].[usp_LocationSummary]</c> for the given user name (username or email)
    /// and returns a plain-text block summarizing the result sets, suitable for prompt injection.
    /// Returns <see langword="null"/> when the summary could not be retrieved.
    /// </summary>
    Task<string?> GetLocationSummaryTextAsync(string userName, CancellationToken cancellationToken = default);
}
