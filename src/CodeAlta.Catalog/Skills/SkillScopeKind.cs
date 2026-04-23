namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Identifies the visibility scope of a skill root.
/// </summary>
public enum SkillScopeKind
{
    /// <summary>
    /// Project-local scope.
    /// </summary>
    Project,

    /// <summary>
    /// User/global scope.
    /// </summary>
    User,

    /// <summary>
    /// Plugin-contributed scope.
    /// </summary>
    Plugin,

    /// <summary>
    /// Built-in scope.
    /// </summary>
    Builtin,

    /// <summary>
    /// Temporary scope.
    /// </summary>
    Temporary,
}
