namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Describes the filesystem context used to resolve built-in skill roots.
/// </summary>
public sealed record SkillDiscoveryContext
{
    /// <summary>
    /// Gets the active project roots.
    /// </summary>
    public IReadOnlyList<string> ProjectRoots { get; init; } = [];

    /// <summary>
    /// Gets the user-level CodeAlta root (for example <c>~/.alta</c>).
    /// </summary>
    public string? UserCodeAltaRoot { get; init; }

    /// <summary>
    /// Gets the user profile root (for example <c>~</c>).
    /// </summary>
    public string? UserProfileRoot { get; init; }

    /// <summary>
    /// Gets a value indicating whether built-in providers should be used.
    /// </summary>
    public bool UseBuiltInRoots { get; init; } = true;

    /// <summary>
    /// Gets additional explicit root registrations.
    /// </summary>
    public IReadOnlyList<SkillRootRegistration> AdditionalRoots { get; init; } = [];
}
