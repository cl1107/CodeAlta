using CodeAlta.Catalog;
using CodeAlta.Models;
using CodeAlta.Threading;
using CodeAlta.Views;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Styling;

namespace CodeAlta.App;

internal sealed class NavigatorActionCoordinator
{
    private readonly CodeAltaShellController _shellController;
    private readonly ShellThreadStateCoordinator _threadStateCoordinator;
    private readonly Func<IUiDispatcher> _getUiDispatcher;
    private readonly Func<Rectangle?> _getDialogBounds;
    private readonly Func<Visual?> _getFocusTarget;
    private readonly Action<string, bool, StatusTone> _setStatus;
    private readonly Action _setReadyStatusForCurrentSelection;

    public NavigatorActionCoordinator(
        CodeAltaShellController shellController,
        ShellThreadStateCoordinator threadStateCoordinator,
        Func<IUiDispatcher> getUiDispatcher,
        Func<Rectangle?> getDialogBounds,
        Func<Visual?> getFocusTarget,
        Action<string, bool, StatusTone> setStatus,
        Action setReadyStatusForCurrentSelection)
    {
        ArgumentNullException.ThrowIfNull(shellController);
        ArgumentNullException.ThrowIfNull(threadStateCoordinator);
        ArgumentNullException.ThrowIfNull(getUiDispatcher);
        ArgumentNullException.ThrowIfNull(getDialogBounds);
        ArgumentNullException.ThrowIfNull(getFocusTarget);
        ArgumentNullException.ThrowIfNull(setStatus);
        ArgumentNullException.ThrowIfNull(setReadyStatusForCurrentSelection);

        _shellController = shellController;
        _threadStateCoordinator = threadStateCoordinator;
        _getUiDispatcher = getUiDispatcher;
        _getDialogBounds = getDialogBounds;
        _getFocusTarget = getFocusTarget;
        _setStatus = setStatus;
        _setReadyStatusForCurrentSelection = setReadyStatusForCurrentSelection;
    }

    public void ConfirmDeleteThread(string threadId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadId);

        var thread = _threadStateCoordinator.FindThread(threadId);
        if (thread is null)
        {
            _setStatus($"Thread '{threadId}' was not found.", false, StatusTone.Warning);
            return;
        }

        var project = _threadStateCoordinator.GetProjectById(thread.ProjectRef);
        var bodyLines = new List<string>
        {
            $"Delete thread '{thread.Title}'?",
        };
        if (project is not null)
        {
            bodyLines.Add($"Project: {project.DisplayName}");
        }

