using CodeAlta.Agent;
using CodeAlta.Catalog;
using CodeAlta.ViewModels;
using XenoAtom.Ansi;
using XenoAtom.Logging;
using XenoAtom.Terminal;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Commands;
using XenoAtom.Terminal.UI.Controls;
using XenoAtom.Terminal.UI.Extensions.Markdown;
using XenoAtom.Terminal.UI.Figlet;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Input;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Styling;
using XenoAtom.Terminal.UI.Text;
using XenoAtom.Terminal.UI.Threading;

internal sealed partial class CodeAltaApp
{
    private CodeAltaShellView EnsureShellView()
    {
        _threadWorkspaceView ??= new ThreadWorkspaceView(
            _shellViewModel,
            _threadWorkspaceViewModel,
            _promptComposerViewModel,
            () => CreateUsageComputedVisual(EnsureSessionUsagePresenter().BuildIndicatorVisual),
            CreatePromptEditor,
            () => _ = SendSelectedThreadPromptAsync(steer: false),
            OnThreadTabControlSelectionChanged,
            OnChatBackendSelectionChanged,
            OnChatModelSelectionChanged,
            OnChatReasoningSelectionChanged,
            OnChatAutoScrollChanged);

        RefreshSidebarProjection();
        RefreshThreadPaneContent();

        _shellView ??= new CodeAltaShellView(
            _shellViewModel,
            _sidebarCoordinator.View.Root,
            _threadWorkspaceView.Root,
            ThreadCommandBar!);
        return _shellView;
    }

    internal static Visual CreateThreadTabPageContentPlaceholder()
        // The active thread flow is hosted by the splitter, so tabs need a detached placeholder.
        => new Placeholder
        {
            IsVisible = false,
        };

    private void RefreshShellChrome()
        => DispatchToUi(RefreshShellChromeCore);

    internal void RefreshCatalogAndThreadWorkspace()
    {
        DispatchToUi(
            () =>
            {
                RefreshCatalogAndThreadWorkspaceCore();
            });
    }

    private void RefreshHeaderAndThreadWorkspace()
    {
        DispatchToUi(
            () =>
            {
                RefreshHeaderAndThreadWorkspaceCore();
            });
    }

    private void RefreshSelectionAndThreadWorkspace()
    {
        DispatchToUi(
            () =>
            {
                RefreshSelectionAndThreadWorkspaceCore();
            });
    }

    private void RefreshHeaderAndThreadWorkspaceCore()
    {
        VerifyBindableAccess();
        EnsureSelectionDefaults();
        _shellViewModel.HeaderText = BuildHeaderText();
        RefreshThreadWorkspaceCore();
    }

    private void RefreshShellChromeCore()
    {
        VerifyBindableAccess();
        EnsureSelectionDefaults();
        _shellViewModel.HeaderText = BuildHeaderText();
        RefreshSidebarProjection();
    }

    private void RefreshCatalogAndThreadWorkspaceCore()
    {
        RefreshShellChromeCore();
        RefreshThreadWorkspaceCore();
    }

    private void RefreshSelectionAndThreadWorkspaceCore()
    {
        VerifyBindableAccess();
        EnsureSelectionDefaults();
        _shellViewModel.HeaderText = BuildHeaderText();
        SyncSidebarSelectionToCurrentState();
        RefreshThreadWorkspaceCore();
    }

    private void RefreshThreadWorkspaceCore()
    {
        SyncSelectedSessionUsageViewModel();
        _viewRefreshState.Value++;
        _usageRefreshState.Value++;
        RefreshThreadPaneContent();
    }

    private void RefreshThreadPaneContent()
    {
        if (ThreadPaneLayout is null || ThreadBodySplitter is null || ThreadInput is null)
        {
            return;
        }

        SyncThreadTabControl();

        var selectedThread = GetSelectedThread();
        if (selectedThread is null)
        {
            RefreshChatSelectorsForDraftScope();
            UpdatePromptAvailabilityUi();
            ThreadBodySplitter.First = WelcomePaneFactory.Build(GetSelectedProject(), _globalScopeSelected);
            SetReadyStatusForCurrentSelection();

            return;
        }

        var tab = EnsureThreadTab(selectedThread);
        RefreshChatSelectorsForThread(tab);
        UpdatePromptAvailabilityUi();
        ThreadBodySplitter.First = tab.Timeline.Flow;
        SetReadyStatusForCurrentSelection();
    }

    internal void SelectGlobalScope()
    {
        ResetPendingThreadTabSelection();
        _draftTabOpen = true;
        _globalScopeSelected = true;
        _selectedThreadId = null;
        _viewState.SelectedThreadId = null;
        _viewState.UpdatedAt = DateTimeOffset.UtcNow;
        _ = PersistViewStateAsync();
        RefreshSelectionAndThreadWorkspace();
    }

