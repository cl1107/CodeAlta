using CodeAlta.Threading;
using CodeAlta.Models;
using CodeAlta.ViewModels;

namespace CodeAlta.App.Context;

internal sealed class ChatSelectorStateContext
{
    private readonly ThreadWorkspaceViewModel _workspaceViewModel;
    private readonly Func<IUiDispatcher> _getUiDispatcher;
    private readonly Action _verifyBindableAccess;

    public ChatSelectorStateContext(
        ThreadWorkspaceViewModel workspaceViewModel,
        Func<IUiDispatcher> getUiDispatcher,
        Action verifyBindableAccess)
    {
        ArgumentNullException.ThrowIfNull(workspaceViewModel);
        ArgumentNullException.ThrowIfNull(getUiDispatcher);
        ArgumentNullException.ThrowIfNull(verifyBindableAccess);

        _workspaceViewModel = workspaceViewModel;
        _getUiDispatcher = getUiDispatcher;
        _verifyBindableAccess = verifyBindableAccess;
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
        => _getUiDispatcher();

    public void VerifyBindableAccess()
        => _verifyBindableAccess();
}
