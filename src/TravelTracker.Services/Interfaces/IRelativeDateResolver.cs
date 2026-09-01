using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Resolves the limited set of supported relative date expressions using server time.
/// </summary>
public interface IRelativeDateResolver
{
    /// <summary>
    /// Resolves an expression using the configured travel-assistant time zone. When an assistant also
    /// proposes an ISO date, the proposed date must match the server result.
    /// </summary>
    RelativeDateResolution Resolve(string expression, DateOnly? proposedDate = null);
}
