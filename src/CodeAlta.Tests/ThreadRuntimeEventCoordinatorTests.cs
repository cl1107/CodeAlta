using CodeAlta.Agent;
using CodeAlta.App;
using CodeAlta.App.State;
using CodeAlta.Catalog;
using CodeAlta.Models;
using CodeAlta.Orchestration.Runtime;
using CodeAlta.Presentation.Timeline;
using CodeAlta.Threading;

namespace CodeAlta.Tests;

[TestClass]
public sealed class ThreadRuntimeEventCoordinatorTests
{
    [TestMethod]
    public void ShouldPromoteAgentEventToThinking_IgnoresToolOutputDeltas()
    {
        var delta = new AgentContentDeltaEvent(
            AgentBackendIds.Codex,
            "session-1",
            DateTimeOffset.UtcNow,
            null,
            AgentContentKind.ToolOutput,
            "tool-1",
            "activity-1",
            "line 1");

        Assert.IsFalse(ThreadRuntimeEventCoordinator.ShouldPromoteAgentEventToThinking(delta));
    }

    [TestMethod]
    public void ShouldRefreshShellChromeAfterRuntimeEvent_IgnoresToolOutputDeltas()
    {
        var runtimeEvent = new WorkThreadAgentEvent(
            "thread-1",
            new AgentContentDeltaEvent(
                AgentBackendIds.Codex,
                "session-1",
                DateTimeOffset.UtcNow,
                null,
                AgentContentKind.ToolOutput,
                "tool-1",
                "activity-1",
                "line 1"));

        Assert.IsFalse(ThreadRuntimeEventCoordinator.ShouldRefreshShellChromeAfterRuntimeEvent(runtimeEvent));
    }

    [TestMethod]
    public void ShouldRefreshShellChromeAfterRuntimeEvent_RefreshesForAssistantCompletion()
    {
        var runtimeEvent = new WorkThreadAgentEvent(
            "thread-1",
            new AgentContentCompletedEvent(
                AgentBackendIds.Codex,
                "session-1",
                DateTimeOffset.UtcNow,
                null,
                AgentContentKind.Assistant,
                "assistant-1",
                null,
                "Final answer"));

        Assert.IsTrue(ThreadRuntimeEventCoordinator.ShouldRefreshShellChromeAfterRuntimeEvent(runtimeEvent));
    }

    [TestMethod]
    public void HandleAgentEvent_KeepsManualCompactionStatusWhileProgressEventsArrive()
    {
        var thread = CreateThread();
        var tab = CreateOpenThreadState(thread);
        tab.PendingManualCompaction = true;
        tab.StatusMessage = $"Compacting '{thread.Title}'...";
        tab.StatusBusy = true;
        tab.StatusTone = StatusTone.Info;
        tab.HasCustomStatus = true;

        var coordinator = CreateCoordinator(thread, tab);
        coordinator.HandleAgentEvent(
            thread,
            tab,
            new AgentActivityEvent(
                AgentBackendIds.Copilot,
                "session-1",
                DateTimeOffset.UtcNow,
                null,
                AgentActivityKind.ToolCall,
                AgentActivityPhase.Started,
                "activity-1",
                null,
                "compact",
                "Compacting session..."));

        Assert.AreEqual($"Compacting '{thread.Title}'...", tab.StatusMessage);
        Assert.IsTrue(tab.StatusBusy);
        Assert.AreEqual(StatusTone.Info, tab.StatusTone);
    }

    [TestMethod]
    public void ApplyRuntimeEvent_HostCompactionCompletionClearsPendingManualCompactionStatus()
    {
        var thread = CreateThread();
        var tab = CreateOpenThreadState(thread);
        tab.PendingManualCompaction = true;
        tab.StatusMessage = $"Compacting '{thread.Title}'...";
        tab.StatusBusy = true;
        tab.StatusTone = StatusTone.Info;
        tab.HasCustomStatus = true;

        var coordinator = CreateCoordinator(thread, tab);
        coordinator.ApplyRuntimeEvent(
            new WorkThreadHostEvent(
                thread.ThreadId,
                DateTimeOffset.UtcNow,
                AgentSessionUpdateKind.CompactionCompleted,
                "Manual compaction completed."));

        Assert.IsFalse(tab.PendingManualCompaction);
        Assert.IsFalse(tab.HasCustomStatus);
        Assert.IsFalse(tab.StatusBusy);
        Assert.IsNull(tab.StatusMessage);
    }

    private static ThreadRuntimeEventCoordinator CreateCoordinator(WorkThreadDescriptor thread, OpenThreadState tab)
    {
        return new ThreadRuntimeEventCoordinator(
            findThread: id => id == thread.ThreadId ? thread : null,
            findOpenThread: id => id == tab.Thread.ThreadId ? tab : null,
            getAutoApproveEnabled: static () => false,
            isSelectedThread: id => id == thread.ThreadId,
            invalidateSelectedSessionUsage: static () => { },
            refreshShellChrome: static () => { },
            setShellStatus: static (_, _, _) => { },
            setThreadStatus: static (state, message, busy, tone) =>
            {
                state.StatusMessage = message;
                state.StatusBusy = busy;
                state.StatusTone = tone;
                state.HasCustomStatus = true;
            },
            clearThreadStatus: static state =>
            {
                state.StatusMessage = null;
                state.StatusBusy = false;
                state.StatusTone = StatusTone.Info;
                state.HasCustomStatus = false;
            },
            drainQueuedPromptAsync: static (_, _) => Task.CompletedTask);
    }

    private static OpenThreadState CreateOpenThreadState(WorkThreadDescriptor thread)
    {
        var timeline = new ThreadTimelinePresenter(new InlineUiDispatcher(), () => true, static () => null);
        return new OpenThreadState(thread, timeline);
    }

    private static WorkThreadDescriptor CreateThread()
    {
        return new WorkThreadDescriptor
        {
            ThreadId = "thread-1",
            Kind = WorkThreadKind.ProjectThread,
            BackendId = AgentBackendIds.Copilot.Value,
            BackendSessionId = "backend-thread-1",
            ProjectRef = "project-1",
            WorkingDirectory = @"C:\code\CodeAlta",
            Title = "Review startup",
            Status = WorkThreadStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            LastActiveAt = DateTimeOffset.UtcNow,
            StartedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class InlineUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess()
            => true;

        public void Post(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            action();
        }

        public Task InvokeAsync(Action action)
        {
            ArgumentNullException.ThrowIfNull(action);
            action();
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(Func<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            return Task.FromResult(action());
        }
    }
}
