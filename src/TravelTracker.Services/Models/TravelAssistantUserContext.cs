using System;

namespace TravelTracker.Services.Models;

/// <summary>
/// Immutable trusted identity for the travel assistant surface. Every value is derived from the
/// authenticated principal and the internal user record; nothing here is ever model or caller supplied.
/// </summary>
public sealed record TravelAssistantUserContext
{
    public TravelAssistantUserContext(int userId, string externalId, string displayName, string email)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "A travel assistant user context requires a resolved internal user id greater than zero.");
        }

        UserId = userId;
        ExternalId = externalId ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        Email = email ?? string.Empty;
    }

    /// <summary>Internal Travel Tracker user id.</summary>
    public int UserId { get; }

    /// <summary>Entra ID object identifier (or equivalent external identifier) of the authenticated principal.</summary>
    public string ExternalId { get; }

    public string DisplayName { get; }

    public string Email { get; }
}
