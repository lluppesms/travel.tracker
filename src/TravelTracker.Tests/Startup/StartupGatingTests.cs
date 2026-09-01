using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TravelTracker.Extensions;
using TravelTracker.Services;
using TravelTracker.Services.Configuration;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Models;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Startup;

/// <summary>
/// Guards the startup gating that keeps the application running when SQL Server or Azure AD are absent.
/// </summary>
public class StartupGatingTests
{
    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ServiceCollection BuildHostServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticationStateProvider, EmptyAuthenticationStateProvider>();

        var failures = ChatProviderServiceCollectionExtensions.GetAssistantPrerequisiteFailures(configuration);
        var assistantEnabled = failures.Count == 0;
        var sqlConfigured = !string.IsNullOrWhiteSpace(AssistantConnectionStrings.Resolve(configuration));

        services.AddTravelAssistantReadiness(assistantEnabled, failures);

        if (sqlConfigured)
        {
            services.AddScoped(_ => new Moq.Mock<IUserService>().Object);
            services.AddTravelAssistantIdentity();
        }
        else
        {
            services.AddUnavailableTravelAssistantIdentity();
        }

        if (assistantEnabled)
        {
            services.AddTravelAssistantChatProvider(configuration);
        }
        else
        {
            services.AddDisabledTravelAssistantChatProvider();
        }

        return services;
    }

    private static ServiceProvider Build(ServiceCollection services)
        => services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    [Fact]
    public void NoSqlConfiguration_BuildsServiceProviderWithValidationEnabled()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["TravelAssistant:Provider"] = "AgentFramework"
        });

        using var provider = Build(BuildHostServices(configuration));
        using var scope = provider.CreateScope();

        Assert.IsType<UnavailableTravelUserResolver>(scope.ServiceProvider.GetRequiredService<ICurrentTravelUserResolver>());
        Assert.False(provider.GetRequiredService<TravelAssistantReadiness>().IsReady);
    }

    [Fact]
    public async Task DisabledAssistant_ResolvesDisabledChatbotServiceAndReportsProviderUnavailable()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["TravelAssistant:Provider"] = "AgentFramework"
        });

        using var provider = Build(BuildHostServices(configuration));
        using var scope = provider.CreateScope();

        var chatbotService = scope.ServiceProvider.GetRequiredService<IChatbotService>();
        Assert.IsType<DisabledChatbotService>(chatbotService);

        var result = await chatbotService.GetChatResponseAsync("hi", 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(ChatErrorCodes.ProviderUnavailable, result.ErrorCode);
        Assert.Equal(503, result.HttpStatusCode);
        Assert.Equal(string.Empty, result.ThreadId);
        Assert.Equal("The travel assistant is not configured.", result.Message);
    }

    [Fact]
    public async Task UnavailableTravelUserResolver_AlwaysResolvesToNoUser()
    {
        var resolver = new UnavailableTravelUserResolver();

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "external-1")], "TestAuth");

        Assert.Null(await resolver.ResolveAsync(new ClaimsPrincipal(identity), CancellationToken.None));
        Assert.Null(await resolver.ResolveCurrentAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("")]
    [InlineData(null)]
    public void BlankAzureAdTenantId_IsTreatedAsNotConfiguredByBothPredicates(string? tenantId)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["AzureAd:TenantId"] = tenantId,
            ["AzureAd:ClientId"] = "22222222-2222-2222-2222-222222222222",
            ["SqlServer:ConnectionString"] = "Server=(local);Database=Test;Trusted_Connection=True;",
            ["TravelAssistant:Provider"] = "AgentFramework"
        });

        // The host predicate used by Program.cs.
        var azureAdConfigured = !string.IsNullOrWhiteSpace(configuration[TravelAssistantOptionsValidator.AzureAdTenantIdKey]) &&
                                !string.IsNullOrWhiteSpace(configuration[TravelAssistantOptionsValidator.AzureAdClientIdKey]);

        var failures = ChatProviderServiceCollectionExtensions.GetAssistantPrerequisiteFailures(configuration);

        Assert.False(azureAdConfigured);
        Assert.Contains(failures, f => f.Contains(TravelAssistantOptionsValidator.AzureAdTenantIdKey, StringComparison.Ordinal));
    }

    [Fact]
    public void ConnectionStringResolution_MatchesTheAssistantPrerequisiteCheck()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["SqlServer:ConnectionString"] = "   ",
            ["ConnectionStrings:DefaultConnection"] = "Server=(local);Database=Test;Trusted_Connection=True;",
            ["AzureAd:TenantId"] = "11111111-1111-1111-1111-111111111111",
            ["AzureAd:ClientId"] = "22222222-2222-2222-2222-222222222222",
            ["TravelAssistant:Provider"] = "AgentFramework"
        });

        var resolved = AssistantConnectionStrings.Resolve(configuration);
        var failures = ChatProviderServiceCollectionExtensions.GetAssistantPrerequisiteFailures(configuration);

        Assert.False(string.IsNullOrWhiteSpace(resolved));
        Assert.Empty(failures);
    }

    [Fact]
    public void SqlConfigured_UsesTheRealIdentityResolver()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["SqlServer:ConnectionString"] = "Server=(local);Database=Test;Trusted_Connection=True;",
            ["AzureAd:TenantId"] = "11111111-1111-1111-1111-111111111111",
            ["AzureAd:ClientId"] = "22222222-2222-2222-2222-222222222222",
            ["TravelAssistant:Provider"] = "AgentFramework"
        });

        var services = BuildHostServices(configuration);

        Assert.Equal(
            typeof(CurrentTravelUserResolver),
            services.Single(d => d.ServiceType == typeof(ICurrentTravelUserResolver)).ImplementationType);
        Assert.Equal(
            typeof(ChatbotService),
            services.Single(d => d.ServiceType == typeof(IChatbotService)).ImplementationType);
    }

    private sealed class EmptyAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
