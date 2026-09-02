using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TravelTracker.Data.Configuration;

namespace TravelTracker.Services.Configuration;

/// <summary>
/// Registration helpers for <see cref="TravelAssistantOptions"/> and its startup validation.
/// </summary>
public static class TravelAssistantOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Binds the TravelAssistant configuration section and fails startup when the configuration is
    /// invalid, incomplete for the selected provider, or missing authentication/action storage.
    /// </summary>
    public static IServiceCollection AddTravelAssistantOptions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TravelAssistantOptions>()
            .Bind(configuration.GetSection(TravelAssistantOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<TravelAssistantOptions>>(
            _ => new TravelAssistantOptionsValidator(configuration));

        return services;
    }
}
