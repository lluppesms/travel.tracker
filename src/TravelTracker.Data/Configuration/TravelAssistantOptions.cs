namespace TravelTracker.Data.Configuration;

/// <summary>
/// Chat provider implementations that can serve the travel assistant.
/// </summary>
public enum ChatProvider
{
    AgentFramework = 0,
    CopilotSDK = 1
}

/// <summary>
/// Write policy for state-changing assistant actions. Only <see cref="Confirm"/> is allowed in the first release.
/// </summary>
public enum AssistantWriteMode
{
    Confirm = 0,
    AutoExecute = 1
}

/// <summary>
/// Configuration for the travel assistant chat providers, runtime, and safety limits.
/// </summary>
public class TravelAssistantOptions
{
    public const string SectionName = "TravelAssistant";

    public ChatProvider Provider { get; set; } = ChatProvider.AgentFramework;

    public AssistantWriteMode WriteMode { get; set; } = AssistantWriteMode.Confirm;

    /// <summary>Model deployment name used by the selected provider.</summary>
    public string ModelDeploymentName { get; set; } = string.Empty;

    /// <summary>Azure AI Foundry base URL used for the data-plane endpoint.</summary>
    public string FoundryEndpoint { get; set; } = string.Empty;

    /// <summary>Entra token scope requested for the Foundry data plane.</summary>
    public string TokenScope { get; set; } = string.Empty;

    /// <summary>Instance-local writable directory used as COPILOT_HOME (SDK BaseDirectory).</summary>
    public string CopilotHome { get; set; } = string.Empty;

    /// <summary>Time zone used for relative-date resolution.</summary>
    public string TimeZoneId { get; set; } = "America/Chicago";

    public int MaxPromptCharacters { get; set; } = 4000;

    public int MaxTurnsPerSession { get; set; } = 20;

    public int MaxToolResultCharacters { get; set; } = 8000;

    public int MaxSessionsPerUser { get; set; } = 3;

    public int MaxSessionsPerInstance { get; set; } = 100;

    public int TurnTimeoutSeconds { get; set; } = 60;

    public int SessionIdleTimeoutMinutes { get; set; } = 15;

    /// <summary>Lifetime of an unconfirmed pending action before it expires.</summary>
    public int PendingActionExpiryHours { get; set; } = 24;

    /// <summary>Lifetime of an opaque place candidate returned by the lookup boundary.</summary>
    public int CandidateExpiryMinutes { get; set; } = 15;

    /// <summary>Number of days sanitized terminal action audit records are retained.</summary>
    public int ActionAuditRetentionDays { get; set; } = 90;

    /// <summary>Maximum number of compact location search results exposed to the assistant.</summary>
    public int MaxLocationSearchResults { get; set; } = 25;

    /// <summary>Minimum interval between public geocoder requests.</summary>
    public int GeocodingMinimumIntervalMilliseconds { get; set; } = 1000;

    /// <summary>Optional durable Data Protection key-ring directory.</summary>
    public string DataProtectionKeysPath { get; set; } = string.Empty;
}
