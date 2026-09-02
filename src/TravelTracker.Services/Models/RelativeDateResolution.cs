namespace TravelTracker.Services.Models;

/// <summary>
/// The server-authoritative outcome of resolving a date expression supplied by an assistant.
/// </summary>
public sealed record RelativeDateResolution
{
    /// <summary>Whether the expression resolved to a date, needs clarification, or disagrees with a proposed date.</summary>
    public required RelativeDateResolutionStatus Status { get; init; }

    /// <summary>The date resolved by the server when <see cref="Status"/> is <see cref="RelativeDateResolutionStatus.Resolved"/>.</summary>
    public DateOnly? Date { get; init; }

    /// <summary>User-safe explanation for a clarification or disagreement.</summary>
    public string? Message { get; init; }

    /// <summary>Creates a successfully resolved date.</summary>
    public static RelativeDateResolution Resolved(DateOnly date) =>
        new() { Status = RelativeDateResolutionStatus.Resolved, Date = date };

    /// <summary>Creates an outcome that requires the user to clarify the requested date.</summary>
    public static RelativeDateResolution ClarificationRequired(string message) =>
        new() { Status = RelativeDateResolutionStatus.ClarificationRequired, Message = message };

    /// <summary>Creates an outcome for a model-supplied date that differs from the server result.</summary>
    public static RelativeDateResolution DateDisagrees(string message) =>
        new() { Status = RelativeDateResolutionStatus.DateDisagrees, Message = message };
}

/// <summary>
/// Status values for server-side relative-date resolution.
/// </summary>
public enum RelativeDateResolutionStatus
{
    Resolved,
    ClarificationRequired,
    DateDisagrees
}
