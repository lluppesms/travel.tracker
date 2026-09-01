using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using TravelTracker.Data.Configuration;

namespace TravelTracker.Services.Configuration;

/// <summary>
/// Validates <see cref="TravelAssistantOptions"/> at startup. Failure messages never include
/// configuration values, so secrets, keys, tokens, and connection strings cannot leak into logs.
/// </summary>
public class TravelAssistantOptionsValidator : IValidateOptions<TravelAssistantOptions>
{
    public const string AzureAdTenantIdKey = "AzureAd:TenantId";
    public const string AzureAdClientIdKey = "AzureAd:ClientId";
    public const string SqlConnectionStringKey = "SqlServer:ConnectionString";

    private readonly IConfiguration? _configuration;

    public TravelAssistantOptionsValidator()
    {
    }

    public TravelAssistantOptionsValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ValidateOptionsResult Validate(string? name, TravelAssistantOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!Enum.IsDefined(options.Provider))
        {
            failures.Add($"{TravelAssistantOptions.SectionName}:Provider must be '{nameof(ChatProvider.AgentFramework)}' or '{nameof(ChatProvider.CopilotSDK)}'.");
        }

        if (!Enum.IsDefined(options.WriteMode))
        {
            failures.Add($"{TravelAssistantOptions.SectionName}:WriteMode must be '{nameof(AssistantWriteMode.Confirm)}'.");
        }
        else if (options.WriteMode == AssistantWriteMode.AutoExecute)
        {
            failures.Add($"{TravelAssistantOptions.SectionName}:WriteMode '{nameof(AssistantWriteMode.AutoExecute)}' is not supported in this release; use '{nameof(AssistantWriteMode.Confirm)}'.");
        }

        if (string.IsNullOrWhiteSpace(options.TimeZoneId) || !TryResolveTimeZone(options.TimeZoneId))
        {
            failures.Add($"{TravelAssistantOptions.SectionName}:TimeZoneId is not a time zone known to this system.");
        }

        if (options.Provider == ChatProvider.CopilotSDK)
        {
            if (string.IsNullOrWhiteSpace(options.ModelDeploymentName))
            {
                failures.Add($"{TravelAssistantOptions.SectionName}:ModelDeploymentName is required when Provider is '{nameof(ChatProvider.CopilotSDK)}'.");
            }

            if (string.IsNullOrWhiteSpace(options.FoundryEndpoint))
            {
                failures.Add($"{TravelAssistantOptions.SectionName}:FoundryEndpoint is required when Provider is '{nameof(ChatProvider.CopilotSDK)}'.");
            }
            else if (!Uri.TryCreate(options.FoundryEndpoint, UriKind.Absolute, out _))
            {
                failures.Add($"{TravelAssistantOptions.SectionName}:FoundryEndpoint must be an absolute URL.");
            }

            if (string.IsNullOrWhiteSpace(options.TokenScope))
            {
                failures.Add($"{TravelAssistantOptions.SectionName}:TokenScope is required when Provider is '{nameof(ChatProvider.CopilotSDK)}'.");
            }

            if (string.IsNullOrWhiteSpace(options.CopilotHome))
            {
                failures.Add($"{TravelAssistantOptions.SectionName}:CopilotHome is required when Provider is '{nameof(ChatProvider.CopilotSDK)}'.");
            }
        }

        AddPositiveLimitFailure(failures, options.MaxPromptCharacters, nameof(TravelAssistantOptions.MaxPromptCharacters));
        AddPositiveLimitFailure(failures, options.MaxTurnsPerSession, nameof(TravelAssistantOptions.MaxTurnsPerSession));
        AddPositiveLimitFailure(failures, options.MaxToolResultCharacters, nameof(TravelAssistantOptions.MaxToolResultCharacters));
        AddPositiveLimitFailure(failures, options.MaxSessionsPerUser, nameof(TravelAssistantOptions.MaxSessionsPerUser));
        AddPositiveLimitFailure(failures, options.MaxSessionsPerInstance, nameof(TravelAssistantOptions.MaxSessionsPerInstance));
        AddPositiveLimitFailure(failures, options.TurnTimeoutSeconds, nameof(TravelAssistantOptions.TurnTimeoutSeconds));
        AddPositiveLimitFailure(failures, options.SessionIdleTimeoutMinutes, nameof(TravelAssistantOptions.SessionIdleTimeoutMinutes));
        AddPositiveLimitFailure(failures, options.PendingActionExpiryHours, nameof(TravelAssistantOptions.PendingActionExpiryHours));

        if (options.MaxSessionsPerUser > options.MaxSessionsPerInstance)
        {
            failures.Add($"{TravelAssistantOptions.SectionName}:{nameof(TravelAssistantOptions.MaxSessionsPerUser)} cannot exceed {nameof(TravelAssistantOptions.MaxSessionsPerInstance)}.");
        }

        if (_configuration is not null)
        {
            failures.AddRange(ValidateAuthentication(_configuration));
            failures.AddRange(ValidateActionStorage(_configuration));
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    /// <summary>
    /// Returns failure messages when Entra authentication configuration is missing.
    /// </summary>
    public static IReadOnlyList<string> ValidateAuthentication(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration[AzureAdTenantIdKey]))
        {
            failures.Add($"{AzureAdTenantIdKey} is required; the travel assistant requires authenticated users.");
        }

        if (string.IsNullOrWhiteSpace(configuration[AzureAdClientIdKey]))
        {
            failures.Add($"{AzureAdClientIdKey} is required; the travel assistant requires authenticated users.");
        }

        return failures;
    }

    /// <summary>
    /// Returns failure messages when SQL action storage configuration is missing. The connection
    /// string value is never included in the message.
    /// </summary>
    public static IReadOnlyList<string> ValidateActionStorage(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = AssistantConnectionStrings.Resolve(configuration);

        return connectionString is null
            ? new List<string> { $"{SqlConnectionStringKey} or {AssistantConnectionStrings.DefaultConnectionKey} is required; the travel assistant stores pending actions in SQL." }
            : new List<string>();
    }

    private static bool TryResolveTimeZone(string timeZoneId)
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
        {
            return true;
        }

        return TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsId!, out _);
    }

    private static void AddPositiveLimitFailure(List<string> failures, int value, string propertyName)
    {
        if (value <= 0)
        {
            failures.Add($"{TravelAssistantOptions.SectionName}:{propertyName} must be greater than zero.");
        }
    }
}
