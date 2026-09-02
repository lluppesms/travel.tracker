using System;
using System.Linq;
using System.Reflection;

using TravelTracker.Services.Models;

namespace TravelTracker.Tests.Models;

public class ChatTurnResultTests
{
    [Fact]
    public void Success_SetsCoreFields()
    {
        var when = DateTimeOffset.UtcNow;
        var statuses = new[] { ToolStatus.Succeeded("search_locations", "2 matches", 12) };

        var result = ChatTurnResult.Success("Hello", "thread-1", when, statuses);

        Assert.Equal("Hello", result.Message);
        Assert.Equal("thread-1", result.ThreadId);
        Assert.Equal(when, result.LatestMessageDate);
        Assert.Same(statuses, result.ToolStatuses);
        Assert.Null(result.PendingAction);
        Assert.Null(result.ErrorCode);
        Assert.True(result.IsSuccess);
        Assert.False(result.IsError);
        Assert.Equal(ChatThreadStatuses.Active, result.ThreadStatus);
        Assert.Equal(200, result.HttpStatusCode);
    }

    [Fact]
    public void Success_DefaultsCollectionsToEmptyNotNull()
    {
        var result = ChatTurnResult.Success("Hello", "thread-1");

        Assert.NotNull(result.ToolStatuses);
        Assert.Empty(result.ToolStatuses);
    }

    [Fact]
    public void DefaultConstructedResult_HasEmptyToolStatuses()
    {
        var result = new ChatTurnResult { Message = "hi", ThreadId = "t" };

        Assert.NotNull(result.ToolStatuses);
        Assert.Empty(result.ToolStatuses);
        Assert.Equal(ChatThreadStatuses.Active, result.ThreadStatus);
    }

    [Fact]
    public void Success_CarriesPendingActionAndThreadStatus()
    {
        var action = CreateActionSummary();

        var result = ChatTurnResult.ThreadReplaced("New thread started", "thread-2", pendingAction: action);

        Assert.Equal("thread-2", result.ThreadId);
        Assert.Equal(ChatThreadStatuses.ThreadReplaced, result.ThreadStatus);
        Assert.Equal("action-1", result.PendingAction?.ActionId);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Failure_CarriesStableErrorCode()
    {
        var result = ChatTurnResult.Failure(ChatErrorCodes.ProviderUnavailable, "The assistant is unavailable.", "thread-1");

        Assert.True(result.IsError);
        Assert.False(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorCode));
        Assert.Equal(ChatErrorCodes.ProviderUnavailable, result.ErrorCode);
        Assert.Equal("The assistant is unavailable.", result.Message);
        Assert.NotNull(result.ToolStatuses);
        Assert.Empty(result.ToolStatuses);
        Assert.Equal(503, result.HttpStatusCode);
    }

    [Fact]
    public void Failure_WithBlankErrorCode_FallsBackToInternalError()
    {
        var result = ChatTurnResult.Failure(" ", "Something went wrong.", "thread-1");

        Assert.Equal(ChatErrorCodes.InternalError, result.ErrorCode);
        Assert.Equal(500, result.HttpStatusCode);
    }

    [Theory]
    [InlineData(ChatErrorCodes.Unauthorized, 401)]
    [InlineData(ChatErrorCodes.Forbidden, 403)]
    [InlineData(ChatErrorCodes.ThreadNotFound, 404)]
    [InlineData(ChatErrorCodes.ActionNotFound, 404)]
    [InlineData(ChatErrorCodes.ThreadReplaced, 409)]
    [InlineData(ChatErrorCodes.ActionConflict, 409)]
    [InlineData(ChatErrorCodes.ActionExpired, 410)]
    [InlineData(ChatErrorCodes.RateLimited, 429)]
    [InlineData(ChatErrorCodes.ProviderUnavailable, 503)]
    [InlineData(ChatErrorCodes.InvalidRequest, 400)]
    [InlineData(ChatErrorCodes.InternalError, 500)]
    public void ErrorCodes_MapToDocumentedHttpStatusCodes(string errorCode, int expected)
    {
        Assert.Equal(expected, ChatErrorCodes.ToHttpStatusCode(errorCode));
    }

    [Fact]
    public void ToHttpStatusCode_HandlesNullAndUnknownCodes()
    {
        Assert.Equal(200, ChatErrorCodes.ToHttpStatusCode(null));
        Assert.Equal(200, ChatErrorCodes.ToHttpStatusCode(string.Empty));
        Assert.Equal(500, ChatErrorCodes.ToHttpStatusCode("not_a_known_code"));
    }

    [Fact]
    public void ToolStatus_FactoriesSetState()
    {
        Assert.Equal(ToolStatusState.Started, ToolStatus.Started("t").State);
        Assert.Equal(ToolStatusState.Succeeded, ToolStatus.Succeeded("t", "ok", 5).State);

        var failed = ToolStatus.Failed("t", "lookup failed", 7);
        Assert.Equal(ToolStatusState.Failed, failed.State);
        Assert.Equal("t", failed.ToolName);
        Assert.Equal("lookup failed", failed.Detail);
        Assert.Equal(7, failed.DurationMs);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("secret")]
    [InlineData("key")]
    [InlineData("token")]
    [InlineData("payload")]
    public void ChatActionSummary_DoesNotExposeSensitiveProperties(string forbiddenFragment)
    {
        var offending = typeof(ChatActionSummary)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .Where(name => name.Contains(forbiddenFragment, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.Empty(offending);
    }

    [Fact]
    public void ChatActionSummary_ExposesOnlyDisplaySafeFields()
    {
        var summary = CreateActionSummary();

        Assert.Equal("action-1", summary.ActionId);
        Assert.Equal("create_location", summary.ActionType);
        Assert.Equal("Add Springfield", summary.Title);
        Assert.Equal(new DateOnly(2026, 8, 31), summary.Date);
        Assert.True(summary.ExpiresAt > DateTimeOffset.UtcNow);
    }

    private static ChatActionSummary CreateActionSummary() => new()
    {
        ActionId = "action-1",
        ActionType = "create_location",
        Title = "Add Springfield",
        Summary = "Add a new visited location.",
        DisplayName = "Springfield",
        LocationText = "Springfield, IL",
        Date = new DateOnly(2026, 8, 31),
        TypeName = "City",
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
    };
}
