namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Represents one registered filesystem root used during skill discovery.
/// </summary>
public sealed record SkillRootRegistration
{
    /// <summary>
    /// Gets the absolute or root-relative filesystem path.
    /// </summary>
    public required string RootPath { get; init; }

    /// <summary>
    /// Gets the source kind.
    /// </summary>
    public required SkillSourceKind SourceKind { get; init; }

    /// <summary>
    /// Gets the opaque source identifier.
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    /// Gets the scope.
    /// </summary>
    public required SkillScopeKind Scope { get; init; }

    /// <summary>
    /// Gets the precedence where lower values win.
    /// </summary>
    public required int Precedence { get; init; }

    /// <summary>
    /// Gets a value indicating whether skills from this root are trusted for model advertisement.
    /// </summary>
    public bool IsTrusted { get; init; } = true;
}
