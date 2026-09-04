using TravelTracker.Services;
using TravelTracker.Services.Models;

namespace TravelTracker.Tests.Services;

public class DisabledChatbotServiceTests
{
    [Fact]
    public async Task GetChatResponseAsync_ReturnsProviderUnavailable()
    {
        var result = await new DisabledChatbotService().GetChatResponseAsync("hello", 7, "thread-1");

        Assert.False(result.IsSuccess);
        Assert.Equal(ChatErrorCodes.ProviderUnavailable, result.ErrorCode);
        Assert.Equal("thread-1", result.ThreadId);
    }

    [Fact]
    public async Task GetChatResponseAsync_UsesEmptyThreadIdWhenMissing()
    {
        var result = await new DisabledChatbotService().GetChatResponseAsync("hello", 7);

        Assert.Equal(string.Empty, result.ThreadId);
    }

    [Fact]
    public async Task GetChatResponseAsync_WhenCancelled_Throws()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new DisabledChatbotService().GetChatResponseAsync("hello", 7, cancellationToken: source.Token));
    }
}