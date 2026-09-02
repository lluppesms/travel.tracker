using TravelTracker.Data.Models;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

public interface ILocationLookupService
{
    bool IsConfigured { get; }
    Task<LocationLookupResult> LookupLocationAsync(
        string name,
        string address,
        string city,
        string state,
        string zipCode,
        CancellationToken cancellationToken = default);
    Task<PlaceLookupResult> LookupPlaceAsync(
        PlaceLookupRequest request,
        CancellationToken cancellationToken = default);
    Task<PlaceCandidate?> ResolveCandidateAsync(
        string candidateId,
        CancellationToken cancellationToken = default);
}
