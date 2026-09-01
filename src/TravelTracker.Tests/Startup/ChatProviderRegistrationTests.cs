using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Moq;

using TravelTracker.Data.Configuration;
using TravelTracker.Extensions;
using TravelTracker.Services;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Startup;

public class ChatProviderRegistrationTests
{
    private static IConfiguration BuildConfiguration(string provider, bool includeAuthentication = true, bool includeSql = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["TravelAssistant:Provider"] = provider,
            ["TravelAssistant:WriteMode"] = "Confirm",
            ["TravelAssistant:TimeZoneId"] = "America/Chicago"
        };

        if (includeAuthentication)
        {
            values["AzureAd:TenantId"] = "11111111-1111-1111-1111-111111111111";
            values["AzureAd:ClientId"] = "22222222-2222-2222-2222-222222222222";
        }

        if (includeSql)
        {
            values["SqlServer:ConnectionString"] = "Server=(local);Database=Test;Trusted_Connection=True;";
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static ServiceCollection BuildServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticationStateProvider, FakeAuthenticationStateProvider>();
        services.AddScoped(_ => new Mock<IUserService>().Object);
        services.AddTravelAssistantIdentity();
        return services;
    }

    private static ServiceProvider Build(ServiceCollection services)
        => services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

    [Fact]
    public void AgentFrameworkProvider_RegistersChatbotService()
    {
        var configuration = BuildConfiguration("AgentFramework");
        var services = BuildServices(configuration);

        services.AddTravelAssistantChatProvider(configuration);

        var descriptor = services.Single(d => d.ServiceType == typeof(IChatbotService));
        Assert.Equal(typeof(ChatbotService), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void CopilotSdkProvider_FailsFastWithNonSecretMessage()
    {
        var configuration = BuildConfiguration("CopilotSDK");
        var services = BuildServices(configuration);

        var exception = Assert.Throws<OptionsValidationException>(() => services.AddTravelAssistantChatProvider(configuration));

        Assert.Contains("CopilotSDK", exception.Message, StringComparison.Ordinal);
        Assert.Contains("TravelAssistant:Provider", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AgentFramework", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Server=", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("11111111", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownProvider_FailsFast()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<OptionsValidationException>(() => services.AddTravelAssistantChatProvider((ChatProvider)42));

        Assert.Contains("TravelAssistant:Provider", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CurrentTravelUserResolver_ResolvesFromScope()
    {
        var configuration = BuildConfiguration("AgentFramework");
        var services = BuildServices(configuration);

        using var provider = Build(services);
        using var scope = provider.CreateScope();

        var resolver = scope.ServiceProvider.GetRequiredService<ICurrentTravelUserResolver>();
        Assert.IsType<CurrentTravelUserResolver>(resolver);

        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>();
        Assert.IsType<CurrentPrincipalAccessor>(accessor);

        Assert.Null(await resolver.ResolveCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public void ScopedIdentityServices_CannotBeResolvedFromRootScope()
    {
        var configuration = BuildConfiguration("AgentFramework");
        var services = BuildServices(configuration);

        using var provider = Build(services);

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ICurrentTravelUserResolver>());
    }

    [Fact]
    public void SingletonCapturingScopedService_FailsScopeValidation()
    {
        var configuration = BuildConfiguration("AgentFramework");
        var services = BuildServices(configuration);
        services.AddSingleton<CapturingSingleton>();

        Assert.ThrowsAny<Exception>(() => Build(services));
    }

    [Fact]
    public void MissingPrerequisites_ReportKeysOnly()
    {
        var configuration = BuildConfiguration("AgentFramework", includeAuthentication: false, includeSql: false);

        var failures = ChatProviderServiceCollectionExtensions.GetAssistantPrerequisiteFailures(configuration);

        Assert.Contains(failures, f => f.Contains("AzureAd:TenantId", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("AzureAd:ClientId", StringComparison.Ordinal));
        Assert.Contains(failures, f => f.Contains("SqlServer:ConnectionString", StringComparison.Ordinal));
        Assert.All(failures, f => Assert.DoesNotContain("Server=", f, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CompletePrerequisites_ProduceNoFailures()
    {
        var configuration = BuildConfiguration("AgentFramework");

        Assert.Empty(ChatProviderServiceCollectionExtensions.GetAssistantPrerequisiteFailures(configuration));
    }

    private sealed class CapturingSingleton(ICurrentTravelUserResolver resolver)
    {
        public ICurrentTravelUserResolver Resolver { get; } = resolver;
    }

    private sealed class FakeAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
