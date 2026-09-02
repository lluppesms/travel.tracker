using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;
using TravelTracker.Services.Interfaces;

namespace TravelTracker.Controllers;

/// <summary>
/// Readiness and liveness probes for the Travel Tracker Copilot service.
/// Returns 200 OK if ready, 503 Service Unavailable if not.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly ICopilotHealthCheckService _healthCheckService;
    private readonly ILogger<HealthCheckController> _logger;

    public HealthCheckController(
        ICopilotHealthCheckService healthCheckService,
        ILogger<HealthCheckController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint that verifies:
    /// - Authentication configuration (Entra AD)
    /// - SQL action storage connectivity
    /// - Copilot runtime is started and healthy
    /// - Copilot runtime ping succeeds
    /// - Foundry provider configuration is available
    /// </summary>
    /// <remarks>
    /// No authentication required. This endpoint is designed for Kubernetes/container health probes.
    /// Returns detailed failure reasons only in logs (never in response body for security).
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK if all checks pass; 503 Service Unavailable if any check fails.</returns>
    [AllowAnonymous]
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            var isHealthy = await _healthCheckService.IsHealthyAsync(cancellationToken);

            if (isHealthy)
            {
                _logger.LogDebug("Health check passed.");
                return Ok();
            }

            var failures = await _healthCheckService.GetFailureReasonsAsync(cancellationToken);
            _logger.LogWarning(
                "Health check failed; missing configuration: {MissingConfigurationKeys}",
                string.Join(", ", failures));

            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// Liveness probe endpoint that indicates whether the service is running.
    /// Always returns 200 OK if the service is responding.
    /// </summary>
    /// <remarks>
    /// This endpoint is suitable for container liveness probes that detect hung services.
    /// No authentication required.
    /// </remarks>
    /// <returns>200 OK.</returns>
    [AllowAnonymous]
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok();
    }
}
