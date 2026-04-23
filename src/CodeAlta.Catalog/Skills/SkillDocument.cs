namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Represents a loaded skill document.
/// </summary>
public sealed record SkillDocument
{
    /// <summary>
    /// Gets the discovered skill descriptor.
    /// </summary>
    public required SkillDescriptor Descriptor { get; init; }

    /// <summary>
    /// Gets the parsed frontmatter.
    /// </summary>
    public required SkillFrontmatter Frontmatter { get; init; }

    /// <summary>
    /// Gets the raw YAML frontmatter text without delimiters.
    /// </summary>
    public required string RawFrontmatter { get; init; }

    /// <summary>
    /// Gets the raw <c>SKILL.md</c> content.
    /// </summary>
    public required string RawContent { get; init; }

    /// <summary>
    /// Gets the markdown body after the closing frontmatter delimiter.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Gets the skill name.
    /// </summary>
    public string Name => Descriptor.Name;

    /// <summary>
    /// Gets the skill directory path.
    /// </summary>
    public string Path => Descriptor.SkillRootPath;

    /// <summary>
    /// Gets the raw <c>SKILL.md</c> content.
    /// </summary>
    public string Content => RawContent;
}


