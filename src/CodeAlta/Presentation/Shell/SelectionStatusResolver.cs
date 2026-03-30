using CodeAlta.Models;

namespace CodeAlta.Presentation.Shell;

internal static class SelectionStatusResolver
{
    public static StatusSnapshot Resolve(
        string readyMessage,
        bool hasThreadStatus,
        string? threadStatusMessage,
        bool threadStatusBusy,
        StatusTone threadStatusTone,
        bool promptEdited,
        bool promptUnavailable,
        string? promptUnavailableMessage,
        StatusTone promptUnavailableTone)
    {
        if (hasThreadStatus && !string.IsNullOrWhiteSpace(threadStatusMessage))
        {
            return new StatusSnapshot(threadStatusMessage!, threadStatusBusy, threadStatusTone);
        }

        if (promptUnavailable && !string.IsNullOrWhiteSpace(promptUnavailableMessage))
        {
            return new StatusSnapshot(promptUnavailableMessage!, Busy: false, promptUnavailableTone);
        }

        if (promptEdited)
        {
            return new StatusSnapshot(
                StatusVisualFormatter.BuildPromptEditedStatusText(),
                Busy: false,
                StatusTone.Info,
                StatusVisualFormatter.BuildPromptEditedIconMarkup());
        }

        return new StatusSnapshot(readyMessage, Busy: false, StatusTone.Ready);
    }
}
