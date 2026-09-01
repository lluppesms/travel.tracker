using Microsoft.Extensions.Options;

using TravelTracker.Services;
using TravelTracker.Services.Configuration;

namespace TravelTracker.Extensions;

/// <summary>
/// Travel assistant registration (REQ-002, OPS-008, SEC-004) using the documented service lifetime matrix.
/// All failure messages name configuration KEYS only; configuration values are never included.
/// </summary>
public static class ChatProviderServiceCollectionExtensions
{
    /// <summary>
    /// Evaluates the assistant prerequisites that the legacy application treats as optional
    /// (Entra authentication and SQL action storage). Returns key-only failure messages.
    /// </summary>
    public static IReadOnlyList<string> GetAssistantPrerequisiteFailures(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var failures = new List<string>();
        failures.AddRange(TravelAssistantOptionsValidator.ValidateAuthentication(configuration));
        failures.AddRange(TravelAssistantOptionsValidator.ValidateActionStorage(configuration));
        return failures;
    }

    /// <summary>
    /// Registers the scoped identity services required by every assistant entry point.
    /// </summary>
    public static IServiceCollection AddTravelAssistantIdentity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICurrentPrincipalAccessor, CurrentPrincipalAccessor>();
        services.AddScoped<ICurrentTravelUserResolver, CurrentTravelUserResolver>();

        return services;
    }

    /// <summary>
    /// Registers the identity services used when SQL action storage is absent and <c>IUserService</c>
    /// therefore does not exist. Assistant entry points still activate and resolve to no user.
    /// </summary>
    public static IServiceCollection AddUnavailableTravelAssistantIdentity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICurrentPrincipalAccessor, CurrentPrincipalAccessor>();
        services.AddScoped<ICurrentTravelUserResolver, UnavailableTravelUserResolver>();

        return services;
    }

    /// <summary>
    /// Registers the chat provider used when the assistant prerequisites are missing, so callers receive a
    /// stable <c>provider_unavailable</c> result instead of a dependency injection failure.
    /// </summary>
    public static IServiceCollection AddDisabledTravelAssistantChatProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IChatbotService, DisabledChatbotService>();

        return services;
    }

    /// <summary>
    /// Records whether the assistant surface is available so pages and endpoints can report a
    /// clear reason instead of failing with a dependency injection error.
    /// </summary>
    public static IServiceCollection AddTravelAssistantReadiness(this IServiceCollection services, bool isReady, IReadOnlyList<string> failures)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(failures);

        services.AddSingleton(new TravelAssistantReadiness(isReady, failures));

        return services;
    }

    /// <summary>
    /// Selects the chat provider implementation from configuration. Startup fails fast when the
    /// selected provider is unknown or is not available in this release.
    /// </summary>
    public static IServiceCollection AddTravelAssistantChatProvider(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(TravelAssistantOptions.SectionName).Get<TravelAssistantOptions>()
            ?? new TravelAssistantOptions();

        return services.AddTravelAssistantChatProvider(options.Provider);
    }

    /// <summary>
    /// Single provider selection point. Phase 3 (TASK-018) adds the <c>CopilotSDK</c> registration here.
    /// </summary>
    public static IServiceCollection AddTravelAssistantChatProvider(this IServiceCollection services, ChatProvider provider)
    {
        ArgumentNullException.ThrowIfNull(services);

        switch (provider)
        {
            case ChatProvider.AgentFramework:
                services.AddScoped<IChatbotService, ChatbotService>();
                break;

            case ChatProvider.CopilotSDK:
                // Phase 3 TASK-018 replaces this throw with:
                //   services.AddScoped<IChatbotService, CopilotChatbotService>();
                throw new OptionsValidationException(
                    TravelAssistantOptions.SectionName,
                    typeof(TravelAssistantOptions),
                    [$"Chat provider '{nameof(ChatProvider.CopilotSDK)}' is not yet available; set {TravelAssistantOptions.SectionName}:Provider to '{nameof(ChatProvider.AgentFramework)}'."]);

            default:
                throw new OptionsValidationException(
                    TravelAssistantOptions.SectionName,
                    typeof(TravelAssistantOptions),
                    [$"{TravelAssistantOptions.SectionName}:Provider must be '{nameof(ChatProvider.AgentFramework)}' or '{nameof(ChatProvider.CopilotSDK)}'."]);
        }

        return services;
    }
}
