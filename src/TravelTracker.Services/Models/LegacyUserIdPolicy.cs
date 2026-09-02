using System;

namespace TravelTracker.Services.Models;

/// <summary>
/// Outcome of evaluating a legacy <c>userId</c> query value against the resolved identity.
/// </summary>
public enum LegacyUserIdEvaluation
{
    /// <summary>No legacy value was supplied.</summary>
    NotSupplied = 0,

    /// <summary>The legacy value matches the resolved user and can be ignored.</summary>
    MatchesResolvedUser = 1,

    /// <summary>The legacy value does not match the resolved user and must be rejected.</summary>
    Mismatched = 2
}

/// <summary>
/// Enforces the API contract rule that a legacy <c>userId</c> query is ignored only when it equals the
/// resolved user, and rejected otherwise. A legacy value can never select a different user.
/// </summary>
public static class LegacyUserIdPolicy
{
    public static LegacyUserIdEvaluation Evaluate(TravelAssistantUserContext context, int? legacyUserId)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (legacyUserId is null)
        {
            return LegacyUserIdEvaluation.NotSupplied;
        }

        return legacyUserId.Value == context.UserId
            ? LegacyUserIdEvaluation.MatchesResolvedUser
            : LegacyUserIdEvaluation.Mismatched;
    }

    public static bool IsLegacyUserIdAcceptable(TravelAssistantUserContext context, int? legacyUserId)
        => Evaluate(context, legacyUserId) != LegacyUserIdEvaluation.Mismatched;
}
