using System.Runtime.CompilerServices;
using CodeAlta.Agent;
using CodeAlta.App;
using CodeAlta.App.Context;
using CodeAlta.Models;
using CodeAlta.Presentation.Chat;
using CodeAlta.Presentation.Workspace;
using CodeAlta.Threading;
using CodeAlta.ViewModels;

namespace CodeAlta.Tests;

[TestClass]
public sealed class ChatSelectorCoordinatorTests
{
    [TestMethod]
    public void RefreshForDraftScope_SwitchingBackend_UpdatesModelOptionsAndSyncsSelectors()
    {
        var workspaceViewModel = new ThreadWorkspaceViewModel();
        var promptComposerViewModel = new PromptComposerViewModel();
        var backendStates = ChatBackendPresentation.CreateBackendStates();

        var codexState = backendStates[AgentBackendIds.Codex.Value];
        codexState.Availability = ChatBackendAvailability.Ready;
        codexState.Models.Add(new AgentModelInfo(
            "gpt-5-codex",
            DisplayName: "GPT-5 Codex",
            DefaultReasoningEffort: AgentReasoningEffort.High,
            SupportedReasoningEfforts: [AgentReasoningEffort.Medium, AgentReasoningEffort.High]));

        var copilotState = backendStates[AgentBackendIds.Copilot.Value];
        copilotState.Availability = ChatBackendAvailability.Ready;
        copilotState.Models.Add(new AgentModelInfo(
            "gpt-4.1",
            DisplayName: "GPT-4.1",
            DefaultReasoningEffort: AgentReasoningEffort.Medium,
            SupportedReasoningEfforts: [AgentReasoningEffort.Low, AgentReasoningEffort.Medium]));
        copilotState.Models.Add(new AgentModelInfo(
            "o4-mini",
            DisplayName: "o4-mini",
            DefaultReasoningEffort: AgentReasoningEffort.Low,
            SupportedReasoningEfforts: [AgentReasoningEffort.Low]));

        var selectorState = new ChatSelectorStateContext(
            workspaceViewModel,
            static () => new InlineUiDispatcher(),
            static () => { });
        var preferences = new ChatPreferenceContext(
            ApplyDraftBackendPreference,
            static _ => throw new NotSupportedException(),
            static (_, _, _) => { },
            static (_, _, _, _, _) => { });
        var workspaceRefresh = new WorkspaceRefreshContext(
            static () => { },
            static () => { });
        var threadSelection = (ThreadSelectionContext)RuntimeHelpers.GetUninitializedObject(typeof(ThreadSelectionContext));
        var syncCallCount = 0;
        var coordinator = new ChatSelectorCoordinator(
            workspaceViewModel,
            promptComposerViewModel,
            backendStates,
            selectorState,
            threadSelection,
            preferences,
            workspaceRefresh,
            () => syncCallCount++);

        coordinator.RefreshForDraftScope(AgentBackendIds.Codex);

        CollectionAssert.AreEqual(new[] { "GPT-5 Codex" }, workspaceViewModel.ModelOptions.Select(static option => option.Label).ToArray());
        Assert.AreEqual(1, syncCallCount);

        coordinator.RefreshForDraftScope(AgentBackendIds.Copilot);

        Assert.AreEqual(1, workspaceViewModel.SelectedBackendIndex);
        CollectionAssert.AreEqual(new[] { "GPT-4.1", "o4-mini" }, workspaceViewModel.ModelOptions.Select(static option => option.Label).ToArray());
        Assert.AreEqual(2, syncCallCount);

        static void ApplyDraftBackendPreference(ChatBackendState backendState)
        {
            backendState.SelectedModelId = ChatBackendPresentation.ResolvePreferredModelId(
                backendState.Models,
                backendState.SelectedModelId);
            backendState.SelectedReasoningEffort = ChatBackendPresentation.ResolvePreferredReasoningEffort(
                ChatBackendPreferenceCoordinator.FindModel(backendState.Models, backendState.SelectedModelId),
                backendState.SelectedReasoningEffort);
        }
    }

    private sealed class InlineUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => true;

        public void Post(Action callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            callback();
        }

        public Task InvokeAsync(Action callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            callback();
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(Func<T> callback)
        {
            ArgumentNullException.ThrowIfNull(callback);
            return Task.FromResult(callback());
        }
    }
}
