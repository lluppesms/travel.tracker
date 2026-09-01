using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Models;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class CurrentTravelUserResolverTests
{
    private const string ObjectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    private static CurrentTravelUserResolver CreateResolver(
        Mock<IUserService> userService,
        ClaimsPrincipal? ambientPrincipal = null)
    {
        var principalAccessor = new Mock<ICurrentPrincipalAccessor>();
        principalAccessor
            .Setup(accessor => accessor.GetCurrentPrincipalAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ambientPrincipal);

        return new CurrentTravelUserResolver(
            userService.Object,
            principalAccessor.Object,
            NullLogger<CurrentTravelUserResolver>.Instance);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "TestAuth"));

    [Fact]
    public async Task ResolveAsync_ReturnsNull_WhenPrincipalIsUnauthenticated()
    {
        var userService = new Mock<IUserService>();
        var resolver = CreateResolver(userService);
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await resolver.ResolveAsync(principal);

        Assert.Null(result);
        userService.Verify(
            service => service.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_WhenPrincipalIsNull()
    {
        var userService = new Mock<IUserService>();
        var resolver = CreateResolver(userService);

        var result = await resolver.ResolveAsync(null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_ResolvesInternalUser_FromObjectIdentifierClaim()
    {
        var userService = new Mock<IUserService>();
        userService
            .Setup(service => service.GetOrCreateUserAsync("entra-oid-1", "Test User", "test@example.com"))
            .ReturnsAsync(new User { Id = 42, Username = "Test User", Email = "test@example.com" });

        var resolver = CreateResolver(userService);
        var principal = CreateAuthenticatedPrincipal(
            new Claim(ObjectIdClaimType, "entra-oid-1"),
            new Claim("name", "Test User"),
            new Claim("email", "test@example.com"));

        var result = await resolver.ResolveAsync(principal);

        Assert.NotNull(result);
        Assert.Equal(42, result!.UserId);
        Assert.Equal("entra-oid-1", result.ExternalId);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal("test@example.com", result.Email);
        userService.Verify(
            service => service.GetOrCreateUserAsync("entra-oid-1", "Test User", "test@example.com"),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_ResolvesInternalUser_FromShortOidClaim()
    {
        var userService = new Mock<IUserService>();
        userService
            .Setup(service => service.GetOrCreateUserAsync("oid-9", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new User { Id = 9 });

        var resolver = CreateResolver(userService);
        var principal = CreateAuthenticatedPrincipal(new Claim("oid", "oid-9"));

        var result = await resolver.ResolveAsync(principal);

        Assert.NotNull(result);
        Assert.Equal(9, result!.UserId);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_WhenAuthenticatedPrincipalHasNoIdentifierClaim()
    {
        var userService = new Mock<IUserService>();
        var resolver = CreateResolver(userService);
        var principal = CreateAuthenticatedPrincipal(new Claim("name", "No Id User"));

        var result = await resolver.ResolveAsync(principal);

        Assert.Null(result);
        userService.Verify(
            service => service.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveCurrentAsync_ReturnsNull_WhenOnlyApiKeyHeaderIsPresent()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-API-Key"] = "global-api-key";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var userService = new Mock<IUserService>();
        var resolver = CreateResolver(userService, httpContext.User);

        var result = await resolver.ResolveCurrentAsync();

        Assert.Null(result);
        userService.Verify(service => service.GetUserByApiKeyAsync(It.IsAny<string>()), Times.Never);
        userService.Verify(service => service.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
        userService.Verify(
            service => service.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_IgnoresClaimedInternalUserId_AndUsesResolvedUser()
    {
        var userService = new Mock<IUserService>();
        userService
            .Setup(service => service.GetOrCreateUserAsync("entra-oid-1", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new User { Id = 7 });

        var resolver = CreateResolver(userService);
        var principal = CreateAuthenticatedPrincipal(
            new Claim(ObjectIdClaimType, "entra-oid-1"),
            new Claim("userId", "999"),
            new Claim("UserId", "999"));

        var result = await resolver.ResolveAsync(principal);

        Assert.NotNull(result);
        Assert.Equal(7, result!.UserId);
    }

    [Fact]
    public async Task ResolveCurrentAsync_UsesAmbientPrincipal()
    {
        var userService = new Mock<IUserService>();
        userService
            .Setup(service => service.GetOrCreateUserAsync("circuit-oid", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new User { Id = 15 });

        var principal = CreateAuthenticatedPrincipal(new Claim("oid", "circuit-oid"));
        var resolver = CreateResolver(userService, principal);

        var result = await resolver.ResolveCurrentAsync();

        Assert.NotNull(result);
        Assert.Equal(15, result!.UserId);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_WhenUserServiceReturnsNoUsableUser()
    {
        var userService = new Mock<IUserService>();
        userService
            .Setup(service => service.GetOrCreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((User)null!);

        var resolver = CreateResolver(userService);
        var principal = CreateAuthenticatedPrincipal(new Claim("oid", "entra-oid-1"));

        var result = await resolver.ResolveAsync(principal);

        Assert.Null(result);
    }

    [Fact]
    public void TravelAssistantUserContext_Throws_WhenUserIdIsNotPositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new TravelAssistantUserContext(0, "oid", "name", "email@example.com"));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(42, true)]
    [InlineData(43, false)]
    public void LegacyUserIdPolicy_AcceptsOnlyMatchingOrMissingValues(int? legacyUserId, bool expected)
    {
        var context = new TravelAssistantUserContext(42, "oid", "Test User", "test@example.com");

        Assert.Equal(expected, LegacyUserIdPolicy.IsLegacyUserIdAcceptable(context, legacyUserId));
    }

    [Fact]
    public void LegacyUserIdPolicy_EvaluateReportsSpecificOutcomes()
    {
        var context = new TravelAssistantUserContext(42, "oid", "Test User", "test@example.com");

        Assert.Equal(LegacyUserIdEvaluation.NotSupplied, LegacyUserIdPolicy.Evaluate(context, null));
        Assert.Equal(LegacyUserIdEvaluation.MatchesResolvedUser, LegacyUserIdPolicy.Evaluate(context, 42));
        Assert.Equal(LegacyUserIdEvaluation.Mismatched, LegacyUserIdPolicy.Evaluate(context, 43));
    }
}
