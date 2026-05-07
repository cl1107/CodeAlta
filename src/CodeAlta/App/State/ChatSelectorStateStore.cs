using CodeAlta.Models;
using CodeAlta.Threading;
using CodeAlta.ViewModels;

namespace CodeAlta.App.State;

internal sealed class ChatSelectorStateStore
{
    private readonly ThreadWorkspaceViewModel _workspaceViewModel;
    private readonly IUiDispatcher _uiDispatcher;

    public ChatSelectorStateStore(
        ThreadWorkspaceViewModel workspaceViewModel,
        IUiDispatcher uiDispatcher)
    {
        ArgumentNullException.ThrowIfNull(workspaceViewModel);
        ArgumentNullException.ThrowIfNull(uiDispatcher);

        _workspaceViewModel = workspaceViewModel;
        _uiDispatcher = uiDispatcher;
    }

    public int? GetSelectedBackendIndex()
        => _workspaceViewModel.SelectedBackendIndex >= 0 ? _workspaceViewModel.SelectedBackendIndex : null;

    public int? GetSelectedModelIndex()
        => _workspaceViewModel.SelectedModelIndex >= 0 ? _workspaceViewModel.SelectedModelIndex : null;

    public int? GetSelectedReasoningIndex()
        => _workspaceViewModel.SelectedReasoningIndex >= 0 ? _workspaceViewModel.SelectedReasoningIndex : null;

    public void SetBackendSelection(IReadOnlyList<ChatBackendOption> items, int selectedIndex)
    {
        ArgumentNullException.ThrowIfNull(items);
        _workspaceViewModel.BackendOptions = items;
        _workspaceViewModel.SelectedBackendIndex = selectedIndex;
    }

    public void SetModelSelection(IReadOnlyList<ChatModelOption> items, int selectedIndex)
    {
        ArgumentNullException.ThrowIfNull(items);
        _workspaceViewModel.ModelOptions = items;
        _workspaceViewModel.SelectedModelIndex = selectedIndex;
    }

    public void SetReasoningSelection(IReadOnlyList<ChatReasoningOption> items, int selectedIndex)
    {
        ArgumentNullException.ThrowIfNull(items);
        _workspaceViewModel.ReasoningOptions = items;
        _workspaceViewModel.SelectedReasoningIndex = selectedIndex;
    }

    public IUiDispatcher GetUiDispatcher()
        => _uiDispatcher;

    public void VerifyBindableAccess()
        => _uiDispatcher.VerifyAccess();
}
