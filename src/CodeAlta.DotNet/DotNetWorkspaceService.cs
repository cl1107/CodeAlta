namespace CodeAlta.DotNet;

/// <summary>
/// Discovers and snapshots .NET solution/project graph metadata.
/// </summary>
public sealed class DotNetWorkspaceService
{
    /// <summary>
    /// Loads a workspace snapshot from a repository root.
    /// </summary>
    /// <param name="repoRoot">Repository root path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discovered workspace snapshot.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="repoRoot"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the repository root does not exist.</exception>
    public Task<DotNetWorkspaceSnapshot> LoadAsync(
        string repoRoot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            throw new ArgumentException("Repository root is required.", nameof(repoRoot));
        }

        var normalizedRoot = Path.GetFullPath(repoRoot);
        if (!Directory.Exists(normalizedRoot))
        {
            throw new DirectoryNotFoundException($"Repository root '{normalizedRoot}' was not found.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var solutions = Directory.EnumerateFiles(normalizedRoot, "*.sln", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(normalizedRoot, "*.slnx", SearchOption.AllDirectories))
            .Where(path => !IsInIgnoredFolder(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var projects = EnumerateProjectFiles(normalizedRoot)
            .Select(path => new DotNetProjectInfo
            {
                Name = Path.GetFileNameWithoutExtension(path),
                ProjectPath = path,
                Language = GetLanguage(path),
                RelativePath = Path.GetRelativePath(normalizedRoot, path),
            })
            .OrderBy(static x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Task.FromResult(
            new DotNetWorkspaceSnapshot
            {
                RepoRoot = normalizedRoot,
                SolutionPaths = solutions,
                Projects = projects,
                LoadedAt = DateTimeOffset.UtcNow,
            });
    }

    /// <summary>
    /// Lists discovered projects from a repository root.
    /// </summary>
    /// <param name="repoRoot">Repository root path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discovered projects.</returns>
    public async Task<IReadOnlyList<DotNetProjectInfo>> ListProjectsAsync(
        string repoRoot,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await LoadAsync(repoRoot, cancellationToken).ConfigureAwait(false);
        return snapshot.Projects;
    }

    private static IEnumerable<string> EnumerateProjectFiles(string root)
    {
        foreach (var path in Directory.EnumerateFiles(root, "*.*proj", SearchOption.AllDirectories))
        {
            if (IsInIgnoredFolder(path))
            {
                continue;
            }

            var extension = Path.GetExtension(path);
            if (!extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".fsproj", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".vbproj", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return Path.GetFullPath(path);
        }
    }

    private static bool IsInIgnoredFolder(string path)
    {
        return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
               path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ||
               path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetLanguage(string projectPath)
    {
        var extension = Path.GetExtension(projectPath);
        return extension.ToLowerInvariant() switch
        {
            ".csproj" => "csharp",
            ".fsproj" => "fsharp",
            ".vbproj" => "vb",
            _ => "unknown",
        };
    }
}
