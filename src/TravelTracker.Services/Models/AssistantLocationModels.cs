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
