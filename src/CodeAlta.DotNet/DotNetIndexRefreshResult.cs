using CodeAlta.Persistence;

namespace CodeAlta.DotNet;

/// <summary>
/// Represents the result of refreshing .NET knowledge/index artifacts.
/// </summary>
public sealed record DotNetIndexRefreshResult
{
    /// <summary>
    /// Gets project graph artifact id.
    /// </summary>
    public required ArtifactId ProjectGraphArtifactId { get; init; }

    /// <summary>
    /// Gets number of symbol records discovered.
    /// </summary>
    public required int SymbolCount { get; init; }

    /// <summary>
    /// Gets number of indexed documents produced.
    /// </summary>
    public required int IndexedDocumentCount { get; init; }
}
