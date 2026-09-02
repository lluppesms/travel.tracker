namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Exception thrown when session quota limits are exceeded.
/// </summary>
public class SessionQuotaExceededException : InvalidOperationException
{
    public SessionQuotaExceededException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when a session operation is attempted by a different user.
/// </summary>
public class CrossUserSessionException : InvalidOperationException
{
    public CrossUserSessionException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when a session has been idle too long and is no longer valid.
/// </summary>
public class StaleSessionException : InvalidOperationException
{
    public StaleSessionException(string message) : base(message) { }
}
