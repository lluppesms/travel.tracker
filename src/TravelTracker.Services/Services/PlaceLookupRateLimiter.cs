using TravelTracker.Data.Configuration;

namespace TravelTracker.Services.Services;

public sealed class PlaceLookupRateLimiter(
    TimeProvider timeProvider,
    IOptions<TravelAssistantOptions> options) : IPlaceLookupRateLimiter
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly TimeSpan _minimumInterval =
        TimeSpan.FromMilliseconds(Math.Max(0, options.Value.GeocodingMinimumIntervalMilliseconds));
    private DateTimeOffset _lastRequest = DateTimeOffset.MinValue;

    public async Task WaitAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var delay = _minimumInterval - (_timeProvider.GetUtcNow() - _lastRequest);
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, _timeProvider, cancellationToken).ConfigureAwait(false);
            }

            _lastRequest = _timeProvider.GetUtcNow();
        }
        finally
        {
            _gate.Release();
        }
    }
}
