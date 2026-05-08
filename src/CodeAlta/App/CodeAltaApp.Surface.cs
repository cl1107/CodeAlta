using CodeAlta.Catalog;
using CodeAlta.Frontend.Commands;
using CodeAlta.Presentation.Prompting;
using CodeAlta.ViewModels;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace CodeAlta.Views;

internal sealed class CodeAltaAppSurfaceRequest
{
    public required CodeAltaShellViewModel ShellViewModel { get; init; }

    public required ThreadWorkspaceViewModel WorkspaceViewModel { get; init; }

    public required PromptComposerViewModel PromptComposerViewModel { get; init; }

    public required IReadOnlyList<ThreadWorkspaceCommandBinding> WorkspaceCommandBindings { get; init; }

    public required ThreadWorkspaceChromeController WorkspaceChromeController { get; init; }

    public required PromptComposerViewController PromptComposerController { get; init; }

    public required QueuedPromptStripController QueuedPromptController { get; init; }

    public required ModelProviderSelectorController ModelProviderSelectorController { get; init; }

    public required ThreadTabHostController ThreadTabHostController { get; init; }

    public required IProjectFileSearchService ProjectFileSearchService { get; init; }

    public required Func<string?> GetPromptReferenceProjectRoot { get; init; }

    public required Binding<string?> PromptText { get; init; }

    public required State<float> ThinkingAnimationPhase01 { get; init; }

    public PromptImageWorkspaceCallbacks? PromptImageCallbacks { get; init; }

    public required Visual Sidebar { get; init; }

    public required ShellCommandSurfaceCoordinator ShellCommandSurfaceCoordinator { get; init; }

    public required Action OpenAcpManager { get; init; }

    public required Action ToggleTerminalLoopCallback { get; init; }

    public required Action FocusSidebar { get; init; }

    public required Action FocusPromptEditor { get; init; }

    public required Func<bool> CanUseCommandPalette { get; init; }

    public Func<CommandBar, Visual?>? ComposePluginFooter { get; init; }

    public bool CommandBarMultiLine { get; init; }
}

internal static class CodeAltaAppSurfaceFactory
{
    public static CodeAltaShellSurface Create(CodeAltaAppSurfaceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return CodeAltaShellViewFactory.CreateSurface(new CodeAltaShellSurfaceOptions
        {
            ShellViewModel = request.ShellViewModel,
            WorkspaceViewModel = request.WorkspaceViewModel,
            PromptComposerViewModel = request.PromptComposerViewModel,
            WorkspaceCommandBindings = request.WorkspaceCommandBindings,
            WorkspaceChromeController = request.WorkspaceChromeController,
            PromptComposerController = request.PromptComposerController,
            QueuedPromptController = request.QueuedPromptController,
            ModelProviderSelectorController = request.ModelProviderSelectorController,
            ThreadTabHostController = request.ThreadTabHostController,
            ProjectFileSearchService = request.ProjectFileSearchService,
            GetPromptReferenceProjectRoot = request.GetPromptReferenceProjectRoot,
            PromptText = request.PromptText,
            ThinkingAnimationPhase01 = request.ThinkingAnimationPhase01,
            PromptImageCallbacks = request.PromptImageCallbacks,
            Sidebar = request.Sidebar,
            ShellCommandSurfaceCoordinator = request.ShellCommandSurfaceCoordinator,
            OpenAcpManager = request.OpenAcpManager,
            ToggleTerminalLoopCallback = request.ToggleTerminalLoopCallback,
            FocusSidebar = request.FocusSidebar,
            FocusPromptEditor = request.FocusPromptEditor,
            CanUseCommandPalette = request.CanUseCommandPalette,
            ComposePluginFooter = request.ComposePluginFooter,
            CommandBarMultiLine = request.CommandBarMultiLine,
        });
    }
}
