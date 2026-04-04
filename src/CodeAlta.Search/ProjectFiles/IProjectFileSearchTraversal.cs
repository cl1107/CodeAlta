namespace CodeAlta.Search;

internal interface IProjectFileSearchTraversal
{
    ProjectFileTraversalSnapshot Traverse(
        string projectRoot,
        IReadOnlyDictionary<string, ProjectFileUsageEntry> usageByRelativePath,
        int batchSize,
        Action<IReadOnlyList<ProjectFileSearchItem>> onBatch,
        CancellationToken cancellationToken);
}
