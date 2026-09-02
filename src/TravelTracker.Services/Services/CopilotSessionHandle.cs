using GitHub.Copilot;

using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

internal sealed class CopilotSessionHandle(CopilotSession session) : ICopilotSessionHandle
{
    private readonly CopilotSession _session = session ?? throw new ArgumentNullException(nameof(session));

    public string SessionId => _session.SessionId;

    public async Task<CopilotTurnResponse> SendAndWaitAsync(
        string prompt,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var modelCallCount = 0;
        long? inputTokens = null;
        long? outputTokens = null;
        long? cacheReadTokens = null;
        long? cacheWriteTokens = null;
        double? totalCost = null;

        using var subscription = _session.On<AssistantUsageEvent>(usageEvent =>
        {
            var data = usageEvent.Data;
            if (data is null)
            {
                return;
            }

            modelCallCount++;
            inputTokens = (inputTokens ?? 0) + (data.InputTokens ?? 0);
            outputTokens = (outputTokens ?? 0) + (data.OutputTokens ?? 0);
            cacheReadTokens = (cacheReadTokens ?? 0) + (data.CacheReadTokens ?? 0);
            cacheWriteTokens = (cacheWriteTokens ?? 0) + (data.CacheWriteTokens ?? 0);
#pragma warning disable GHCP001 // AssistantUsageData.Cost is evaluation-only; used deliberately to surface AI Credits cost to users.
            if (data.Cost is { } cost)
            {
                totalCost = (totalCost ?? 0) + cost;
            }
#pragma warning restore GHCP001
        });

        var response = await _session.SendAndWaitAsync(
            new MessageOptions { Prompt = prompt },
            timeout,
            cancellationToken).ConfigureAwait(false);

        return new CopilotTurnResponse
        {
            Content = response?.Data?.Content,
            ModelCallCount = modelCallCount,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            CacheReadTokens = cacheReadTokens,
            CacheWriteTokens = cacheWriteTokens,
            TotalCost = totalCost
        };
    }

    public ValueTask DisposeAsync() => _session.DisposeAsync();
}
