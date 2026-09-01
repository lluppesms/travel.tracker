namespace TravelTracker.Services;

/// <summary>
/// Startup readiness of the travel assistant surface (OPS-008). <see cref="Failures"/> contains
/// configuration key names only and never contains configuration values.
/// </summary>
public sealed class TravelAssistantReadiness(bool isReady, IReadOnlyList<string> failures)
{
    public bool IsReady { get; } = isReady;

    public IReadOnlyList<string> Failures { get; } = failures;
}
