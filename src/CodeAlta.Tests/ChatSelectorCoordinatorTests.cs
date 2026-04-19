using System.Runtime.CompilerServices;
using System.Reflection;
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
        var threadSelection = CreateThreadSelectionContext();
        var syncCallCount = 0;
        var coordinator = new ChatSelectorCoordinator(
            workspaceViewModel,
            promptComposerViewModel,
            backendStates,
            selectorState,
            threadSelection,
            preferences,
            workspaceRefresh,
            static _ => null,
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

    [TestMethod]
    public void GetPreferredBackendId_UsesConfiguredDefaultProvider()
    {
        var workspaceViewModel = new ThreadWorkspaceViewModel();
        var promptComposerViewModel = new PromptComposerViewModel();
        AgentBackendDescriptor[] backendDescriptors =
        [
            new AgentBackendDescriptor(AgentBackendIds.Codex, "Codex"),
            new AgentBackendDescriptor(new AgentBackendId("zai"), "ZAI"),
        ];
        var backendStates = ChatBackendPresentation.CreateBackendStates(backendDescriptors);
        backendStates[AgentBackendIds.Codex.Value].Availability = ChatBackendAvailability.Ready;
        backendStates["zai"].Availability = ChatBackendAvailability.Ready;

        var coordinator = CreateCoordinator(
            backendDescriptors,
            workspaceViewModel,
            promptComposerViewModel,
            backendStates,
            static _ => "zai");

        var preferredBackendId = coordinator.GetPreferredBackendId();

        Assert.AreEqual("zai", preferredBackendId.Value);
    }

    [TestMethod]
    public void GetPreferredBackendId_FallsBackToFirstReadyProviderDeterministically()
    {
        var workspaceViewModel = new ThreadWorkspaceViewModel();
        var promptComposerViewModel = new PromptComposerViewModel();
        AgentBackendDescriptor[] backendDescriptors =
        [
            new AgentBackendDescriptor(new AgentBackendId("zai"), "ZAI"),
            new AgentBackendDescriptor(new AgentBackendId("openai"), "OpenAI"),
            new AgentBackendDescriptor(AgentBackendIds.Codex, "Codex"),
        ];
        var backendStates = ChatBackendPresentation.CreateBackendStates(backendDescriptors);
        backendStates["zai"].Availability = ChatBackendAvailability.Unsupported;
        backendStates["openai"].Availability = ChatBackendAvailability.Ready;
        backendStates[AgentBackendIds.Codex.Value].Availability = ChatBackendAvailability.Ready;

        var coordinator = CreateCoordinator(
            backendDescriptors,
            workspaceViewModel,
            promptComposerViewModel,
            backendStates,
            static _ => null);

        var preferredBackendId = coordinator.GetPreferredBackendId();

        Assert.AreEqual("openai", preferredBackendId.Value);
    }

    private static ChatSelectorCoordinator CreateCoordinator(
        IReadOnlyList<AgentBackendDescriptor> backendDescriptors,
        ThreadWorkspaceViewModel workspaceViewModel,
        PromptComposerViewModel promptComposerViewModel,
        Dictionary<string, ChatBackendState> backendStates,
        Func<string?, string?> getEffectiveDefaultProviderKey)
    {
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
        var threadSelection = CreateThreadSelectionContext();
        return new ChatSelectorCoordinator(
            backendDescriptors,
            workspaceViewModel,
            promptComposerViewModel,
            backendStates,
            selectorState,
            threadSelection,
            preferences,
            workspaceRefresh,
            getEffectiveDefaultProviderKey,
            static () => { });
    }

    private static ThreadSelectionContext CreateThreadSelectionContext()
    {
        var coordinator = (ShellThreadStateCoordinator)RuntimeHelpers.GetUninitializedObject(typeof(ShellThreadStateCoordinator));
        var selectionCoordinatorField = typeof(ShellThreadStateCoordinator).GetField("_selectionCoordinator", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(selectionCoordinatorField);
        selectionCoordinatorField.SetValue(coordinator, new ShellSelectionCoordinator());
        return new ThreadSelectionContext(
            coordinator,
            static (_, _) => Task.CompletedTask,
            static _ => false);
    }

    private static void ApplyDraftBackendPreference(ChatBackendState backendState)
    {
        backendState.SelectedModelId = ChatBackendPresentation.ResolvePreferredModelId(
            backendState.Models,
            backendState.SelectedModelId);
        backendState.SelectedReasoningEffort = ChatBackendPresentation.ResolvePreferredReasoningEffort(
            ChatBackendPreferenceCoordinator.FindModel(backendState.Models, backendState.SelectedModelId),
            backendState.SelectedReasoningEffort);
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
