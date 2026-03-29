using CodeAlta.ViewModels;

namespace CodeAlta.App;

internal sealed class ProjectThreadsDialogState
{
    private readonly List<ProjectThreadsDialogRowViewModel> _rows;

    public ProjectThreadsDialogState(IEnumerable<ProjectThreadsDialogRowViewModel> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _rows = rows.ToList();
    }

    public IReadOnlyList<ProjectThreadsDialogRowViewModel> Rows => _rows;

    public int SelectedCount => _rows.Count(static row => row.IsSelected);

    public IReadOnlyList<string> GetSelectedThreadIds()
        => _rows
            .Where(static row => row.IsSelected)
            .Select(static row => row.ThreadId)
            .ToArray();

    public void SelectAll()
    {
        foreach (var row in _rows)
        {
            row.IsSelected = true;
        }
    }

    public void SelectNone()
    {
        foreach (var row in _rows)
        {
            row.IsSelected = false;
        }
    }

    public void InvertSelection()
    {
        foreach (var row in _rows)
        {
            row.IsSelected = !row.IsSelected;
        }
    }

    public int RemoveThreads(IReadOnlyCollection<string> threadIds)
    {
        ArgumentNullException.ThrowIfNull(threadIds);
        return _rows.RemoveAll(row => threadIds.Contains(row.ThreadId, StringComparer.OrdinalIgnoreCase));
    }
}
