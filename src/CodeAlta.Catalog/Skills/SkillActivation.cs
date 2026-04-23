namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Represents the canonical activated-skill payload and related metadata.
/// </summary>
public sealed record SkillActivation
{
    /// <summary>
    /// Gets the skill descriptor.
    /// </summary>
    public required SkillDescriptor Descriptor { get; init; }

    /// <summary>
    /// Gets the loaded skill document.
    /// </summary>
    public required SkillDocument Document { get; init; }

    /// <summary>
    /// Gets the resolved base directory URI.
    /// </summary>
    public required string BaseDirectoryUri { get; init; }

    /// <summary>
    /// Gets the bounded file list under the skill root.
    /// </summary>
    public IReadOnlyList<string> Files { get; init; } = [];

    /// <summary>
    /// Gets the canonical payload text injected into the conversation.
    /// </summary>
    public required string Payload { get; init; }
}
