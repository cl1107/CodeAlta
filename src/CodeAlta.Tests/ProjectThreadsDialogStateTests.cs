using CodeAlta.App;
using CodeAlta.ViewModels;

namespace CodeAlta.Tests;

[TestClass]
public sealed class ProjectThreadsDialogStateTests
{
    [TestMethod]
    public void SelectionCommands_UpdateSelectedRows()
    {
        var state = new ProjectThreadsDialogState(
        [
            CreateRow("thread-1"),
            CreateRow("thread-2"),
            CreateRow("thread-3"),
        ]);

        state.SelectAll();
        Assert.AreEqual(3, state.SelectedCount);

        state.InvertSelection();
        Assert.AreEqual(0, state.SelectedCount);

        state.SelectAll();
        state.SelectNone();
        Assert.AreEqual(0, state.SelectedCount);
    }

    [TestMethod]
    public void RemoveThreads_RemovesMatchingRows()
    {
        var state = new ProjectThreadsDialogState(
        [
            CreateRow("thread-1"),
            CreateRow("thread-2"),
            CreateRow("thread-3"),
        ]);

        var removed = state.RemoveThreads(["thread-1", "thread-3"]);

        Assert.AreEqual(2, removed);
        CollectionAssert.AreEqual(new[] { "thread-2" }, state.Rows.Select(static row => row.ThreadId).ToArray());
    }

    private static ProjectThreadsDialogRowViewModel CreateRow(string threadId)
    {
        return new ProjectThreadsDialogRowViewModel
        {
            ThreadId = threadId,
            Title = threadId,
        };
    }
}
