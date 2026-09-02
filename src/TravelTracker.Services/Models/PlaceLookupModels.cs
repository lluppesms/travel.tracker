namespace TravelTracker.Services.Models;

public enum PlaceLookupStatus
{
    Found,
    Ambiguous,
    NotFound
}

public sealed record PlaceLookupRequest
{
    public required string Name { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public int MaxCandidates { get; init; } = 5;
}

public sealed record PlaceProviderEvidence
{
    public required string Provider { get; init; }
    public required string ProviderReference { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
}

public sealed record PlaceCandidate
{
    public required string CandidateId { get; init; }
    public required string Name { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string PostalCode { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required double Score { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
    public bool CoordinateDivergenceDetected { get; init; }
    public IReadOnlyList<PlaceProviderEvidence> Evidence { get; init; } = [];
}

public sealed record PlaceLookupResult
{
    public required PlaceLookupStatus Status { get; init; }
    public IReadOnlyList<PlaceCandidate> Candidates { get; init; } = [];
    public bool UsedBroaderFallback { get; init; }
    public string? Message { get; init; }
}
