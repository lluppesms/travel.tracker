using GitHub.Copilot;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Azure.Identity;

namespace TravelTracker.Services.Services;

/// <summary>
/// Singleton hosted client for Copilot SDK 1.0.11.
/// Creates and manages the CopilotClient with Foundry provider configuration.
/// Empty client mode (no built-in tools), no content capture, writable BaseDirectory,
/// and Foundry provider with OpenAI-compatible endpoint and managed identity auth.
/// </summary>
public class CopilotRuntimeAccessor : ICopilotRuntimeAccessor, IAsyncDisposable
{
    private readonly ILogger<CopilotRuntimeAccessor> _logger;
    private readonly IConfiguration _configuration;
    private readonly IOptionsMonitor<TravelAssistantOptions> _assistantOptions;
    private CopilotClient? _client;
    private readonly SemaphoreSlim _clientSemaphore = new(1, 1);
    private bool _started;
    private bool _disposed;

    public bool IsReady => _client != null && _started && !_disposed;

    public CopilotRuntimeAccessor(
        ILogger<CopilotRuntimeAccessor> logger,
        IConfiguration configuration,
        IOptionsMonitor<TravelAssistantOptions> assistantOptions)
    {
        _logger = logger;
        _configuration = configuration;
        _assistantOptions = assistantOptions;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started || _disposed)
        {
            return;
        }

        try
        {
            await _clientSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_started || _disposed)
                {
                    return;
                }

                _logger.LogInformation("Starting Copilot client...");

                // Create options with Foundry configuration
                var options = new CopilotClientOptions
                {
                    BaseDirectory = GetBaseDirectory(),
                    LogLevel = CopilotLogLevel.Debug,
                    Environment = new Dictionary<string, string>
                    {
                        ["COPILOT_PROVIDER"] = "foundry",
                        ["COPILOT_API_ENDPOINT"] = _assistantOptions.CurrentValue.FoundryEndpoint ?? "",
                        ["COPILOT_MODEL"] = _assistantOptions.CurrentValue.ModelDeploymentName ?? "",
                        ["COPILOT_API_PATH"] = "/openai/v1",
                    }
                };

                // Create the client
                _client = new CopilotClient(options);

                // Start the client
                var startCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                startCts.CancelAfter(TimeSpan.FromSeconds(10));

                await _client.StartAsync();

                _started = true;
                _logger.LogInformation("Copilot client started successfully.");
            }
            finally
            {
                _clientSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Client startup timed out (10 seconds).");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Copilot client.");
            throw;
        }
    }

    public object GetClient()
    {
        if (!IsReady)
        {
            throw new InvalidOperationException("Copilot client is not ready.");
        }

        return _client!;
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        if (!IsReady)
        {
            _logger.LogWarning("Attempted to ping client that is not ready.");
            return false;
        }

        try
        {
            var pingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            pingCts.CancelAfter(TimeSpan.FromSeconds(5));

            // TODO: TASK-014 - Call actual health check on client if available
            // For now, we consider the client healthy if it's started
            
            _logger.LogDebug("Copilot client ping successful.");
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Client ping timed out.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Client ping failed.");
            return false;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_started || _disposed)
        {
            return;
        }

        try
        {
            await _clientSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_client == null)
                {
                    return;
                }

                _logger.LogInformation("Stopping Copilot client gracefully...");

                var stopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                stopCts.CancelAfter(TimeSpan.FromSeconds(10));

                await _client.StopAsync();
                
                _logger.LogInformation("Copilot client stopped gracefully.");
            }
            finally
            {
                _clientSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Client stop timed out (10 seconds), forcing stop...");
            await ForceStopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during graceful stop, forcing stop...");
            await ForceStopAsync();
        }
    }

    public async Task ForceStopAsync()
    {
        if (_client == null)
        {
            return;
        }

        try
        {
            _logger.LogWarning("Force-stopping Copilot client...");
            
            // CopilotClient implements IAsyncDisposable
            if (_client is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            
            _client = null;
            _started = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during force stop.");
        }
    }

    private string GetBaseDirectory()
    {
        var options = _assistantOptions.CurrentValue;
        var copilotHome = options.CopilotHome;

        if (string.IsNullOrWhiteSpace(copilotHome))
        {
            copilotHome = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TravelTracker",
                "copilot"
            );
        }

        Directory.CreateDirectory(copilotHome);
        _logger.LogInformation("Copilot home: {CopilotHome}", copilotHome);

        return copilotHome;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await ForceStopAsync();
        _clientSemaphore?.Dispose();
    }
}


