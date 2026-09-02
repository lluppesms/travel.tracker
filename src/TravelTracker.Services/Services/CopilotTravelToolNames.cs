namespace TravelTracker.Services.Services;

/// <summary>
/// Defines the complete allowlist of custom tools available to Copilot sessions.
/// </summary>
public static class CopilotTravelToolNames
{
    /// <summary>Searches the authenticated user's compact location history.</summary>
    public const string SearchUserLocations = "search_user_locations";

    /// <summary>Lists valid configured location types.</summary>
    public const string GetLocationTypes = "get_location_types";

    /// <summary>Looks up ranked place candidates.</summary>
    public const string LookupPlace = "lookup_place";

    /// <summary>Prepares, but does not confirm, a durable add-location action.</summary>
    public const string PrepareAddVisitedLocation = "prepare_add_visited_location";

    /// <summary>Gets the complete ordered tool inventory.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        SearchUserLocations,
        GetLocationTypes,
        LookupPlace,
        PrepareAddVisitedLocation
    ];

    /// <summary>Gets the tools that perform bounded read-only operations.</summary>
    public static IReadOnlyList<string> ReadOnly { get; } =
    [
        SearchUserLocations,
        GetLocationTypes,
        LookupPlace
    ];

    internal static string? Normalize(string? toolName)
    {
        const string customPrefix = "custom:";
        var candidate = toolName?.StartsWith(customPrefix, StringComparison.Ordinal) == true
            ? toolName[customPrefix.Length..]
            : toolName;

        return All.Contains(candidate, StringComparer.Ordinal) ? candidate : null;
    }
}
