using GitHub.Copilot;
using Microsoft.Extensions.AI;
using TravelTracker.Services.Models;

namespace TravelTracker.Services.Interfaces;

/// <summary>
/// Creates the explicit travel-tool allowlist for a user-owned Copilot session.
/// </summary>
public interface ICopilotTravelToolFactory
{
    /// <summary>Creates tools bound to a trusted user and thread.</summary>
    ICollection<AIFunctionDeclaration> CreateTools(TravelAssistantUserContext user, string threadId);

    /// <summary>
    /// Applies the deny-by-default permission handler and content-safe lifecycle hooks.
    /// </summary>
    /// <param name="sessionConfig">Session configuration to secure.</param>
    void ConfigureSession(SessionConfig sessionConfig);
}
