namespace CodeAlta.App;

internal interface IAcpManagementRuntimeActions
{
    Task ReloadAcpBackendsAsync();

    Task ProbeAcpBackendAsync(string agentId);
}

internal sealed class DelegatingAcpManagementRuntimeActions : IAcpManagementRuntimeActions
{
    private readonly Func<Task> _reloadAcpBackendsAsync;
    private readonly Func<string, Task> _probeAcpBackendAsync;

    public DelegatingAcpManagementRuntimeActions(
        Func<Task> reloadAcpBackendsAsync,
        Func<string, Task> probeAcpBackendAsync)
    {
        ArgumentNullException.ThrowIfNull(reloadAcpBackendsAsync);
        ArgumentNullException.ThrowIfNull(probeAcpBackendAsync);

        _reloadAcpBackendsAsync = reloadAcpBackendsAsync;
        _probeAcpBackendAsync = probeAcpBackendAsync;
    }

    public Task ReloadAcpBackendsAsync()
        => _reloadAcpBackendsAsync();

    public Task ProbeAcpBackendAsync(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return _probeAcpBackendAsync(agentId);
    }
}
