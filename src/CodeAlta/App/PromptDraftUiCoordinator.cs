using CodeAlta.Models;
using CodeAlta.ViewModels;
using XenoAtom.Terminal.UI;

namespace CodeAlta.App;

internal sealed class PromptDraftUiCoordinator
{
    private readonly PromptDraftCoordinator _promptDrafts;
    private readonly PromptDraftViewModel _viewModel;
    private ThreadSessionState? _selectedSession;

    public PromptDraftUiCoordinator(PromptDraftCoordinator promptDrafts)
    {
        ArgumentNullException.ThrowIfNull(promptDrafts);

        _promptDrafts = promptDrafts;
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
            PromptText = promptText;
        }
    }

    public void ClearPromptText()
        => PromptText = string.Empty;

    public void ClearDraftPromptText()
    {
        _promptDrafts.RememberPrompt(session: null, string.Empty);
        if (_selectedSession is null)
        {
            PromptText = string.Empty;
        }
    }

    private void OnPromptTextChanged(string? value)
        => _promptDrafts.RememberPrompt(_selectedSession, value);
}