    internal void SelectProjectScope(string projectId)
    {
        ResetPendingThreadTabSelection();
        _draftTabOpen = true;
        _globalScopeSelected = false;
        _selectedProjectId = projectId;
        _selectedThreadId = null;
        _viewState.SelectedThreadId = null;
        _viewState.UpdatedAt = DateTimeOffset.UtcNow;
        _ = PersistViewStateAsync();
        RefreshSelectionAndThreadWorkspace();
    }

    private void EnsureSelectionDefaults()
    {
        if (!string.IsNullOrWhiteSpace(_selectedThreadId) &&
            _threads.All(thread => !string.Equals(thread.ThreadId, _selectedThreadId, StringComparison.OrdinalIgnoreCase)))
        {
            _selectedThreadId = null;
        }

        if (string.IsNullOrWhiteSpace(_selectedProjectId) ||
            _projects.All(project => !string.Equals(project.Id, _selectedProjectId, StringComparison.OrdinalIgnoreCase)))
        {
            _selectedProjectId = _projects.FirstOrDefault()?.Id;
        }

        if (!_globalScopeSelected && _selectedProjectId is null)
        {
            _globalScopeSelected = true;
        }
    }

    private string BuildHeaderText()
    {
        return ShellTextFormatter.BuildHeaderText(
            GetSelectedThread(),
            GetSelectedProject(),
            _catalogOptions.GlobalRoot,
            GetPreferredBackendId().Value,
            _globalScopeSelected);
    }

    internal void SetStatus(string message, bool showSpinner = false, StatusTone tone = StatusTone.Info)
    {
        DispatchToUi(
            () =>
            {
                VerifyBindableAccess();
                _shellViewModel.StatusText = message;
                _shellViewModel.StatusBusy = showSpinner;
                _shellViewModel.StatusTone = tone;
                _shellViewModel.StatusIconMarkup = StatusVisualFormatter.BuildStatusIconMarkup(tone);
            });
    }

    internal static StatusSnapshot ResolveSelectionStatus(
        string readyMessage,
        bool hasThreadStatus,
        string? threadStatusMessage,
        bool threadStatusBusy,
        StatusTone threadStatusTone,
        bool promptUnavailable,
        string? promptUnavailableMessage,
        StatusTone promptUnavailableTone)
    {
        if (hasThreadStatus && !string.IsNullOrWhiteSpace(threadStatusMessage))
        {
            return new StatusSnapshot(threadStatusMessage!, threadStatusBusy, threadStatusTone);
        }

        if (promptUnavailable && !string.IsNullOrWhiteSpace(promptUnavailableMessage))
        {
            return new StatusSnapshot(promptUnavailableMessage!, Busy: false, promptUnavailableTone);
        }

        return new StatusSnapshot(readyMessage, Busy: false, StatusTone.Ready);
    }

    private void SetThreadStatus(
        OpenThreadState tab,
        string message,
        bool showSpinner = false,
        StatusTone tone = StatusTone.Info,
        bool hasCustomStatus = true)
    {
        ArgumentNullException.ThrowIfNull(tab);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var changed =
            !string.Equals(tab.StatusMessage, message, StringComparison.Ordinal) ||
            tab.StatusBusy != showSpinner ||
            tab.StatusTone != tone ||
            tab.HasCustomStatus != hasCustomStatus;

        tab.StatusMessage = message;
        tab.StatusBusy = showSpinner;
        tab.StatusTone = tone;
        tab.HasCustomStatus = hasCustomStatus;

        if (IsSelectedThread(tab.Thread.ThreadId))
        {
            SetReadyStatusForCurrentSelection();
        }

        if (changed)
        {
            InvalidateThreadChrome();
        }
    }

    private void ClearThreadStatus(OpenThreadState tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        SetThreadStatus(
            tab,
            ShellTextFormatter.BuildReadyStatusText(tab.Thread, GetSelectedProject(), globalScopeSelected: false),
            tone: StatusTone.Ready,
            hasCustomStatus: false);
    }

    private void InvalidateThreadChrome()
    {
        DispatchToUi(() => _viewRefreshState.Value++);
    }

    private void InvalidateSelectedSessionUsage()
    {
        DispatchToUi(
            () =>
            {
                SyncSelectedSessionUsageViewModel();
                _usageRefreshState.Value++;
            });
    }

    private bool IsSelectedThread(string threadId)
        => !string.IsNullOrWhiteSpace(threadId) &&
           string.Equals(_selectedThreadId, threadId, StringComparison.OrdinalIgnoreCase);

