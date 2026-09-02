using Microsoft.Extensions.Options;
using TravelTracker.Data.Configuration;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Services;

/// <summary>
/// Starts, verifies, and stops the singleton Copilot SDK runtime with the application host.
/// </summary>
public sealed class CopilotRuntimeHostedService(
    ICopilotRuntimeAccessor runtimeAccessor,
    ICopilotSessionCoordinator sessionCoordinator,
    IOptionsMonitor<TravelAssistantOptions> assistantOptions,
    TravelAssistantReadiness readiness,
    ILogger<CopilotRuntimeHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!readiness.IsReady || assistantOptions.CurrentValue.Provider != ChatProvider.CopilotSDK)
        {
            return;
        }

        await sessionCoordinator.CleanupAbandonedSessionsAsync(cancellationToken).ConfigureAwait(false);
        await runtimeAccessor.StartAsync(cancellationToken).ConfigureAwait(false);
        if (!await runtimeAccessor.PingAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Copilot SDK runtime readiness ping failed.");
        }

        logger.LogInformation("Copilot SDK runtime is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => runtimeAccessor.StopAsync(cancellationToken);
}
