using CodeAlta.Presentation.Prompting;
using CodeAlta.ViewModels;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace CodeAlta.Views;

internal sealed class QueuedPromptStripView
{
    public QueuedPromptStripView(
        ThreadWorkspaceViewModel workspaceViewModel,
        Action<string> copyMarkdown,
        Action<string> convertQueuedPromptToSteer,
        Action<string> deletePendingSteer,
        Action<string> deleteQueuedPrompt,
        Action<string, int> updateQueuedPromptCount,
        Action<string, string> updateQueuedPromptText,
        Func<Action<string>, string?, ChatPromptEditor> createPromptEditor)
    {
        ArgumentNullException.ThrowIfNull(workspaceViewModel);
        ArgumentNullException.ThrowIfNull(copyMarkdown);
        ArgumentNullException.ThrowIfNull(convertQueuedPromptToSteer);
        ArgumentNullException.ThrowIfNull(deletePendingSteer);
        ArgumentNullException.ThrowIfNull(deleteQueuedPrompt);
        ArgumentNullException.ThrowIfNull(updateQueuedPromptCount);
        ArgumentNullException.ThrowIfNull(updateQueuedPromptText);
        ArgumentNullException.ThrowIfNull(createPromptEditor);

        Root = new ComputedVisual(
            () => QueuedPromptListView.Build(
                workspaceViewModel.PromptStripItems,
                copyMarkdown,
                convertQueuedPromptToSteer,
                deletePendingSteer,
                deleteQueuedPrompt,
                updateQueuedPromptCount,
                updateQueuedPromptText,
                createPromptEditor));
    }

    public Visual Root { get; }
}
