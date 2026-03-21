using CodeAlta.Models;

namespace CodeAlta.App;

internal sealed class PromptDraftCoordinator
{
    private string _draftPromptText = string.Empty;

    public void RememberPrompt(ThreadSessionState? session, string? text)
    {
        var normalized = text ?? string.Empty;
        if (session is null)
        {
            _draftPromptText = normalized;
            return;
        }

        session.PromptDraftText = normalized;
    }

    public string GetPrompt(ThreadSessionState? session)
        => session?.PromptDraftText ?? _draftPromptText;
}
