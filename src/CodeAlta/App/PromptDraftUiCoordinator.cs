using CodeAlta.Models;
using CodeAlta.ViewModels;
using XenoAtom.Terminal.UI;

namespace CodeAlta.App;

internal sealed class PromptDraftUiCoordinator
{
    private readonly PromptDraftCoordinator _promptDrafts;
    private readonly PromptDraftViewModel _viewModel;
    private readonly Action<ThreadSessionState?, PromptDraftChange> _onPromptChanged;
    private ThreadSessionState? _selectedSession;
    private bool _syncingPromptText;

    public PromptDraftUiCoordinator(
        PromptDraftCoordinator promptDrafts,
        Action<ThreadSessionState?, PromptDraftChange> onPromptChanged)
    {
        ArgumentNullException.ThrowIfNull(promptDrafts);
        ArgumentNullException.ThrowIfNull(onPromptChanged);

        _promptDrafts = promptDrafts;
        _onPromptChanged = onPromptChanged;
        _viewModel = new PromptDraftViewModel(OnPromptTextChanged);
    }

    public Binding<string?> PromptTextBinding => _viewModel.Bind.PromptText;

    public string? PromptText
    {
        get => _viewModel.PromptText;
        set => _viewModel.PromptText = value ?? string.Empty;
    }

    public void SyncPromptText(ThreadSessionState? session)
    {
        _selectedSession = session;

        var promptText = _promptDrafts.GetPrompt(session);
        if (!string.Equals(PromptText, promptText, StringComparison.Ordinal))
        {
            _syncingPromptText = true;
            try
            {
                PromptText = promptText;
            }
            finally
            {
                _syncingPromptText = false;
            }
        }
    }

    public void ClearPromptText()
        => PromptText = string.Empty;

    public void ClearDraftPromptText()
    {
        var change = _promptDrafts.RememberPrompt(null, string.Empty);
        _onPromptChanged(null, change);
        if (_selectedSession is null)
        {
            PromptText = string.Empty;
        }
    }

    private void OnPromptTextChanged(string? value)
    {
        if (_syncingPromptText)
        {
            return;
        }

        var change = _promptDrafts.RememberPrompt(_selectedSession, value);
        _onPromptChanged(_selectedSession, change);
    }
}