        new ConfirmationDialog(
            "Delete Thread",
            bodyLines,
            "Delete",
            ControlTone.Error,
            () => DeleteThreadAsync(thread),
            _getDialogBounds,
            _getFocusTarget)
            .Show();
    }

    public void ConfirmDeleteProject(string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var project = _threadStateCoordinator.GetProjectById(projectId);
        if (project is null)
        {
            _setStatus($"Project '{projectId}' was not found.", false, StatusTone.Warning);
            return;
        }

        var visibleThreads = _threadStateCoordinator.Threads
            .Where(thread =>
                thread.Status != WorkThreadStatus.Archived &&
                string.Equals(thread.ProjectRef, projectId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        new ConfirmationDialog(
            "Delete Project Threads",
            [
                $"Delete {visibleThreads.Length} thread(s) from '{project.DisplayName}'?",
                "The project will be hidden from the default navigator view.",
            ],
            "Delete",
            ControlTone.Error,
            () => DeleteProjectAsync(project, visibleThreads),
            _getDialogBounds,
            _getFocusTarget,
            noteText: "This deletes thread history only. The project directory on disk is not deleted.")
            .Show();
    }

    public void OpenProjectThreads(string projectId)
    {
        ArgumentNullException.ThrowIfNull(projectId);

        if (string.IsNullOrWhiteSpace(projectId))
        {
            var globalThreads = _threadStateCoordinator.Threads
                .Where(thread =>
                    thread.Status != WorkThreadStatus.Archived &&
                    thread.Kind == WorkThreadKind.GlobalThread)
                .ToArray();

            new ProjectThreadsDialog(
                CreateGlobalDialogProject(),
                globalThreads,
                threadIds => DeleteProjectThreadsAsync(projectId: null, threadIds),
                threadId => _shellController.OpenThreadAsync(threadId, CancellationToken.None),
                _getUiDispatcher,
                _getDialogBounds,
                _getFocusTarget)
                .Show();
            return;
        }

        var project = _threadStateCoordinator.GetProjectById(projectId);
        if (project is null)
        {
            _setStatus($"Project '{projectId}' was not found.", false, StatusTone.Warning);
            return;
        }

        var threads = _threadStateCoordinator.Threads
            .Where(thread =>
                thread.Status != WorkThreadStatus.Archived &&
                string.Equals(thread.ProjectRef, projectId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        new ProjectThreadsDialog(
            project,
            threads,
            threadIds => DeleteProjectThreadsAsync(projectId, threadIds),
            threadId => _shellController.OpenThreadAsync(threadId, CancellationToken.None),
            _getUiDispatcher,
            _getDialogBounds,
            _getFocusTarget)
            .Show();
    }

    public void OpenProjectDetails(string projectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var project = _threadStateCoordinator.GetProjectById(projectId);
        if (project is null)
        {
            _setStatus($"Project '{projectId}' was not found.", false, StatusTone.Warning);
            return;
        }

        new ProjectDetailsDialog(
            project,
            SaveProjectAsync,
            _getUiDispatcher,
            _getDialogBounds,
            _getFocusTarget)
            .Show();
    }

    public async Task RenameProjectDisplayNameAsync(string projectId, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        var project = _threadStateCoordinator.GetProjectById(projectId);
        if (project is null)
        {
            throw new InvalidOperationException($"Project '{projectId}' was not found.");
        }

        var updatedProject = CloneProject(project);
        updatedProject.DisplayName = displayName.Trim();
        await SaveProjectAsync(updatedProject).ConfigureAwait(false);
    }

    private async Task DeleteThreadAsync(WorkThreadDescriptor thread)
    {
        try
        {
            _setStatus($"Deleting thread '{thread.Title}'...", true, StatusTone.Info);
            await _shellController.DeleteThreadAsync(thread.ThreadId, CancellationToken.None).ConfigureAwait(false);
            await _getUiDispatcher().InvokeAsync(() =>
            {
                _threadStateCoordinator.RemoveDeletedThread(thread.ThreadId, thread.ProjectRef);
                _setReadyStatusForCurrentSelection();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _setStatus($"Failed to delete thread: {ex.Message}", false, StatusTone.Error);
        }
    }

    private async Task DeleteProjectAsync(ProjectDescriptor project, IReadOnlyList<WorkThreadDescriptor> threads)
    {
        try
        {
            _setStatus($"Deleting threads for project '{project.DisplayName}'...", true, StatusTone.Info);
            var result = await _shellController.DeleteProjectAsync(project.Id, CancellationToken.None).ConfigureAwait(false);
            await _getUiDispatcher().InvokeAsync(() =>
            {
                _threadStateCoordinator.RemoveDeletedProject(project.Id, result.DeletedThreadIds);
                _setReadyStatusForCurrentSelection();
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _setStatus($"Failed to delete project threads: {ex.Message}", false, StatusTone.Error);
        }
    }

    private async Task DeleteProjectThreadsAsync(string? projectId, IReadOnlyList<string> threadIds)
    {
        try
        {
            _setStatus($"Deleting {threadIds.Count} thread(s)...", true, StatusTone.Info);
            var fallbackProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId;
            foreach (var threadId in threadIds)
            {
                await _shellController.DeleteThreadAsync(threadId, CancellationToken.None).ConfigureAwait(false);
                await _getUiDispatcher().InvokeAsync(
                    () => _threadStateCoordinator.RemoveDeletedThread(threadId, fallbackProjectId))
                    .ConfigureAwait(false);
            }

            await _getUiDispatcher().InvokeAsync(_setReadyStatusForCurrentSelection).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _setStatus($"Failed to delete selected threads: {ex.Message}", false, StatusTone.Error);
        }
    }

    private async Task SaveProjectAsync(ProjectDescriptor project)
    {
        try
        {
            _setStatus($"Saving project '{project.DisplayName}'...", true, StatusTone.Info);
            await _shellController.SaveProjectAsync(project, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _setStatus($"Failed to save project: {ex.Message}", false, StatusTone.Error);
            throw;
        }
    }

    private static ProjectDescriptor CloneProject(ProjectDescriptor project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return new ProjectDescriptor
        {
            Id = project.Id,
            Slug = project.Slug,
            Name = project.Name,
            DisplayName = project.DisplayName,
            ProjectPath = project.ProjectPath,
            DefaultBranch = project.DefaultBranch,
            Description = project.Description,
            Tags = [.. project.Tags],
            Checkout = project.Checkout,
            SourcePath = project.SourcePath,
            Archived = project.Archived,
            MarkdownBody = project.MarkdownBody,
        };
    }

    private static ProjectDescriptor CreateGlobalDialogProject()
    {
        return new ProjectDescriptor
        {
            Id = "__global__",
            Slug = "global",
            Name = "Global",
            DisplayName = "Global",
            ProjectPath = string.Empty,
            DefaultBranch = string.Empty,
        };
    }
}
