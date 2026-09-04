using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using TravelTracker.Services;

namespace TravelTracker.Tests.Services;

public class PrincipalAccessorTests
{
    [Fact]
    public async Task AuthenticationStateAccessor_ReturnsAuthenticatedPrincipal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity("test"));
        var provider = new Mock<AuthenticationStateProvider>();
        provider.Setup(value => value.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(principal));
        var accessor = new AuthenticationStatePrincipalAccessor(provider.Object);

        var result = await accessor.GetCurrentPrincipalAsync();

        Assert.Same(principal, result);
    }

    [Fact]
    public async Task AuthenticationStateAccessor_WhenCancelled_Throws()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var provider = new Mock<AuthenticationStateProvider>();
        var accessor = new AuthenticationStatePrincipalAccessor(provider.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => accessor.GetCurrentPrincipalAsync(source.Token));
        provider.Verify(value => value.GetAuthenticationStateAsync(), Times.Never);
    }

    [Fact]
    public async Task HttpContextAccessor_ReturnsCurrentUser()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity("test"));
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        var accessor = new HttpContextPrincipalAccessor(httpContextAccessor);

        var result = await accessor.GetCurrentPrincipalAsync();

        Assert.Same(principal, result);
    }

    [Fact]
    public async Task HttpContextAccessor_WhenContextMissing_ReturnsNull()
    {
        var accessor = new HttpContextPrincipalAccessor(new HttpContextAccessor());

        var result = await accessor.GetCurrentPrincipalAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task HttpContextAccessor_WhenCancelled_Throws()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var accessor = new HttpContextPrincipalAccessor(new HttpContextAccessor());

        await Assert.ThrowsAsync<OperationCanceledException>(() => accessor.GetCurrentPrincipalAsync(source.Token));
    }
}