    internal void SetReadyStatusForCurrentSelection()
    {
        var selectedThread = GetSelectedThread();
        var readyMessage = ShellTextFormatter.BuildReadyStatusText(selectedThread, GetSelectedProject(), _globalScopeSelected);
        var promptUnavailable = TryGetPromptUnavailableStatus(out var promptUnavailableMessage, out var promptUnavailableTone);
        if (selectedThread is not null &&
            _threadTabs.TryGetValue(selectedThread.ThreadId, out var selectedTab))
        {
            var snapshot = ResolveSelectionStatus(
                readyMessage,
                selectedTab.HasCustomStatus,
                selectedTab.ViewModel.StatusMessage,
                selectedTab.ViewModel.StatusBusy,
                selectedTab.ViewModel.StatusTone,
                promptUnavailable,
                promptUnavailableMessage,
                promptUnavailableTone);
            SetStatus(snapshot.Message, snapshot.Busy, snapshot.Tone);
            return;
        }

        if (promptUnavailable)
        {
            SetStatus(promptUnavailableMessage, tone: promptUnavailableTone);
            return;
        }

        SetStatus(readyMessage, tone: StatusTone.Ready);
    }

    private SessionUsagePresenter EnsureSessionUsagePresenter()
    {
        _sessionUsagePresenter ??= new SessionUsagePresenter(
            _sessionUsageViewModel,
            markdown => (ThreadPaneLayout?.App)?.Terminal.Clipboard.TrySetText(markdown),
            build => CreateUsageComputedVisual(build));
        return _sessionUsagePresenter;
    }

    private void SyncSelectedSessionUsageViewModel()
    {
        VerifyBindableAccess();
        var selectedThread = GetSelectedThread();
        if (selectedThread is not null)
        {
            var tab = EnsureThreadTab(selectedThread);
            var backendState = _chatBackendStates[tab.BackendId.Value];
            _sessionUsageViewModel.Usage = tab.Usage;
            _sessionUsageViewModel.BackendName = backendState.DisplayName;
            _sessionUsageViewModel.ModelName = tab.ModelId ?? backendState.SelectedModelId;
            return;
        }

        var backendId = GetPreferredBackendId();
        var draftBackendState = _chatBackendStates[backendId.Value];
        _sessionUsageViewModel.Usage = null;
        _sessionUsageViewModel.BackendName = draftBackendState.DisplayName;
        _sessionUsageViewModel.ModelName = draftBackendState.SelectedModelId;
    }

    private T ReadBindableState<T>(Func<T> read)
    {
        ArgumentNullException.ThrowIfNull(read);

        return UiDispatch.Invoke(
            GetUiDispatcher(),
            () =>
            {
                VerifyBindableAccess();
                return read();
            });
    }

    internal void SetShellInitialized(bool isInitialized)
    {
        DispatchToUi(() => _shellViewModel.IsInitialized = isInitialized);
    }

    private void DispatchToUi(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var dispatcher = GetUiDispatcher();
        UiDispatch.Post(
            dispatcher,
            action,
            allowInline: ShouldRunInlineOnCurrentThread(
                dispatcher.CheckAccess(),
                _terminalLoopStarted));
    }

    internal static bool CanAccessBindableState(
        bool dispatcherHasAccess,
        bool terminalLoopStarted)
    {
        if (!terminalLoopStarted)
        {
            return true;
        }

        return dispatcherHasAccess;
    }

    private void VerifyBindableAccess()
    {
        var dispatcher = GetUiDispatcher();
        if (CanAccessBindableState(dispatcher.CheckAccess(), _terminalLoopStarted))
        {
            return;
        }

        throw new InvalidOperationException("Bindable view-model state must be accessed on the UI thread.");
    }

    internal static bool ShouldRunInlineOnCurrentThread(
        bool dispatcherHasAccess,
        bool terminalLoopStarted)
    {
        if (!terminalLoopStarted)
        {
            return true;
        }

        return dispatcherHasAccess;
    }

    private IUiDispatcher GetUiDispatcher()
        => _uiDispatcher ??= new TerminalUiDispatcher(Dispatcher.Current);

    private ComputedVisual CreateComputedVisual(Func<Visual> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        return new ComputedVisual(
            () =>
            {
                var _ = _viewRefreshState.Value;
                return build();
            });
    }

    private ComputedVisual CreateUsageComputedVisual(Func<Visual> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        return new ComputedVisual(
            () =>
            {
                var _ = _usageRefreshState.Value;
                return build();
            });
    }

    private void ClearThreadInput()
    {
        UiDispatch.Invoke(
            GetUiDispatcher(),
            () =>
            {
                ThreadInput!.Text = string.Empty;
                return 0;
            });
    }

    private void ClearThreadTitleDraft()
    {
        DispatchToUi(() => _sidebarViewModel.DraftThreadTitle = string.Empty);
    }

