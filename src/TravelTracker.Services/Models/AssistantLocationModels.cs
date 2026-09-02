namespace TravelTracker.Services.Models;

public sealed record AssistantLocationSearchResult
{
    public required int LocationId { get; init; }
    public required string Name { get; init; }
    public required string LocationType { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required DateOnly VisitDate { get; init; }
    public string TrustLabel { get; init; } = "untrusted_stored_text";
}

/// <summary>
/// Compact model-visible representation of a configured location type.
/// </summary>
public sealed record AssistantLocationTypeResult
{
    /// <summary>Gets the configured location type name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the configured location type description.</summary>
    public required string Description { get; init; }
}

public enum LocationTypeResolutionStatus
{
    Found,
    Ambiguous,
    NotFound
}

public sealed record LocationTypeResolutionResult
{
    public required LocationTypeResolutionStatus Status { get; init; }
    public LocationType? LocationType { get; init; }
    public IReadOnlyList<string> Matches { get; init; } = [];
}
