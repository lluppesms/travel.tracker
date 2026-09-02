using System;
using Microsoft.Extensions.Configuration;

namespace TravelTracker.Services.Configuration;

/// <summary>
/// Single definition of the SQL connection string used by both the travel assistant prerequisite
/// checks and host service registration, so they can never disagree about what is configured.
/// </summary>
public static class AssistantConnectionStrings
{
    public const string SqlConnectionStringKey = TravelAssistantOptionsValidator.SqlConnectionStringKey;
    public const string DefaultConnectionName = "DefaultConnection";
    public const string DefaultConnectionKey = "ConnectionStrings:" + DefaultConnectionName;

    /// <summary>
    /// Returns the effective SQL connection string, or <c>null</c> when none is configured.
    /// <see cref="string.IsNullOrWhiteSpace(string?)"/> semantics apply: <c>SqlServer:ConnectionString</c>
    /// wins when non-blank, otherwise <c>ConnectionStrings:DefaultConnection</c> is used.
    /// </summary>
    public static string? Resolve(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var primary = configuration[SqlConnectionStringKey];
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        var fallback = configuration.GetConnectionString(DefaultConnectionName);
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }
}
