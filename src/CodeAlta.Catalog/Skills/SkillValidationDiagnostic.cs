namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Represents one skill validation diagnostic.
/// </summary>
public sealed record SkillValidationDiagnostic
{
    /// <summary>
    /// Gets the diagnostic severity.
    /// </summary>
    public required SkillValidationSeverity Severity { get; init; }

    /// <summary>
    /// Gets the stable diagnostic code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the human-readable message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the related frontmatter field when applicable.
    /// </summary>
    public string? FieldName { get; init; }

    /// <summary>
    /// Gets the related path when applicable.
    /// </summary>
    public string? Path { get; init; }
}
