namespace CodeAlta.Search;

internal sealed record ProjectFileTraversalSnapshot(
    bool IsGitAware,
    IReadOnlyList<ProjectFileSearchItem> Items);
