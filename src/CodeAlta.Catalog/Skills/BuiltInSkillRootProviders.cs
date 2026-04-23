namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Resolves project-local CodeAlta skill roots.
/// </summary>
public sealed class ProjectCodeAltaSkillRootProvider : ISkillRootProvider
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<SkillRootRegistration>> GetRootsAsync(
        SkillDiscoveryContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult<IReadOnlyList<SkillRootRegistration>>(
            context.ProjectRoots
                .Where(static path => !string.IsNullOrWhiteSpace(path))
                .Select(static projectRoot => new SkillRootRegistration
                {
                    RootPath = Path.Combine(projectRoot, ".alta", "skills"),
                    SourceKind = SkillSourceKind.ProjectAlta,
                    SourceId = $"project-alta:{Path.GetFullPath(projectRoot)}",
                    Scope = SkillScopeKind.Project,
                    Precedence = 0,
                })
                .ToArray());
    }
}

/// <summary>
/// Resolves project-local common Agent Skills roots.
/// </summary>
public sealed class ProjectCommonSkillRootProvider : ISkillRootProvider
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<SkillRootRegistration>> GetRootsAsync(
        SkillDiscoveryContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult<IReadOnlyList<SkillRootRegistration>>(
            context.ProjectRoots
                .Where(static path => !string.IsNullOrWhiteSpace(path))
                .Select(static projectRoot => new SkillRootRegistration
                {
                    RootPath = Path.Combine(projectRoot, ".agents", "skills"),
                    SourceKind = SkillSourceKind.ProjectCommon,
                    SourceId = $"project-common:{Path.GetFullPath(projectRoot)}",
                    Scope = SkillScopeKind.Project,
                    Precedence = 1,
                })
                .ToArray());
    }
}

/// <summary>
/// Resolves user-level CodeAlta skill roots.
/// </summary>
public sealed class UserCodeAltaSkillRootProvider : ISkillRootProvider
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<SkillRootRegistration>> GetRootsAsync(
        SkillDiscoveryContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.UserCodeAltaRoot))
        {
            return ValueTask.FromResult<IReadOnlyList<SkillRootRegistration>>([]);
        }

        return ValueTask.FromResult<IReadOnlyList<SkillRootRegistration>>(
        [
            new SkillRootRegistration
            {
                RootPath = Path.Combine(context.UserCodeAltaRoot, "skills"),
                SourceKind = SkillSourceKind.UserAlta,
                SourceId = $"user-alta:{Path.GetFullPath(context.UserCodeAltaRoot)}",
                Scope = SkillScopeKind.User,
                Precedence = 2,
            },
        ]);
    }
}

/// <summary>
/// Resolves user-level common Agent Skills roots.
/// </summary>
public sealed class UserCommonSkillRootProvider : ISkillRootProvider
{
    /// <inheritdoc />
    public ValueTask<IReadOnlyList<SkillRootRegistration>> GetRootsAsync(
        SkillDiscoveryContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(context.UserProfileRoot))
        {
            return ValueTask.FromResult<IReadOnlyList<SkillRootRegistration>>([]);
        }

        return ValueTask.FromResult<IReadOnlyList<SkillRootRegistration>>(
        [
            new SkillRootRegistration
            {
                RootPath = Path.Combine(context.UserProfileRoot, ".agents", "skills"),
                SourceKind = SkillSourceKind.UserCommon,
                SourceId = $"user-common:{Path.GetFullPath(context.UserProfileRoot)}",
                Scope = SkillScopeKind.User,
                Precedence = 3,
            },
        ]);
    }
}
