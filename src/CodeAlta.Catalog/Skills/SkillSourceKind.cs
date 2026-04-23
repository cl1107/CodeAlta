namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Identifies the provenance of a discovered skill.
/// </summary>
public enum SkillSourceKind
{
    /// <summary>
    /// Project-local CodeAlta skill root (<c>.alta/skills</c>).
    /// </summary>
    ProjectAlta,

    /// <summary>
    /// Project-local common Agent Skills root (<c>.agents/skills</c>).
    /// </summary>
    ProjectCommon,

    /// <summary>
    /// User-level CodeAlta skill root (<c>~/.alta/skills</c>).
    /// </summary>
    UserAlta,

    /// <summary>
    /// User-level common Agent Skills root (<c>~/.agents/skills</c>).
    /// </summary>
    UserCommon,

    /// <summary>
    /// Plugin-contributed skill root.
    /// </summary>
    Plugin,

    /// <summary>
    /// Built-in skill root.
    /// </summary>
    Builtin,

    /// <summary>
    /// Temporary or explicit test/tooling root.
    /// </summary>
    Temporary,
}
