namespace CodeAlta.DotNet;

/// <summary>
/// Represents a discovered .NET workspace snapshot.
/// </summary>
public sealed record DotNetWorkspaceSnapshot
{
    /// <summary>
    /// Gets the repo root path.
    /// </summary>
    public required string RepoRoot { get; init; }

    /// <summary>
    /// Gets discovered solution paths.
    /// </summary>
    public required IReadOnlyList<string> SolutionPaths { get; init; }

    /// <summary>
    /// Gets discovered project infos.
    /// </summary>
    public required IReadOnlyList<DotNetProjectInfo> Projects { get; init; }

    /// <summary>
    /// Gets snapshot creation timestamp in UTC.
    /// </summary>
    public required DateTimeOffset LoadedAt { get; init; }
}

/// <summary>
/// Represents discovered .NET project metadata.
/// </summary>
public sealed record DotNetProjectInfo
{
    /// <summary>
    /// Gets project name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets full project file path.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Gets project language identifier (csharp/fsharp/vb).
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// Gets path relative to repo root.
    /// </summary>
    public required string RelativePath { get; init; }
}
