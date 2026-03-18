using CodeAlta.Agent;

namespace CodeAlta.Catalog;

/// <summary>
/// Describes the effective model and reasoning preference for a backend.
/// </summary>
/// <param name="Model">The preferred backend model identifier.</param>
/// <param name="ReasoningEffort">The preferred reasoning effort.</param>
public sealed record CodeAltaBackendPreference(
    string? Model,
    AgentReasoningEffort? ReasoningEffort);
