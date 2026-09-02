namespace TravelTracker.Services.Interfaces;

public interface IPlaceLookupRateLimiter
{
    Task WaitAsync(CancellationToken cancellationToken = default);
}
