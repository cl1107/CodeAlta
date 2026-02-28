namespace CodeAlta.DotNet;

/// <summary>
/// Produces compact, linkable .NET context snippets for agent consumption.
/// </summary>
public sealed class DotNetContextProvider
{
    private readonly DotNetWorkspaceService _workspaceService;
    private readonly SymbolIndexService _symbolIndexService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotNetContextProvider"/> class.
    /// </summary>
    /// <param name="workspaceService">Workspace discovery service.</param>
    /// <param name="symbolIndexService">Symbol index service.</param>
    /// <exception cref="ArgumentNullException">Thrown when required dependencies are <see langword="null"/>.</exception>
    public DotNetContextProvider(
        DotNetWorkspaceService workspaceService,
        SymbolIndexService symbolIndexService)
    {
        ArgumentNullException.ThrowIfNull(workspaceService);
        ArgumentNullException.ThrowIfNull(symbolIndexService);

        _workspaceService = workspaceService;
        _symbolIndexService = symbolIndexService;
    }

    /// <summary>
    /// Returns compact snippets matching a symbol query.
    /// </summary>
    /// <param name="repoRoot">Repository root.</param>
    /// <param name="query">Symbol query text.</param>
    /// <param name="limit">Maximum snippets.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Symbol context snippets.</returns>
    public async Task<IReadOnlyList<DotNetContextSnippet>> SymbolContextAsync(
        string repoRoot,
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query is required.", nameof(query));
        }

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be positive.");
        }

        var snapshot = await _workspaceService.LoadAsync(repoRoot, cancellationToken).ConfigureAwait(false);
        var symbols = await _symbolIndexService.BuildIndexAsync(snapshot, cancellationToken).ConfigureAwait(false);

        return symbols
            .Where(x =>
                x.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                x.FullyQualifiedName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static x => x.FilePath, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(ToSnippet)
            .ToArray();
    }

    /// <summary>
    /// Returns snippets for declarations found in a single file.
    /// </summary>
    /// <param name="filePath">Source file path.</param>
    /// <param name="limit">Maximum snippets.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>File context snippets.</returns>
    public async Task<IReadOnlyList<DotNetContextSnippet>> FileContextAsync(
        string filePath,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be positive.");
        }

        var symbols = await _symbolIndexService.BuildIndexForFileAsync(filePath, cancellationToken).ConfigureAwait(false);
        return symbols
            .OrderBy(static x => x.StartLine)
            .Take(limit)
            .Select(ToSnippet)
            .ToArray();
    }

    private static DotNetContextSnippet ToSnippet(DotNetSymbolRecord symbol)
    {
        var summary = string.IsNullOrWhiteSpace(symbol.Summary) ? string.Empty : $" | {symbol.Summary}";
        return new DotNetContextSnippet
        {
            Title = symbol.FullyQualifiedName,
            Content = $"{symbol.Kind} {symbol.FullyQualifiedName} ({Path.GetFileName(symbol.FilePath)}:{symbol.StartLine}){summary}".Trim(),
            SourceUri = $"file://{symbol.FilePath}#L{symbol.StartLine}",
        };
    }
}
