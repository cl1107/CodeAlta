namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Represents a safely resolved skill resource file.
/// </summary>
public sealed record SkillResource
{
    /// <summary>
    /// Gets the skill descriptor.
    /// </summary>
    public required SkillDescriptor Descriptor { get; init; }

    /// <summary>
    /// Gets the requested relative path.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Gets the absolute resolved file path.
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Gets the file bytes.
    /// </summary>
    public required byte[] Content { get; init; }
}
