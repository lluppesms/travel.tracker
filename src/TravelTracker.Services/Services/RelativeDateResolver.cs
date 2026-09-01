using Microsoft.Extensions.Options;

using TravelTracker.Data.Configuration;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

/// <summary>
/// Provides server-authoritative resolution for the relative date expressions supported by the first release.
/// </summary>
public sealed class RelativeDateResolver(
    TimeProvider timeProvider,
    IOptions<TravelAssistantOptions> options) : IRelativeDateResolver
{
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly TravelAssistantOptions _options = options.Value;

    /// <inheritdoc />
    public RelativeDateResolution Resolve(string expression, DateOnly? proposedDate = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return RelativeDateResolution.ClarificationRequired("Please provide the date of your visit.");
        }

        if (!TryResolveTimeZone(_options.TimeZoneId, out var timeZone))
        {
            throw new InvalidOperationException("TravelAssistant:TimeZoneId is not a time zone known to this system.");
        }

        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), timeZone).DateTime);
        DateOnly? resolvedDate = expression.Trim().ToLowerInvariant() switch
        {
            "today" => localToday,
            "yesterday" => localToday.AddDays(-1),
            _ => null
        };

        if (resolvedDate is null)
        {
            return RelativeDateResolution.ClarificationRequired(
                $"I can resolve only 'today' or 'yesterday' automatically. Please provide '{expression}' as an ISO date (YYYY-MM-DD).");
        }

        if (proposedDate is not null && proposedDate != resolvedDate)
        {
            return RelativeDateResolution.DateDisagrees(
                "The proposed visit date does not match the server-resolved date. Please confirm the date.");
        }

        return RelativeDateResolution.Resolved(resolvedDate.Value);
    }

    private static bool TryResolveTimeZone(string timeZoneId, out TimeZoneInfo timeZone)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out timeZone!))
        {
            return true;
        }

        return TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId, out timeZone!);
    }
}
