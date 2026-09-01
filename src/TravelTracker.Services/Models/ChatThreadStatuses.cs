namespace TravelTracker.Services.Models;

/// <summary>
/// Stable, provider-neutral chat thread status values returned alongside a chat turn.
/// </summary>
public static class ChatThreadStatuses
{
    /// <summary>The requested thread was reused and remains active.</summary>
    public const string Active = "active";

    /// <summary>The requested thread was stale or unknown, so a new thread was created and returned.</summary>
    public const string ThreadReplaced = "thread_replaced";
}
