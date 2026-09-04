using System.Security.Claims;
using TravelTracker.Services;

namespace TravelTracker.Tests.Services;

public class UnavailableTravelUserResolverTests
{
    [Fact]
    public async Task ResolveAsync_ReturnsNoUser()
    {
        var result = await new UnavailableTravelUserResolver().ResolveAsync(new ClaimsPrincipal());

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveCurrentAsync_ReturnsNoUser()
    {
        var result = await new UnavailableTravelUserResolver().ResolveCurrentAsync();

        Assert.Null(result);
    }
}