using GitHub.Copilot;

namespace TravelTracker.Services.Services;

internal sealed class CopilotSessionHandle(CopilotSession session) : ICopilotSessionHandle
{
    private readonly CopilotSession _session = session ?? throw new ArgumentNullException(nameof(session));

    public string SessionId => _session.SessionId;

    public async Task<string?> SendAndWaitAsync(
        string prompt,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var response = await _session.SendAndWaitAsync(
            new MessageOptions { Prompt = prompt },
            timeout,
            cancellationToken).ConfigureAwait(false);

        return response?.Data?.Content;
    }

    public ValueTask DisposeAsync() => _session.DisposeAsync();
}
