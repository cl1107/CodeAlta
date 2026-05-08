using CodeAlta.Presentation.Prompting;

namespace CodeAlta.App;

internal interface IPromptSessionService
{
    bool IsPromptTextEmpty();

    void ClearPromptText();

    void RestorePromptText(string prompt);

    IReadOnlyList<PromptImageAttachment> SnapshotPromptImages();

    void RestorePromptImages(IReadOnlyList<PromptImageAttachment> images);

    void UpdatePromptAvailabilityUi();

    void UpdatePromptImageAttachmentsUi();
}
