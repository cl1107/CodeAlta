using CodeAlta.App.State;
using CodeAlta.Models;
using CodeAlta.Threading;

namespace CodeAlta.App;

internal readonly record struct ShellStatusUpdate(string Message, bool ShowSpinner, StatusTone Tone);

internal readonly record struct ThreadStatusUpdate(string Message, bool ShowSpinner, StatusTone Tone);

internal interface IShellStatusPort
{
    void SetShellStatus(ShellStatusUpdate update);

    void SetThreadStatus(OpenThreadState thread, ThreadStatusUpdate update);

    void ClearThreadStatus(OpenThreadState thread);

    void SetProviderSessionLoadStatus(string? message);
}

internal sealed class ShellStatusPort : IShellStatusPort
{
    private readonly IUiDispatcher _uiDispatcher;
    private readonly Action<string, bool, StatusTone> _setShellStatus;
    private readonly Action<OpenThreadState, string, bool, StatusTone> _setThreadStatus;
    private readonly Action<OpenThreadState> _clearThreadStatus;
    private readonly Action<string?> _setProviderSessionLoadStatus;

    public ShellStatusPort(
        IUiDispatcher uiDispatcher,
        Action<string, bool, StatusTone> setShellStatus,
        Action<OpenThreadState, string, bool, StatusTone> setThreadStatus,
        Action<OpenThreadState>? clearThreadStatus = null,
        Action<string?>? setProviderSessionLoadStatus = null)
    {
        ArgumentNullException.ThrowIfNull(uiDispatcher);
        ArgumentNullException.ThrowIfNull(setShellStatus);
        ArgumentNullException.ThrowIfNull(setThreadStatus);

        _uiDispatcher = uiDispatcher;
        _setShellStatus = setShellStatus;
        _setThreadStatus = setThreadStatus;
        _clearThreadStatus = clearThreadStatus ?? (_ => { });
        _setProviderSessionLoadStatus = setProviderSessionLoadStatus ?? (_ => { });
    }

    public void SetShellStatus(ShellStatusUpdate update)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(update.Message);
        _uiDispatcher.Invoke(() => _setShellStatus(update.Message, update.ShowSpinner, update.Tone));
    }

    public void SetThreadStatus(OpenThreadState thread, ThreadStatusUpdate update)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentException.ThrowIfNullOrWhiteSpace(update.Message);
        _uiDispatcher.Invoke(() => _setThreadStatus(thread, update.Message, update.ShowSpinner, update.Tone));
    }

    public void ClearThreadStatus(OpenThreadState thread)
    {
        ArgumentNullException.ThrowIfNull(thread);
        _uiDispatcher.Invoke(() => _clearThreadStatus(thread));
    }

    public void SetProviderSessionLoadStatus(string? message)
        => _uiDispatcher.Invoke(() => _setProviderSessionLoadStatus(message));
}
