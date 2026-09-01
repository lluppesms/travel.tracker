using Microsoft.Extensions.Options;

using TravelTracker.Data.Configuration;
using TravelTracker.Services.Models;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class RelativeDateResolverTests
{
    private static readonly TimeZoneInfo ChicagoTimeZone = ResolveTimeZone("America/Chicago");

    [Fact]
    public void Resolve_Yesterday_UsesConfiguredTimeZone()
    {
        var resolver = CreateResolver(new DateTimeOffset(2026, 9, 1, 17, 0, 0, TimeSpan.Zero));

        var result = resolver.Resolve("Yesterday");

        Assert.Equal(RelativeDateResolutionStatus.Resolved, result.Status);
        Assert.Equal(new DateOnly(2026, 8, 31), result.Date);
    }

    [Fact]
    public void Resolve_Today_UsesConfiguredTimeZoneAcrossUtcMidnight()
    {
        var resolver = CreateResolver(new DateTimeOffset(2026, 9, 1, 1, 0, 0, TimeSpan.Zero));

        var result = resolver.Resolve("today");

        Assert.Equal(RelativeDateResolutionStatus.Resolved, result.Status);
        Assert.Equal(new DateOnly(2026, 8, 31), result.Date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("last Tuesday")]
    [InlineData("tomorrow")]
    public void Resolve_UnsupportedExpression_RequiresClarification(string expression)
    {
        var resolver = CreateResolver(new DateTimeOffset(2026, 9, 1, 17, 0, 0, TimeSpan.Zero));

        var result = resolver.Resolve(expression);

        Assert.Equal(RelativeDateResolutionStatus.ClarificationRequired, result.Status);
        Assert.Null(result.Date);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void Resolve_ProposedDateDisagreesWithServer_ReturnsDisagreement()
    {
        var resolver = CreateResolver(new DateTimeOffset(2026, 9, 1, 17, 0, 0, TimeSpan.Zero));

        var result = resolver.Resolve("yesterday", new DateOnly(2026, 9, 1));

        Assert.Equal(RelativeDateResolutionStatus.DateDisagrees, result.Status);
        Assert.Null(result.Date);
    }

    [Fact]
    public void Resolve_ProposedDateMatchesServer_ReturnsResolvedDate()
    {
        var resolver = CreateResolver(new DateTimeOffset(2026, 9, 1, 17, 0, 0, TimeSpan.Zero));

        var result = resolver.Resolve("yesterday", new DateOnly(2026, 8, 31));

        Assert.Equal(RelativeDateResolutionStatus.Resolved, result.Status);
        Assert.Equal(new DateOnly(2026, 8, 31), result.Date);
    }

    private static RelativeDateResolver CreateResolver(DateTimeOffset utcNow) =>
        new(new FixedTimeProvider(utcNow), Options.Create(new TravelAssistantOptions
        {
            TimeZoneId = ChicagoTimeZone.Id
        }));

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
        {
            return timeZone;
        }

        Assert.True(TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId));
        return TimeZoneInfo.FindSystemTimeZoneById(windowsId!);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
