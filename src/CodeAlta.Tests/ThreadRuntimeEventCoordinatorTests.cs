using CodeAlta.Agent;
using CodeAlta.App;
using CodeAlta.Orchestration.Runtime;

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
}
