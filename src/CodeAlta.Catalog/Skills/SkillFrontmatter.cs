namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Represents parsed canonical Agent Skills frontmatter metadata.
/// </summary>
public sealed record SkillFrontmatter
{
    /// <summary>
    /// Gets the skill name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the skill description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional license value.
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Gets the optional compatibility note.
    /// </summary>
    public string? Compatibility { get; init; }

    /// <summary>
    /// Gets portable metadata entries.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the optional allowed-tools string.
    /// </summary>
    public string? AllowedTools { get; init; }

    /// <summary>
    /// Gets unknown top-level frontmatter field names.
    /// </summary>
    public IReadOnlyList<string> UnknownTopLevelFields { get; init; } = [];
}
