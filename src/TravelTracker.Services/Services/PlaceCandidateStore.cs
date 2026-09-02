using System.Collections.Concurrent;
using System.Security.Cryptography;

using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

public sealed class PlaceCandidateStore : IPlaceCandidateStore
{
    private const int MaximumCachedLookups = 1000;

    private readonly ConcurrentDictionary<string, CachedLookup> _lookups = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PlaceCandidate> _candidates = new(StringComparer.Ordinal);

    public PlaceLookupResult? TryGetLookup(string cacheKey, DateTime utcNow)
    {
        if (!_lookups.TryGetValue(cacheKey, out var cached))
        {
            return null;
        }

        if (cached.ExpiresAtUtc > utcNow)
        {
            return cached.Result;
        }

        _lookups.TryRemove(cacheKey, out _);
        return null;
    }

    public PlaceLookupResult StoreLookup(
        string cacheKey,
        PlaceLookupStatus status,
        IReadOnlyList<PlaceCandidate> candidates,
        bool usedBroaderFallback,
        string? message,
        DateTime utcNow,
        TimeSpan lifetime)
    {
        var expiresAt = utcNow.Add(lifetime);
        var storedCandidates = candidates.Select(candidate => candidate with
        {
            CandidateId = CreateOpaqueId(),
            ExpiresAtUtc = expiresAt
        }).ToArray();

        foreach (var candidate in storedCandidates)
        {
            _candidates[candidate.CandidateId] = candidate;
        }

        var result = new PlaceLookupResult
        {
            Status = status,
            Candidates = storedCandidates,
            UsedBroaderFallback = usedBroaderFallback,
            Message = message
        };

        _lookups[cacheKey] = new CachedLookup(result, expiresAt);
        RemoveExpired(utcNow);
        return result;
    }

    public PlaceCandidate? Resolve(string candidateId, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(candidateId)
            || !_candidates.TryGetValue(candidateId, out var candidate))
        {
            return null;
        }

        if (candidate.ExpiresAtUtc <= utcNow)
        {
            _candidates.TryRemove(candidateId, out _);
            return null;
        }

        return candidate;
    }

    private void RemoveExpired(DateTime utcNow)
    {
        foreach (var pair in _lookups)
        {
            if (pair.Value.ExpiresAtUtc <= utcNow)
            {
                _lookups.TryRemove(pair.Key, out _);
            }
        }

        foreach (var pair in _candidates)
        {
            if (pair.Value.ExpiresAtUtc <= utcNow)
            {
                _candidates.TryRemove(pair.Key, out _);
            }
        }

        if (_lookups.Count <= MaximumCachedLookups)
        {
            return;
        }

        foreach (var pair in _lookups
                     .OrderBy(entry => entry.Value.ExpiresAtUtc)
                     .Take(_lookups.Count - MaximumCachedLookups))
        {
            if (_lookups.TryRemove(pair.Key, out var removed))
            {
                foreach (var candidate in removed.Result.Candidates)
                {
                    _candidates.TryRemove(candidate.CandidateId, out _);
                }
            }
        }
    }

    private static string CreateOpaqueId() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(18))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private sealed record CachedLookup(PlaceLookupResult Result, DateTime ExpiresAtUtc);
}
