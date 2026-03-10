namespace CodeAlta.Orchestration.Runtime;

/// <summary>
/// Represents the composed instruction bundle used when starting an agent session.
/// </summary>
public sealed record AgentInstructionBundle
{
    /// <summary>
    /// Gets the system message.
    /// </summary>
    public required string SystemMessage { get; init; }

    /// <summary>
    /// Gets the developer instructions.
    /// </summary>
    public required string DeveloperInstructions { get; init; }
}
