using CodeAlta.App.Events;
using CodeAlta.App.State;
using CodeAlta.Catalog;
using CodeAlta.LiveTool;
using CodeAlta.Models;
using CodeAlta.ViewModels;
using CodeAlta.Views;
using XenoAtom.Logging;
using XenoAtom.Terminal.UI.Styling;

namespace CodeAlta.App;

internal sealed class AskModeCoordinator : IDisposable
{
    private readonly IAltaAskService _askService;
    private readonly ShellSessionStateCoordinator _sessionState;
    private readonly SessionCommandCoordinator _sessionCommands;
    private readonly FrontendEventPublisher _frontendEvents;
    private readonly SessionWorkspaceViewModel _workspaceViewModel;
    private readonly Action<string, bool, StatusTone> _setStatus;
    private readonly IDisposable _subscription;
    private string? _activeAskId;
    private string? _activeSessionId;

    public AskModeCoordinator(
        IAltaAskService askService,
        ShellSessionStateCoordinator sessionState,
        SessionCommandCoordinator sessionCommands,
        FrontendEventPublisher frontendEvents,
        SessionWorkspaceViewModel workspaceViewModel,
        Action<string, bool, StatusTone> setStatus)
    {
        ArgumentNullException.ThrowIfNull(askService);
        ArgumentNullException.ThrowIfNull(sessionState);
        ArgumentNullException.ThrowIfNull(sessionCommands);
        ArgumentNullException.ThrowIfNull(frontendEvents);
        ArgumentNullException.ThrowIfNull(workspaceViewModel);
        ArgumentNullException.ThrowIfNull(setStatus);

        _askService = askService;
        _sessionState = sessionState;
        _sessionCommands = sessionCommands;
        _frontendEvents = frontendEvents;
        _workspaceViewModel = workspaceViewModel;
        _setStatus = setStatus;
        _subscription = _frontendEvents.Subscribe(OnFrontendEvent);
    }

    public void Dispose() => _subscription.Dispose();

    internal bool TryPresentPendingAsk(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        if (_activeAskId is not null)
        {
            return false;
        }

        var ask = _askService.Peek(sessionId);
        if (ask is null || !TryGetIdleSession(sessionId, out var session, out var tab))
        {
            return false;
        }

        _activeAskId = ask.AskId;
        _activeSessionId = sessionId;
        try
        {
            _sessionState.OpenSession(sessionId);
            var form = new AskQuestionFormView(ask);
            form.Submitted += (_, answers) => _ = UiTaskDiagnostics.ObserveAsync(
                () => SubmitAsync(ask, session, tab, answers),
                "submit ask response",
                _setStatus);
            form.CancelRequested += (_, _) => ShowCancelConfirmation(ask, form);
            if (!_workspaceViewModel.TryEnterAskMode(sessionId, form.Root))
            {
                ClearActive();
                return false;
            }

            _setStatus("Answer the queued ask, then submit to continue the session.", false, StatusTone.Info);
            _workspaceViewModel.FocusAskModeControl(form.Tabs);
            return true;
        }
        catch
        {
            ClearActive();
            throw;
        }
    }

    private void OnFrontendEvent(ShellFrontendEvent frontendEvent)
    {
        switch (frontendEvent)
        {
            case AskQueueChangedEvent ask:
                _ = TryPresentPendingAsk(ask.SessionId);
                break;
            case SessionStatusChangedEvent status:
                _ = TryPresentPendingAsk(status.SessionId);
                break;
            case SelectionChangedEvent:
                if (_sessionState.GetSelectedSession() is { } session)
                {
                    _ = TryPresentPendingAsk(session.SessionId);
                }

                break;
        }
    }

    private async Task SubmitAsync(AltaQueuedAsk ask, SessionViewDescriptor session, OpenSessionState tab, IReadOnlyList<AltaAskAnswer> answers)
    {
        if (!IsActive(ask))
        {
            return;
        }

        var markdown = AltaAskAnswerMarkdownFormatter.Format(ask.Request, answers);
        try
        {
            RestoreNormalProjection(ask.SessionId);
            await _sessionCommands.SendAskResponseAsync(session, tab, markdown, ask.AskId);
            _ = _askService.Dequeue(ask.SessionId);
            ClearActive();
            _ = TryPresentPendingAsk(ask.SessionId);
        }
        catch (Exception ex)
        {
            CodeAltaApp.UiLogger.Error(ex, $"Failed to submit ask response for session {ask.SessionId}");
            _activeAskId = null;
            _activeSessionId = null;
            _setStatus($"Failed to submit ask response: {ex.Message}", false, StatusTone.Error);
            _ = TryPresentPendingAsk(ask.SessionId);
        }
    }

    private void Cancel(AltaQueuedAsk ask)
    {
        if (!IsActive(ask))
        {
            return;
        }

        _ = _askService.Dequeue(ask.SessionId);
        RestoreNormalProjection(ask.SessionId);
        ClearActive();
        _setStatus("Ask canceled; no response was sent.", false, StatusTone.Warning);
        _ = TryPresentPendingAsk(ask.SessionId);
    }

    private void ShowCancelConfirmation(AltaQueuedAsk ask, AskQuestionFormView form)
    {
        if (!IsActive(ask))
        {
            return;
        }

        new ConfirmationDialog(
            "Cancel Ask",
            [
                "Exit ask mode without sending a response?",
                "The queued ask will be canceled locally and the session will return to the normal prompt editor.",
            ],
            "Exit without responding",
            ControlTone.Warning,
            () =>
            {
                Cancel(ask);
                return Task.CompletedTask;
            },
            _workspaceViewModel.GetAskModeBounds,
            () => form.Tabs)
            .Show();
    }

    private bool TryGetIdleSession(string sessionId, out SessionViewDescriptor session, out OpenSessionState tab)
    {
        session = null!;
        tab = null!;
        var candidate = _sessionState.FindSession(sessionId);
        if (candidate is null)
        {
            return false;
        }

        var openTab = _sessionState.EnsureSessionTab(candidate);
        if (openTab.StatusBusy || openTab.ActiveRunId is not null || openTab.ActiveRunStartedAt is not null)
        {
            return false;
        }

        session = candidate;
        tab = openTab;
        return true;
    }

    private bool IsActive(AltaQueuedAsk ask)
        => string.Equals(_activeAskId, ask.AskId, StringComparison.Ordinal) && string.Equals(_activeSessionId, ask.SessionId, StringComparison.Ordinal);

    private void RestoreNormalProjection(string sessionId)
        => _workspaceViewModel.ExitAskMode(sessionId);

    private void ClearActive()
    {
        _activeAskId = null;
        _activeSessionId = null;
    }

}
