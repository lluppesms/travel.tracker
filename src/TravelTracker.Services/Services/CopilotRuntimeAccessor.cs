using Azure.Core;
using Azure.Identity;
using GitHub.Copilot;
using Microsoft.Extensions.Options;

namespace TravelTracker.Services.Services;

/// <summary>
/// Owns the singleton Copilot SDK runtime and its Foundry provider configuration.
/// </summary>
public sealed class CopilotRuntimeAccessor(
    ILogger<CopilotRuntimeAccessor> logger,
    IOptionsMonitor<TravelAssistantOptions> assistantOptions,
    DefaultAzureCredential credential) : ICopilotRuntimeAccessor, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CopilotClient? _client;
    private bool _disposed;

    public bool IsReady { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsReady)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (IsReady)
            {
                return;
            }

            var options = assistantOptions.CurrentValue;
            Directory.CreateDirectory(options.CopilotHome);

            _client = new CopilotClient(new CopilotClientOptions
            {
                Mode = CopilotClientMode.Empty,
                BaseDirectory = options.CopilotHome,
                SessionIdleTimeoutSeconds = options.SessionIdleTimeoutMinutes * 60,
                LogLevel = CopilotLogLevel.Info,
                Telemetry = new TelemetryConfig { CaptureContent = false }
            });

            await _client.StartAsync(cancellationToken).ConfigureAwait(false);
            await _client.PingAsync("travel-tracker-readiness", cancellationToken).ConfigureAwait(false);
            IsReady = true;
            logger.LogInformation("Copilot SDK runtime started and responded to ping.");
        }
        catch
        {
            IsReady = false;
            if (_client is not null)
            {
                await _client.ForceStopAsync().ConfigureAwait(false);
                _client = null;
            }

            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ICopilotSessionHandle> CreateSessionAsync(
        SessionConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        var client = GetReadyClient();
        var options = assistantOptions.CurrentValue;
        var endpoint = options.FoundryEndpoint.TrimEnd('/') + "/openai/v1/";

        config.Model = options.ModelDeploymentName;
        config.Provider = new ProviderConfig
        {
            Type = "openai",
            BaseUrl = endpoint,
            WireApi = "responses",
            ModelId = options.ModelDeploymentName,
#pragma warning disable GHCP001 // Required SDK 1.0.11 managed-identity callback.
            BearerTokenProvider = async _ =>
            {
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext([options.TokenScope]),
                    CancellationToken.None).ConfigureAwait(false);
                return token.Token;
            }
#pragma warning restore GHCP001
        };

        var session = await client.CreateSessionAsync(config, cancellationToken).ConfigureAwait(false);
        return new CopilotSessionHandle(session);
    }

    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        await GetReadyClient().DeleteSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        if (!IsReady || _client is null)
        {
            return false;
        }

        try
        {
            await _client.PingAsync("travel-tracker-readiness", cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Copilot SDK runtime ping failed.");
            IsReady = false;
            return false;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_client is null)
            {
                IsReady = false;
                return;
            }

            try
            {
                await _client.StopAsync()
                    .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is TimeoutException or OperationCanceledException)
            {
                logger.LogWarning(exception, "Copilot SDK graceful stop did not complete; forcing shutdown.");
                await _client.ForceStopAsync().ConfigureAwait(false);
            }

            IsReady = false;
            _client = null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ForceStopAsync()
    {
        var client = _client;
        IsReady = false;
        _client = null;
        if (client is not null)
        {
            await client.ForceStopAsync().ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await ForceStopAsync().ConfigureAwait(false);
        _gate.Dispose();
    }

    private CopilotClient GetReadyClient()
        => IsReady && _client is not null
            ? _client
            : throw new InvalidOperationException("Copilot SDK runtime is not ready.");
}
