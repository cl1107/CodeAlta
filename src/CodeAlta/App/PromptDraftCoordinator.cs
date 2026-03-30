using CodeAlta.Models;

namespace CodeAlta.App;

internal sealed class PromptDraftCoordinator
{
    private string _draftPromptText = string.Empty;

    public PromptDraftChange RememberPrompt(ThreadSessionState? session, string? text)
    {
        var normalized = text ?? string.Empty;
        var previous = session?.PromptDraftText ?? _draftPromptText;
        var textChanged = !string.Equals(previous, normalized, StringComparison.Ordinal);
        var editedStateChanged = string.IsNullOrWhiteSpace(previous) != string.IsNullOrWhiteSpace(normalized);

        if (session is null)
        {
            _draftPromptText = normalized;
            return new PromptDraftChange(textChanged, editedStateChanged);
        }

        session.PromptDraftText = normalized;
        return new PromptDraftChange(textChanged, editedStateChanged);
    }

    public string GetPrompt(ThreadSessionState? session)
        => session?.PromptDraftText ?? _draftPromptText;
}
