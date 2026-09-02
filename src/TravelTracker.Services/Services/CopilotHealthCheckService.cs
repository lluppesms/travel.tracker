using Microsoft.Extensions.Options;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Configuration;

namespace TravelTracker.Services.Services;

/// <summary>
/// Checks readiness of the Copilot runtime by verifying:
/// - Authentication configuration (Entra)
/// - SQL action storage configuration
/// - Runtime client is started and healthy
/// - Runtime ping succeeds within timeout
/// - Foundry provider configuration is available
/// </summary>
public class CopilotHealthCheckService : ICopilotHealthCheckService
{
    private readonly ILogger<CopilotHealthCheckService> _logger;
    private readonly ICopilotRuntimeAccessor _runtimeAccessor;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<TravelAssistantOptions> _assistantOptions;

    public CopilotHealthCheckService(
        ILogger<CopilotHealthCheckService> logger,
        ICopilotRuntimeAccessor runtimeAccessor,
        IConfiguration configuration,
        IOptionsMonitor<TravelAssistantOptions> assistantOptions)
    {
        _logger = logger;
        _runtimeAccessor = runtimeAccessor;
        _configuration = configuration;
        _assistantOptions = assistantOptions;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        var failures = await GetFailureReasonsAsync(cancellationToken);
        return failures.Count == 0;
    }

    public async Task<IReadOnlyList<string>> GetFailureReasonsAsync(CancellationToken cancellationToken = default)
    {
        var failures = new List<string>();

        // Check authentication configuration
        failures.AddRange(TravelAssistantOptionsValidator.ValidateAuthentication(_configuration));

        // Check SQL action storage configuration
        failures.AddRange(TravelAssistantOptionsValidator.ValidateActionStorage(_configuration));

        // Check runtime is ready
        if (!_runtimeAccessor.IsReady)
        {
            failures.Add("CopilotClient:Runtime");
        }

        // Check runtime ping succeeds (only if runtime is ready)
        if (_runtimeAccessor.IsReady)
        {
            try
            {
                var pingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                pingCts.CancelAfter(TimeSpan.FromSeconds(5));

                var isHealthy = await _runtimeAccessor.PingAsync(pingCts.Token);
                if (!isHealthy)
                {
                    failures.Add("CopilotClient:Ping");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Runtime health check timed out.");
                failures.Add("CopilotClient:PingTimeout");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Runtime health check failed.");
                failures.Add("CopilotClient:Ping");
            }
        }

        // Check Foundry provider configuration
        var options = _assistantOptions.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.FoundryEndpoint))
        {
            failures.Add("TravelAssistant:FoundryEndpoint");
        }

        if (string.IsNullOrWhiteSpace(options.ModelDeploymentName))
        {
            failures.Add("TravelAssistant:ModelDeploymentName");
        }

        return failures;
    }
}
