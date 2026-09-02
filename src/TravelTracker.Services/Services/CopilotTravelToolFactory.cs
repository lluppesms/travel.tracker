using System.ComponentModel;
using GitHub.Copilot;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Services;

/// <summary>
/// Creates narrowly scoped Copilot tools that delegate to the durable assistant action boundary.
/// </summary>
public sealed class CopilotTravelToolFactory(IServiceScopeFactory scopeFactory) : ICopilotTravelToolFactory
{
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
                "search_user_locations",
                "Searches only the authenticated user's compact location records."),
            Define(
                async ([Description("Exact or partial configured location type name")] string locationTypeName, CancellationToken cancellationToken) =>
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITravelAssistantActionService>();
                    return await service.ResolveLocationTypeAsync(user, locationTypeName, cancellationToken)
                        .ConfigureAwait(false);
                },
                "resolve_location_type",
                "Resolves a configured location type without guessing ambiguous matches."),
            Define(
                async (
                    [Description("Place name")] string name,
                    [Description("Optional city")] string? city,
                    [Description("Optional state or region")] string? state,
                    CancellationToken cancellationToken) =>
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<ITravelAssistantActionService>();
                    return await service.LookupPlaceAsync(
                        user,
                        new PlaceLookupRequest { Name = name, City = city, State = state },
                        cancellationToken).ConfigureAwait(false);
                },
                "lookup_place",
                "Returns ranked place candidates with opaque candidate identifiers."),
            Define(
                async (
                    [Description("Opaque candidate identifier from lookup_place")] string candidateId,
                    [Description("Location display name")] string locationName,
                    [Description("Configured location type name")] string locationTypeName,
                    [Description("User's original ISO or relative date expression")] string dateExpression,
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
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                },
                "prepare_add_visited_location",
                "Prepares a durable pending location action; it never confirms the write.")
        ];
    }

    private static AIFunction Define<TDelegate>(
        TDelegate handler,
        string name,
        string description)
        where TDelegate : Delegate
        => CopilotTool.DefineTool(
            handler,
            toolOptions: new CopilotToolOptions { SkipPermission = true },
            factoryOptions: new AIFunctionFactoryOptions
            {
                Name = name,
                Description = description
            });
}
