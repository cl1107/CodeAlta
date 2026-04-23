namespace CodeAlta.Catalog.Skills;

/// <summary>
/// Provides filesystem skill roots for discovery.
/// </summary>
public interface ISkillRootProvider
{
    /// <summary>
    /// Resolves skill roots for the provided discovery context.
    /// </summary>
    /// <param name="context">Discovery context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolved root registrations.</returns>
    ValueTask<IReadOnlyList<SkillRootRegistration>> GetRootsAsync(
        SkillDiscoveryContext context,
        CancellationToken cancellationToken = default);
}
