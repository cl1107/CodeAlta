using CodeAlta.Agent;
using CodeAlta.Catalog;
using CodeAlta.Threading;

namespace CodeAlta.App;

internal interface IThreadModelProviderReadinessService
{
    bool IsModelProviderReady(WorkThreadDescriptor thread);
}

internal sealed class ThreadModelProviderReadinessService : IThreadModelProviderReadinessService
{
    private readonly Func<WorkThreadDescriptor, bool> _isModelProviderReady;

    public ThreadModelProviderReadinessService(Func<WorkThreadDescriptor, bool> isModelProviderReady)
    {
        ArgumentNullException.ThrowIfNull(isModelProviderReady);
        _isModelProviderReady = isModelProviderReady;
    }

    public bool IsModelProviderReady(WorkThreadDescriptor thread)
    {
        ArgumentNullException.ThrowIfNull(thread);
        return _isModelProviderReady(thread);
    }
}

internal interface IThreadHistoryLoaderService
{
    Task EnsureThreadHistoryLoadedAsync(WorkThreadDescriptor thread, CancellationToken cancellationToken);
}

internal sealed class ThreadHistoryLoaderService : IThreadHistoryLoaderService
{
    private readonly Func<WorkThreadDescriptor, CancellationToken, Task> _ensureThreadHistoryLoadedAsync;

    public ThreadHistoryLoaderService(Func<WorkThreadDescriptor, CancellationToken, Task> ensureThreadHistoryLoadedAsync)
    {
        ArgumentNullException.ThrowIfNull(ensureThreadHistoryLoadedAsync);
        _ensureThreadHistoryLoadedAsync = ensureThreadHistoryLoadedAsync;
    }

    public Task EnsureThreadHistoryLoadedAsync(WorkThreadDescriptor thread, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(thread);
        return _ensureThreadHistoryLoadedAsync(thread, cancellationToken);
    }
}

internal interface IThreadStateTabLifecycleService
{
    void ResetPendingThreadTabSelection();

    void RemoveThreadTabPage(string threadId, ShellTabCloseReason reason);
}

internal sealed class ThreadStateTabLifecycleService : IThreadStateTabLifecycleService
{
    private readonly Action _resetPendingThreadTabSelection;
    private readonly Action<string, ShellTabCloseReason> _removeThreadTabPage;

    public ThreadStateTabLifecycleService(
        Action resetPendingThreadTabSelection,
        Action<string, ShellTabCloseReason> removeThreadTabPage)
    {
        ArgumentNullException.ThrowIfNull(resetPendingThreadTabSelection);
        ArgumentNullException.ThrowIfNull(removeThreadTabPage);
        _resetPendingThreadTabSelection = resetPendingThreadTabSelection;
        _removeThreadTabPage = removeThreadTabPage;
    }

    public void ResetPendingThreadTabSelection()
        => _resetPendingThreadTabSelection();

    public void RemoveThreadTabPage(string threadId, ShellTabCloseReason reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);
        _removeThreadTabPage(threadId, reason);
    }
}
