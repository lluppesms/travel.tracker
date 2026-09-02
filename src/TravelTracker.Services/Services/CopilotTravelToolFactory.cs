using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using GitHub.Copilot;
using GitHub.Copilot.Rpc;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

/// <summary>
/// Creates narrowly scoped Copilot tools that delegate to the durable assistant action boundary.
/// </summary>
public sealed class CopilotTravelToolFactory(
    IServiceScopeFactory scopeFactory,
    ILogger<CopilotTravelToolFactory> logger,
    TimeProvider timeProvider) : ICopilotTravelToolFactory
{
    private readonly ConcurrentDictionary<string, ToolInvocationContext> _toolInvocations =
        new(StringComparer.Ordinal);

    public ICollection<AIFunctionDeclaration> CreateTools(TravelAssistantUserContext user, string threadId)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);

        return
        [
            Define(
                async ([Description("Location name or text to search for")] string query, CancellationToken cancellationToken) =>
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITravelAssistantActionService>();
                    return await service.SearchUserLocationsAsync(user, query, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                },
                CopilotTravelToolNames.SearchUserLocations,
                "Searches only the authenticated user's compact location records.",
                skipPermission: true),
            Define(
                async (CancellationToken cancellationToken) =>
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITravelAssistantActionService>();
                    return await service.GetLocationTypesAsync(user, cancellationToken)
                        .ConfigureAwait(false);
                },
                CopilotTravelToolNames.GetLocationTypes,
                "Lists valid configured location type names and descriptions.",
                skipPermission: true),
            Define(
                async (
                    [Description("Place name")] string name,
                    [Description("Optional street address")] string? address,
                    [Description("Optional city")] string? city,
                    [Description("Optional state or region")] string? state,
                    [Description("Optional postal or ZIP code")] string? postalCode,
                    CancellationToken cancellationToken) =>
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITravelAssistantActionService>();
                    return await service.LookupPlaceAsync(
                        user,
                        new PlaceLookupRequest
                        {
                            Name = name,
                            Address = address,
                            City = city,
                            State = state,
                            PostalCode = postalCode
                        },
                        cancellationToken).ConfigureAwait(false);
                },
                CopilotTravelToolNames.LookupPlace,
                "Returns ranked place candidates with opaque candidate identifiers.",
                skipPermission: true),
            Define(
                async (
                    [Description("Opaque candidate identifier from lookup_place")] string candidateId,
                    [Description("Location display name")] string locationName,
                    [Description("Configured location type name")] string locationTypeName,
                    [Description("User's original ISO or relative date expression")] string dateExpression,
                    [Description("Optional model-proposed ISO date; application validation is authoritative")] string? proposedIsoDate,
                    [Description("Optional street address")] string? address,
                    [Description("Optional city")] string? city,
                    [Description("Optional state or region")] string? state,
                    [Description("Optional postal or ZIP code")] string? postalCode,
                    [Description("Optional latitude")] double? latitude,
                    [Description("Optional longitude")] double? longitude,
                    [Description("Optional user comments treated as untrusted data")] string? comments,
                    [Description("Optional rating from 0 through 5"), Range(0, 5)] int? rating,
                    CancellationToken cancellationToken) =>
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITravelAssistantActionService>();
                    return await service.PrepareAddLocationAsync(
                        user,
                        threadId,
                        candidateId,
                        locationName,
                        locationTypeName,
                        dateExpression,
                        proposedIsoDate,
                        address,
                        city,
                        state,
                        postalCode,
                        latitude,
                        longitude,
                        comments,
                        rating,
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                },
                CopilotTravelToolNames.PrepareAddVisitedLocation,
                "Prepares a durable pending location action; it never confirms the write.",
                skipPermission: false)
        ];
    }

    public void ConfigureSession(SessionConfig sessionConfig)
    {
        ArgumentNullException.ThrowIfNull(sessionConfig);
        sessionConfig.OnPermissionRequest = (request, _) =>
        {
            var toolName = request is PermissionRequestCustomTool customTool
                ? CopilotTravelToolNames.Normalize(customTool.ToolName)
                : null;

            if (toolName == CopilotTravelToolNames.PrepareAddVisitedLocation)
            {
#pragma warning disable GHCP001 // SDK 1.0.11 marks permission decisions experimental.
                return Task.FromResult(PermissionDecision.ApproveOnce());
#pragma warning restore GHCP001
            }

            logger.LogWarning(
                "Denied Copilot permission request of kind {PermissionKind} for tool {ToolName}.",
                request.Kind,
                toolName ?? "unknown");
#pragma warning disable GHCP001 // SDK 1.0.11 marks permission decisions experimental.
            return Task.FromResult(PermissionDecision.Reject("This operation is not available."));
#pragma warning restore GHCP001
        };

        sessionConfig.Hooks = new SessionHooks
        {
            OnPreToolUse = (input, _) =>
            {
                var toolName = CopilotTravelToolNames.Normalize(input.ToolName);
                var correlationId = Guid.NewGuid().ToString("N");
                _toolInvocations[CreateInvocationKey(input.SessionId, input.ToolName)] =
                    new ToolInvocationContext(correlationId, timeProvider.GetTimestamp());

                if (toolName is null)
                {
                    logger.LogWarning(
                        "Denied unknown Copilot tool with correlation {CorrelationId}.",
                        correlationId);
                    return Task.FromResult<PreToolUseHookOutput?>(new PreToolUseHookOutput
                    {
                        PermissionDecision = "deny",
                        PermissionDecisionReason = "This operation is not available."
                    });
                }

                logger.LogInformation(
                    "Copilot tool {ToolName} started with correlation {CorrelationId}.",
                    toolName,
                    correlationId);
                return Task.FromResult<PreToolUseHookOutput?>(new PreToolUseHookOutput());
            },
            OnPostToolUse = (input, _) =>
            {
                var context = TakeInvocationContext(input.SessionId, input.ToolName);
                var toolName = CopilotTravelToolNames.Normalize(input.ToolName) ?? "unknown";
                var (resultClass, actionId) = ClassifyResult(input.ToolResult);
                logger.LogInformation(
                    "Copilot tool {ToolName} completed with correlation {CorrelationId}, duration {DurationMs} ms, result {ResultClass}, action {ActionId}.",
                    toolName,
                    context.CorrelationId,
                    context.DurationMs,
                    resultClass,
                    actionId ?? "none");
                return Task.FromResult<PostToolUseHookOutput?>(new PostToolUseHookOutput());
            },
            OnPostToolUseFailure = (input, _) =>
            {
                var context = TakeInvocationContext(input.SessionId, input.ToolName);
                var toolName = CopilotTravelToolNames.Normalize(input.ToolName) ?? "unknown";
                logger.LogWarning(
                    "Copilot tool {ToolName} failed with correlation {CorrelationId}, duration {DurationMs} ms, result failure, action none.",
                    toolName,
                    context.CorrelationId,
                    context.DurationMs);
                return Task.FromResult<PostToolUseFailureHookOutput?>(new PostToolUseFailureHookOutput());
            }
        };
    }

    private static AIFunction Define<TDelegate>(
        TDelegate handler,
        string name,
        string description,
        bool skipPermission)
        where TDelegate : Delegate
        => CopilotTool.DefineTool(
            handler,
            toolOptions: new CopilotToolOptions { SkipPermission = skipPermission },
            factoryOptions: new AIFunctionFactoryOptions
            {
                Name = name,
                Description = description
            });

    private ToolInvocationLogContext TakeInvocationContext(string sessionId, string toolName)
    {
        if (_toolInvocations.TryRemove(CreateInvocationKey(sessionId, toolName), out var context))
        {
            return new ToolInvocationLogContext(
                context.CorrelationId,
                (long)timeProvider.GetElapsedTime(context.StartTimestamp).TotalMilliseconds);
        }

        return new ToolInvocationLogContext(Guid.NewGuid().ToString("N"), 0);
    }

    private static string CreateInvocationKey(string sessionId, string toolName) =>
        $"{sessionId}\u001f{toolName}";

    private static (string ResultClass, string? ActionId) ClassifyResult(JsonElement? toolResult)
    {
        if (toolResult is not { ValueKind: JsonValueKind.Object } result)
        {
            return (toolResult?.ValueKind.ToString().ToLowerInvariant() ?? "none", null);
        }

        var resultClass = "object";
        if (TryGetProperty(result, "success", out var success) &&
            success.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            resultClass = success.GetBoolean() ? "success" : "failure";
        }
        else if (TryGetProperty(result, "status", out var status) &&
                 status.ValueKind == JsonValueKind.String)
        {
            resultClass = status.GetString()?.ToLowerInvariant() switch
            {
                "found" => "found",
                "ambiguous" => "ambiguous",
                "notfound" or "not_found" => "not_found",
                _ => "object"
            };
        }

        string? actionId = null;
        if (TryGetProperty(result, "actionId", out var actionIdElement) &&
            actionIdElement.ValueKind == JsonValueKind.String &&
            Guid.TryParseExact(actionIdElement.GetString(), "N", out var parsedActionId))
        {
            actionId = parsedActionId.ToString("N");
        }

        return (resultClass, actionId);
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private sealed record ToolInvocationContext(string CorrelationId, long StartTimestamp);

    private sealed record ToolInvocationLogContext(string CorrelationId, long DurationMs);
}
