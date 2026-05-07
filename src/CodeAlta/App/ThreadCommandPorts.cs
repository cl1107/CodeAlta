using CodeAlta.App.State;
using CodeAlta.Catalog;
using CodeAlta.Models;
using CodeAlta.Threading;

namespace CodeAlta.App;

internal interface IThreadLifecycleCommandPort
{
    Task<WorkThreadDescriptor?> CreateGlobalThreadAsync(string? title = null);

    Task<WorkThreadDescriptor?> CreateProjectThreadAsync(string? title = null);

    Task PersistViewStateAsync();

    void RekeyThreadIdentity(string oldThreadId, WorkThreadDescriptor thread);
}

internal sealed class DelegatingThreadLifecycleCommandPort : IThreadLifecycleCommandPort
{
    private readonly Func<string?, Task<WorkThreadDescriptor?>> _createGlobalThreadAsync;
    private readonly Func<string?, Task<WorkThreadDescriptor?>> _createProjectThreadAsync;
    private readonly Func<Task> _persistViewStateAsync;
    private readonly Action<string, WorkThreadDescriptor>? _rekeyThreadIdentity;

    public DelegatingThreadLifecycleCommandPort(
        Func<string?, Task<WorkThreadDescriptor?>> createGlobalThreadAsync,
        Func<string?, Task<WorkThreadDescriptor?>> createProjectThreadAsync,
        Func<Task> persistViewStateAsync,
        Action<string, WorkThreadDescriptor>? rekeyThreadIdentity = null)
    {
        ArgumentNullException.ThrowIfNull(createGlobalThreadAsync);
        ArgumentNullException.ThrowIfNull(createProjectThreadAsync);
        ArgumentNullException.ThrowIfNull(persistViewStateAsync);

        _createGlobalThreadAsync = createGlobalThreadAsync;
        _createProjectThreadAsync = createProjectThreadAsync;
        _persistViewStateAsync = persistViewStateAsync;
        _rekeyThreadIdentity = rekeyThreadIdentity;
    }

    public Task<WorkThreadDescriptor?> CreateGlobalThreadAsync(string? title = null)
        => _createGlobalThreadAsync(title);

    public Task<WorkThreadDescriptor?> CreateProjectThreadAsync(string? title = null)
        => _createProjectThreadAsync(title);

    public Task PersistViewStateAsync()
        => _persistViewStateAsync();

    public void RekeyThreadIdentity(string oldThreadId, WorkThreadDescriptor thread)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldThreadId);
        ArgumentNullException.ThrowIfNull(thread);
        _rekeyThreadIdentity?.Invoke(oldThreadId, thread);
    }
}

internal interface IThreadCommandUiPort
{
    bool TrySetPromptUnavailableStatus();

    bool GetAutoApproveEnabled();

    void ClearDraftInput();

    void SetReadyStatusForCurrentSelection();

    void RefreshHeaderAndThreadWorkspace();

    void RefreshCatalogAndThreadWorkspace();

    void TryRenderInteraction(OpenThreadState tab, Action action, string context);
}

internal sealed class ThreadCommandUiPort : IThreadCommandUiPort
{
    private readonly IUiDispatcher _uiDispatcher;
    private readonly Func<bool> _trySetPromptUnavailableStatus;
    private readonly Func<bool> _getAutoApproveEnabled;
    private readonly Action _clearDraftInput;
    private readonly Action _setReadyStatusForCurrentSelection;
    private readonly Action _refreshHeaderAndThreadWorkspace;
    private readonly Action _refreshCatalogAndThreadWorkspace;
    private readonly Action<OpenThreadState, Action, string> _tryRenderInteraction;

    public ThreadCommandUiPort(
        IUiDispatcher uiDispatcher,
        Func<bool> trySetPromptUnavailableStatus,
        Func<bool> getAutoApproveEnabled,
        Action clearDraftInput,
        Action setReadyStatusForCurrentSelection,
        Action refreshHeaderAndThreadWorkspace,
        Action refreshCatalogAndThreadWorkspace,
        Action<OpenThreadState, Action, string> tryRenderInteraction)
    {
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(trySetPromptUnavailableStatus);
        ArgumentNullException.ThrowIfNull(getAutoApproveEnabled);
        ArgumentNullException.ThrowIfNull(clearDraftInput);
        ArgumentNullException.ThrowIfNull(setReadyStatusForCurrentSelection);
        ArgumentNullException.ThrowIfNull(refreshHeaderAndThreadWorkspace);
        ArgumentNullException.ThrowIfNull(refreshCatalogAndThreadWorkspace);
        ArgumentNullException.ThrowIfNull(tryRenderInteraction);

        _uiDispatcher = uiDispatcher;
        _trySetPromptUnavailableStatus = trySetPromptUnavailableStatus;
        _getAutoApproveEnabled = getAutoApproveEnabled;
        _clearDraftInput = clearDraftInput;
        _setReadyStatusForCurrentSelection = setReadyStatusForCurrentSelection;
        _refreshHeaderAndThreadWorkspace = refreshHeaderAndThreadWorkspace;
        _refreshCatalogAndThreadWorkspace = refreshCatalogAndThreadWorkspace;
        _tryRenderInteraction = tryRenderInteraction;
    }

    public bool TrySetPromptUnavailableStatus()
        => _uiDispatcher.Invoke(_trySetPromptUnavailableStatus);

    public bool GetAutoApproveEnabled()
        => _uiDispatcher.Invoke(_getAutoApproveEnabled);

    public void ClearDraftInput()
        => _uiDispatcher.Invoke(_clearDraftInput);

    public void SetReadyStatusForCurrentSelection()
        => _uiDispatcher.Invoke(_setReadyStatusForCurrentSelection);

    public void RefreshHeaderAndThreadWorkspace()
        => _uiDispatcher.Invoke(_refreshHeaderAndThreadWorkspace);

    public void RefreshCatalogAndThreadWorkspace()
        => _uiDispatcher.Invoke(_refreshCatalogAndThreadWorkspace);

    public void TryRenderInteraction(OpenThreadState tab, Action action, string context)
    {
        ArgumentNullException.ThrowIfNull(tab);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        _uiDispatcher.Invoke(() => _tryRenderInteraction(tab, action, context));
    }
}
