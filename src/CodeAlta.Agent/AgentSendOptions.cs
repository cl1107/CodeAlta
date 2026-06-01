namespace CodeAlta.Agent;

/// <summary>
/// Options for starting a new run in a session.
/// </summary>
public sealed class AgentSendOptions
{
    /// <summary>
    /// Gets or initializes the input to send.
    /// </summary>
    public required AgentInput Input { get; init; }

    /// <summary>
    /// Gets the optional ask identifier associated with this user prompt.
    /// </summary>
    public string? AskId { get; init; }
}