    private async Task ActivateDraftTabAsync()
    {
        ResetPendingThreadTabSelection();
        _draftTabOpen = true;
        _selectedThreadId = null;
        _viewState.SelectedThreadId = null;
        _viewState.UpdatedAt = DateTimeOffset.UtcNow;
        await PersistViewStateAsync().ConfigureAwait(false);
        RefreshSelectionAndThreadWorkspace();
    }

    private async Task CloseDraftTabAsync()
    {
        _draftTabOpen = false;
        if (string.IsNullOrWhiteSpace(_selectedThreadId))
        {
            _selectedThreadId = _viewState.OpenThreadIds.FirstOrDefault();
            _viewState.SelectedThreadId = _selectedThreadId;
        }

        _viewState.UpdatedAt = DateTimeOffset.UtcNow;
        await PersistViewStateAsync().ConfigureAwait(false);
        RefreshSelectionAndThreadWorkspace();
    }

    private bool GetAutoApproveEnabled()
        => DefaultAutoApproveEnabled;

    private async Task PersistViewStateAsync()
    {
        try
        {
            await _threadCatalog.SaveViewStateAsync(_viewState, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (LogManager.IsInitialized && UiLogger.IsEnabled(LogLevel.Error))
            {
                UiLogger.Error(ex, "Failed to persist thread view state.");
            }
        }
    }

    private static Group CreateSectionGroup(string title, Visual content)
    {
        return new Group(new Markup($"[bold]{title}[/]"), content)
            .Padding(1)
            .Style(XenoAtom.Terminal.UI.Styling.GroupStyle.Rounded);
    }

    private ChatPromptEditor CreatePromptEditor()
    {
        var converter = new MarkdownMarkupConverter();
        var editor = new ChatPromptEditor(text => _ = SendSelectedThreadPromptAsync(steer: false))
            .PromptMarkup("[primary]>[/] ")
            .ContinuationPromptMarkup("[muted]·[/] ")
            .Placeholder(_promptComposerViewModel.Bind.Placeholder)
            .EnterMode(PromptEditorEnterMode.EnterInsertsNewLine)
            .EnableWordHints(true)
            .Highlighter(HighlightMarkdown)
            .MinHeight(3)
            .Style(PromptEditorStyle.Default with
            {
                Padding = new Thickness(0, 0, 1, 0),
                PlaceholderForeground = UiPalette.PromptPlaceholderColor,
            });

        editor.AddCommand(new Command
        {
            Id = "CodeAlta.Thread.Steer",
            LabelMarkup = "Steer",
            DescriptionMarkup = "Send an immediate steering instruction to the selected thread.",
            Gesture = new KeyGesture(TerminalKey.F5),
            Importance = CommandImportance.Primary,
            Presentation = CommandPresentation.CommandBar,
            Execute = _visual => { _ = SendSelectedThreadPromptAsync(steer: true); },
            CanExecute = _visual => _promptComposerViewModel.CanSteer,
        });

        editor.AddCommand(new Command
        {
            Id = "CodeAlta.Thread.Delegate",
            LabelMarkup = "Delegate",
            DescriptionMarkup = "Create a delegated internal thread from the current project thread.",
            Gesture = new KeyGesture(TerminalKey.F7),
            Presentation = CommandPresentation.CommandBar,
            Execute = _visual => { _ = DelegateSelectedThreadAsync(); },
            CanExecute = _visual => _promptComposerViewModel.CanDelegate,
        });

        editor.AddCommand(new Command
        {
            Id = "CodeAlta.Thread.Abort",
            LabelMarkup = "Abort",
            DescriptionMarkup = "Abort the selected thread run.",
            Gesture = new KeyGesture(TerminalKey.F8),
            Presentation = CommandPresentation.CommandBar,
            Execute = _visual => { _ = AbortSelectedThreadAsync(); },
            CanExecute = _visual => _promptComposerViewModel.CanAbort,
        });

        editor.AddCommand(new Command
        {
            Id = "CodeAlta.Thread.CloseTab",
            LabelMarkup = "Close Tab",
            DescriptionMarkup = "Close the current thread tab.",
            Gesture = new KeyGesture(TerminalKey.F9),
            Presentation = CommandPresentation.CommandBar,
            Execute = _visual => { _ = GetSelectedThread() is not null ? CloseSelectedThreadAsync() : CloseDraftTabAsync(); },
            CanExecute = _visual => _promptComposerViewModel.CanCloseTab,
        });

        return editor;

        void HighlightMarkdown(in PromptEditorHighlightRequest request, List<StyledRun> runs)
        {
            converter.Theme = request.Theme;
            converter.Highlight(SnapshotToString(request.Snapshot), runs);
        }

        static string SnapshotToString(ITextSnapshot snapshot)
        {
            if (snapshot.Length == 0)
            {
                return string.Empty;
            }

            return string.Create(snapshot.Length, snapshot, static (span, s) => s.CopyTo(0, span));
        }
    }
}
