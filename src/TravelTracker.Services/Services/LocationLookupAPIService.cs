using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using TravelTracker.Data.Configuration;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

public sealed class LocationLookupAPIService : ILocationLookupService
{
    private const double FoundScoreThreshold = 0.72;
    private const double AmbiguityGap = 0.15;
    private const double DivergenceKilometers = 25;

    private readonly HttpClient _httpClient;
    private readonly IPlaceCandidateStore _candidateStore;
    private readonly IPlaceLookupRateLimiter _rateLimiter;
    private readonly TimeProvider _timeProvider;
    private readonly TravelAssistantOptions _options;
    private readonly ILogger<LocationLookupAPIService> _logger;

    public LocationLookupAPIService(
        HttpClient httpClient,
        IPlaceCandidateStore candidateStore,
        IPlaceLookupRateLimiter rateLimiter,
        TimeProvider timeProvider,
        IOptions<TravelAssistantOptions> options,
        ILogger<LocationLookupAPIService> logger)
    {
        _httpClient = httpClient;
        _candidateStore = candidateStore;
        _rateLimiter = rateLimiter;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;

        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("TravelTracker", "1.0"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("(+https://github.com/lluppesms/travel.tracker)"));
        }
    }

    public bool IsConfigured => true;

    public async Task<LocationLookupResult> LookupLocationAsync(
        string name,
        string address,
        string city,
        string state,
        string zipCode,
        CancellationToken cancellationToken = default)
    {
        var lookup = await LookupPlaceAsync(
            new PlaceLookupRequest
            {
                Name = name,
                Address = address,
                City = city,
                State = state,
                PostalCode = zipCode,
                MaxCandidates = 5
            },
            cancellationToken).ConfigureAwait(false);

        var candidate = lookup.Candidates.FirstOrDefault();
        return candidate is null
            ? new LocationLookupResult
            {
                Success = false,
                ErrorMessage = lookup.Message ?? "No matching place was found."
            }
            : new LocationLookupResult
            {
                Success = lookup.Status == PlaceLookupStatus.Found,
                Address = candidate.Address,
                City = candidate.City,
                State = candidate.State,
                ZipCode = candidate.PostalCode,
                Latitude = candidate.Latitude,
                Longitude = candidate.Longitude,
                ErrorMessage = lookup.Status == PlaceLookupStatus.Ambiguous
                    ? "Multiple possible places were found. Please verify the selection."
                    : string.Empty
            };
    }

    public async Task<PlaceLookupResult> LookupPlaceAsync(
        PlaceLookupRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new PlaceLookupResult
            {
                Status = PlaceLookupStatus.NotFound,
                Message = "A place name is required."
            };
        }

        var normalizedRequest = request with
        {
            Name = request.Name.Trim(),
            Address = NormalizeOptional(request.Address),
            City = NormalizeOptional(request.City),
            State = NormalizeState(request.State),
            PostalCode = NormalizeOptional(request.PostalCode),
            MaxCandidates = Math.Clamp(request.MaxCandidates, 1, 10)
        };
        var cacheKey = BuildCacheKey(normalizedRequest);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var cached = _candidateStore.TryGetLookup(cacheKey, now);
        if (cached is not null)
        {
            return cached;
        }

        var queryAttempts = BuildQueryAttempts(normalizedRequest);
        List<RawCandidate> rawCandidates = [];
        var usedBroaderFallback = false;

        for (var attempt = 0; attempt < queryAttempts.Count; attempt++)
        {
            rawCandidates = await SearchNominatimAsync(
                queryAttempts[attempt],
                normalizedRequest.MaxCandidates,
                cancellationToken).ConfigureAwait(false);

            if (rawCandidates.Count > 0)
            {
                usedBroaderFallback = attempt > 0;
                break;
            }
        }

        if (rawCandidates.Count == 0)
        {
            return _candidateStore.StoreLookup(
                cacheKey,
                PlaceLookupStatus.NotFound,
                [],
                usedBroaderFallback,
                "No matching place was found. Try adding a city or state.",
                now,
                TimeSpan.FromMinutes(Math.Max(1, _options.CandidateExpiryMinutes)));
        }

        var ranked = rawCandidates
            .Select(candidate => ToPlaceCandidate(candidate, normalizedRequest))
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .Take(normalizedRequest.MaxCandidates)
            .ToList();

        var top = ranked[0];
        var photonEvidence = await LookupPhotonEvidenceAsync(top, cancellationToken).ConfigureAwait(false);
        if (photonEvidence is not null)
        {
            var divergent = DistanceKilometers(
                top.Latitude,
                top.Longitude,
                photonEvidence.Latitude,
                photonEvidence.Longitude) > DivergenceKilometers;

            ranked[0] = top with
            {
                CoordinateDivergenceDetected = divergent,
                Evidence = top.Evidence.Concat([photonEvidence]).ToArray()
            };
            top = ranked[0];
        }

        var secondScore = ranked.Count > 1 ? ranked[1].Score : 0;
        var status = top.Score >= FoundScoreThreshold
            && top.Score - secondScore >= AmbiguityGap
            && !top.CoordinateDivergenceDetected
                ? PlaceLookupStatus.Found
                : PlaceLookupStatus.Ambiguous;

        var message = status == PlaceLookupStatus.Found
            ? null
            : top.CoordinateDivergenceDetected
                ? "Providers returned materially different coordinates. Please choose or clarify the place."
                : "Multiple plausible places were found. Please choose a candidate.";

        return _candidateStore.StoreLookup(
            cacheKey,
            status,
            ranked,
            usedBroaderFallback,
            message,
            now,
            TimeSpan.FromMinutes(Math.Max(1, _options.CandidateExpiryMinutes)));
    }

    public Task<PlaceCandidate?> ResolveCandidateAsync(
        string candidateId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_candidateStore.Resolve(candidateId, _timeProvider.GetUtcNow().UtcDateTime));
    }

    private async Task<List<RawCandidate>> SearchNominatimAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            return await SearchNominatimCoreAsync(query, limit, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Nominatim timed out while searching for a place.");
            return [];
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Nominatim was unavailable while searching for a place.");
            return [];
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Nominatim returned an invalid response.");
            return [];
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Nominatim returned an unexpected response shape.");
            return [];
        }
    }

    private async Task<List<RawCandidate>> SearchNominatimCoreAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}" +
                  $"&format=json&addressdetails=1&namedetails=1&limit={limit}&countrycodes=us";

        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Nominatim returned status {StatusCode}.", (int)response.StatusCode);
            return [];
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var candidates = new List<RawCandidate>();

        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (!TryGetCoordinate(item, "lat", out var latitude)
                || !TryGetCoordinate(item, "lon", out var longitude))
            {
                continue;
            }

            item.TryGetProperty("address", out var address);
            item.TryGetProperty("namedetails", out var names);
            var displayName = GetString(item, "display_name");
            var name = GetFirstString(names, "name", "official_name", "short_name");
            if (string.IsNullOrWhiteSpace(name))
            {
                name = displayName.Split(',', StringSplitOptions.TrimEntries)[0];
            }

            var houseNumber = GetString(address, "house_number");
            var road = GetFirstString(address, "road", "pedestrian", "amenity", "tourism", "leisure");
            var streetAddress = string.Join(' ', new[] { houseNumber, road }.Where(value => !string.IsNullOrWhiteSpace(value)));

            candidates.Add(new RawCandidate(
                name,
                streetAddress,
                GetFirstString(address, "city", "town", "village", "municipality", "hamlet"),
                NormalizeState(GetString(address, "state")),
                NormalizePostalCode(GetString(address, "postcode")),
                latitude,
                longitude,
                displayName));
        }

        return candidates;
    }

    private async Task<PlaceProviderEvidence?> LookupPhotonEvidenceAsync(
        PlaceCandidate candidate,
        CancellationToken cancellationToken)
    {
        try
        {
            return await LookupPhotonEvidenceCoreAsync(candidate, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Photon timed out while validating place coordinates.");
            return null;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Photon was unavailable while validating place coordinates.");
            return null;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Photon returned an invalid response.");
            return null;
        }
        catch (InvalidOperationException exception)
        {
            _logger.LogWarning(exception, "Photon returned incomplete coordinate data.");
            return null;
        }
    }

    private async Task<PlaceProviderEvidence?> LookupPhotonEvidenceCoreAsync(
        PlaceCandidate candidate,
        CancellationToken cancellationToken)
    {
        var query = string.Join(", ", new[]
        {
            candidate.Name,
            candidate.Address,
            candidate.City,
            candidate.State,
            candidate.PostalCode
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

        await _rateLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);
        var url = $"https://photon.komoot.io/api?q={Uri.EscapeDataString(query)}&limit=1";
        using var response = await _httpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("features", out var features)
            || features.GetArrayLength() == 0)
        {
            return null;
        }

        if (!features[0].TryGetProperty("geometry", out var geometry)
            || !geometry.TryGetProperty("coordinates", out var coordinates)
            || coordinates.ValueKind != JsonValueKind.Array
            || coordinates.GetArrayLength() < 2
            || coordinates[0].ValueKind != JsonValueKind.Number
            || coordinates[1].ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return new PlaceProviderEvidence
        {
            Provider = "Photon",
            ProviderReference = "photon:0",
            Longitude = coordinates[0].GetDouble(),
            Latitude = coordinates[1].GetDouble()
        };
    }

    private static PlaceCandidate ToPlaceCandidate(RawCandidate candidate, PlaceLookupRequest request) =>
        new()
        {
            CandidateId = string.Empty,
            Name = candidate.Name,
            Address = candidate.Address,
            City = candidate.City,
            State = candidate.State,
            PostalCode = candidate.PostalCode,
            Latitude = candidate.Latitude,
            Longitude = candidate.Longitude,
            Score = Score(candidate, request),
            ExpiresAtUtc = DateTime.MinValue,
            Evidence =
            [
                new PlaceProviderEvidence
                {
                    Provider = "Nominatim",
                    ProviderReference = candidate.ProviderReference,
                    Latitude = candidate.Latitude,
                    Longitude = candidate.Longitude
                }
            ]
        };

    private static double Score(RawCandidate candidate, PlaceLookupRequest request)
    {
        var nameScore = Similarity(request.Name, candidate.Name);
        var cityScore = string.IsNullOrWhiteSpace(request.City) ? 1 : Similarity(request.City, candidate.City);
        var stateScore = string.IsNullOrWhiteSpace(request.State) ? 1 : Similarity(request.State, candidate.State);
        return Math.Round((nameScore * 0.55) + (cityScore * 0.25) + (stateScore * 0.20), 4);
    }

    private static double Similarity(string? expected, string? actual)
    {
        var normalizedExpected = NormalizeForComparison(expected);
        var normalizedActual = NormalizeForComparison(actual);
        if (normalizedExpected.Length == 0)
        {
            return 1;
        }

        if (normalizedExpected == normalizedActual)
        {
            return 1;
        }

        return normalizedActual.Contains(normalizedExpected, StringComparison.Ordinal)
            || normalizedExpected.Contains(normalizedActual, StringComparison.Ordinal)
                ? 0.75
                : 0;
    }

    private static string NormalizeForComparison(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static List<string> BuildQueryAttempts(PlaceLookupRequest request)
    {
        var attempts = new List<string>();
        AddQuery(attempts, request.Name, request.Address, request.City, request.State, request.PostalCode);
        AddQuery(attempts, request.Name, request.City, request.State);
        AddQuery(attempts, request.Name, request.State);
        AddQuery(attempts, request.Name);
        return attempts;
    }

    private static void AddQuery(List<string> attempts, params string?[] parts)
    {
        var query = string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        if (!string.IsNullOrWhiteSpace(query) && !attempts.Contains(query, StringComparer.OrdinalIgnoreCase))
        {
            attempts.Add(query);
        }
    }

    private static string BuildCacheKey(PlaceLookupRequest request) =>
        string.Join('|',
            NormalizeForComparison(request.Name),
            NormalizeForComparison(request.Address),
            NormalizeForComparison(request.City),
            NormalizeForComparison(request.State),
            NormalizeForComparison(request.PostalCode),
            request.MaxCandidates);

    private static string NormalizeOptional(string? value) => value?.Trim() ?? string.Empty;

    private static string NormalizePostalCode(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Split('-', StringSplitOptions.TrimEntries)[0];

    private static string NormalizeState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return string.Empty;
        }

        var trimmed = state.Trim();
        if (trimmed.Length == 2)
        {
            return trimmed.ToUpperInvariant();
        }

        return StateAbbreviations.TryGetValue(trimmed, out var abbreviation)
            ? abbreviation
            : trimmed;
    }

    private static bool TryGetCoordinate(JsonElement element, string propertyName, out double value) =>
        double.TryParse(
            GetString(element, propertyName),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);

    private static string GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(propertyName, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    private static string GetFirstString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var value = GetString(element, propertyName);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static double DistanceKilometers(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        const double earthRadiusKm = 6371;
        var latitudeDelta = DegreesToRadians(latitude2 - latitude1);
        var longitudeDelta = DegreesToRadians(longitude2 - longitude1);
        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2)
                + Math.Cos(DegreesToRadians(latitude1))
                * Math.Cos(DegreesToRadians(latitude2))
                * Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double value) => value * Math.PI / 180;

    private sealed record RawCandidate(
        string Name,
        string Address,
        string City,
        string State,
        string PostalCode,
        double Latitude,
        double Longitude,
        string ProviderReference);

    private static readonly IReadOnlyDictionary<string, string> StateAbbreviations =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Alabama"] = "AL", ["Alaska"] = "AK", ["Arizona"] = "AZ", ["Arkansas"] = "AR",
            ["California"] = "CA", ["Colorado"] = "CO", ["Connecticut"] = "CT", ["Delaware"] = "DE",
            ["Florida"] = "FL", ["Georgia"] = "GA", ["Hawaii"] = "HI", ["Idaho"] = "ID",
            ["Illinois"] = "IL", ["Indiana"] = "IN", ["Iowa"] = "IA", ["Kansas"] = "KS",
            ["Kentucky"] = "KY", ["Louisiana"] = "LA", ["Maine"] = "ME", ["Maryland"] = "MD",
            ["Massachusetts"] = "MA", ["Michigan"] = "MI", ["Minnesota"] = "MN", ["Mississippi"] = "MS",
            ["Missouri"] = "MO", ["Montana"] = "MT", ["Nebraska"] = "NE", ["Nevada"] = "NV",
            ["New Hampshire"] = "NH", ["New Jersey"] = "NJ", ["New Mexico"] = "NM", ["New York"] = "NY",
            ["North Carolina"] = "NC", ["North Dakota"] = "ND", ["Ohio"] = "OH", ["Oklahoma"] = "OK",
            ["Oregon"] = "OR", ["Pennsylvania"] = "PA", ["Rhode Island"] = "RI", ["South Carolina"] = "SC",
            ["South Dakota"] = "SD", ["Tennessee"] = "TN", ["Texas"] = "TX", ["Utah"] = "UT",
            ["Vermont"] = "VT", ["Virginia"] = "VA", ["Washington"] = "WA", ["West Virginia"] = "WV",
            ["Wisconsin"] = "WI", ["Wyoming"] = "WY", ["District of Columbia"] = "DC"
        };
}
