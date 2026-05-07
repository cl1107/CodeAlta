using CodeAlta.App.Events;
using CodeAlta.Views;

namespace CodeAlta.App;

internal sealed class CodeAltaProjectionInvalidator : IShellProjectionInvalidator
{
    private readonly CodeAltaApp _app;

    public CodeAltaProjectionInvalidator(CodeAltaApp app)
    {
        ArgumentNullException.ThrowIfNull(app);
        _app = app;
    }

    public void RefreshCatalogAndThreadWorkspace() => _app.RefreshCatalogAndThreadWorkspace();

    public void RefreshHeaderAndThreadWorkspace() => _app.RefreshHeaderAndThreadWorkspace();

    public void RefreshSelectionAndThreadWorkspace() => _app.RefreshSelectionAndThreadWorkspace();

    public void RefreshShellChrome() => _app.RefreshShellChrome();

    public void UpdatePromptAvailabilityUi() => _app.UpdatePromptAvailabilityUi();

    public void RefreshQueuedPromptList() => _app.RefreshQueuedPromptList();

    public void InvalidateSelectedSessionUsage() => _app.InvalidateSelectedSessionUsage();
}
