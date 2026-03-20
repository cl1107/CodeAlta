namespace CodeAlta.App.Context;

internal sealed class WorkspaceRefreshContext
{
    private readonly Action _invalidateSelectedSessionUsage;
    private readonly Action _refreshHeaderAndThreadWorkspace;

    public WorkspaceRefreshContext(
        Action invalidateSelectedSessionUsage,
        Action refreshHeaderAndThreadWorkspace)
    {
        ArgumentNullException.ThrowIfNull(invalidateSelectedSessionUsage);
        ArgumentNullException.ThrowIfNull(refreshHeaderAndThreadWorkspace);

        _invalidateSelectedSessionUsage = invalidateSelectedSessionUsage;
        _refreshHeaderAndThreadWorkspace = refreshHeaderAndThreadWorkspace;
    }

    public void InvalidateSelectedSessionUsage()
        => _invalidateSelectedSessionUsage();

    public void RefreshHeaderAndThreadWorkspace()
        => _refreshHeaderAndThreadWorkspace();
}
