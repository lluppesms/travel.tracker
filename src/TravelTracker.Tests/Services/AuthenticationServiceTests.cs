using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class AuthenticationServiceTests
{
    [Fact]
    public void UnauthenticatedUser_UsesFallbackIdentityValues()
    {
        var service = CreateService(new DefaultHttpContext(), new Dictionary<string, string?>());

        Assert.Equal(0, service.GetCurrentUserInternalId());
        Assert.Equal("TEST_USER_ENTRA", service.GetCurrentUserId());
        Assert.Equal(string.Empty, service.GetCurrentUserEntraId());
        Assert.Equal("Test User", service.GetCurrentUserDisplayName());
        Assert.Equal("test@example.com", service.GetCurrentUserEmail());
        Assert.False(service.IsGlobalApiKeyUser());
    }

    [Fact]
    public void MatchingSignedInUser_IsAuthorized()
    {
        var context = AuthenticatedContext(new Claim("oid", "entra-1"));
        var users = new Mock<IUserService>();
        users.Setup(service => service.GetOrCreateUserAsync("entra-1", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new User { Id = 7 });
        var authentication = CreateService(context, new Dictionary<string, string?>(), users);

        var result = authentication.ValidateUserAccess(7);

        Assert.Equal(7, result.UserId);
        Assert.Equal(HttpMessages.AuthorizedUser, result.ErrorMessage);
    }

    [Fact]
    public void ConfiguredApiKey_ReturnsConfiguredUser()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-Key"] = "global-key";
        var users = new Mock<IUserService>();
        users.Setup(service => service.GetUserByIdAsync(12)).ReturnsAsync(new User { Id = 12, Email = "owner@example.com" });
        var authentication = CreateService(context, new Dictionary<string, string?>
        {
            ["ApiKey"] = "global-key",
            ["ApiKey_UserID"] = "12",
            ["ApiKey_EmailAddress"] = "owner@example.com"
        }, users);

        Assert.Equal(12, authentication.GetCurrentUserInternalId());
        Assert.True(authentication.IsGlobalApiKeyUser());
    }

    [Fact]
    public void UserApiKey_ReturnsMatchingUser()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-API-Key"] = "user-key";
        var users = new Mock<IUserService>();
        users.Setup(service => service.GetUserByApiKeyAsync("user-key")).ReturnsAsync(new User { Id = 19 });
        var authentication = CreateService(context, new Dictionary<string, string?> { ["ApiKey"] = "other-key" }, users);

        Assert.Equal(19, authentication.GetCurrentUserInternalId());
    }

    [Theory]
    [InlineData("name", "Ada", "Ada")]
    [InlineData("http://schemas.microsoft.com/identity/claims/objectidentifier", "entra-2", "entra-2")]
    [InlineData("email", "ada@example.com", "ada@example.com")]
    public void AuthenticatedClaims_AreRead(string claimType, string value, string expected)
    {
        var service = CreateService(AuthenticatedContext(new Claim(claimType, value)), new Dictionary<string, string?>());

        var actual = claimType == "name"
            ? service.GetCurrentUserDisplayName()
            : claimType == "email" ? service.GetCurrentUserEmail() : service.GetCurrentUserEntraId();

        Assert.Equal(expected, actual);
    }

    private static AuthenticationService CreateService(
        HttpContext context,
        IDictionary<string, string?> settings,
        Mock<IUserService>? users = null)
    {
        var accessor = new HttpContextAccessor { HttpContext = context };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new AuthenticationService(accessor, (users ?? new Mock<IUserService>()).Object, configuration);
    }

    private static DefaultHttpContext AuthenticatedContext(Claim claim)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([claim], "test"))
        };
        return context;
    }
}