using XenoAtom.Terminal.UI;
using CodeAlta.Presentation.Prompting;

namespace CodeAlta.ViewModels;

public sealed partial class ThreadWorkspaceViewModel
{
    public ThreadWorkspaceViewModel()
    {
        BackendStatusMarkup = string.Empty;
        AutoScroll = true;
        SelectedTabIndex = -1;
        QueuedPrompts = [];
    }

    [Bindable]
    public partial string BackendStatusMarkup { get; set; }

    [Bindable]
    public partial bool CanSelectBackend { get; set; }

    [Bindable]
    public partial bool CanSelectModel { get; set; }

    [Bindable]
    public partial bool CanSelectReasoning { get; set; }

    [Bindable]
    public partial bool CanToggleAutoScroll { get; set; }

    [Bindable]
    public partial bool AutoScroll { get; set; }

    [Bindable]
    public partial int SelectedTabIndex { get; set; }

    [Bindable]
    public partial bool HasQueuedPrompts { get; set; }

    [Bindable]
    public partial IReadOnlyList<QueuedPromptListItem> QueuedPrompts { get; set; }

    internal void SetQueuedPrompts(IReadOnlyList<QueuedPromptListItem> queuedPrompts)
    {
        ArgumentNullException.ThrowIfNull(queuedPrompts);

        QueuedPrompts = queuedPrompts;
        HasQueuedPrompts = queuedPrompts.Count > 0;
    }
}
