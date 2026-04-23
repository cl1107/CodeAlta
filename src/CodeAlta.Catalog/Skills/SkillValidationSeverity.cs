namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Identifies the severity of a skill validation diagnostic.
/// </summary>
public enum SkillValidationSeverity
{
    /// <summary>
    /// Non-fatal warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Fatal validation error.
    /// </summary>
    Error,
}
