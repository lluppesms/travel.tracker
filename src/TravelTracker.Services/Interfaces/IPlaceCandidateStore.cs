using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

public interface IPlaceCandidateStore
{
    PlaceLookupResult? TryGetLookup(string cacheKey, DateTime utcNow);
    PlaceLookupResult StoreLookup(
        string cacheKey,
        PlaceLookupStatus status,
        IReadOnlyList<PlaceCandidate> candidates,
        bool usedBroaderFallback,
        string? message,
        DateTime utcNow,
        TimeSpan lifetime);
    PlaceCandidate? Resolve(string candidateId, DateTime utcNow);
}
