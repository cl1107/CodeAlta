using CodeAlta.Threading;

namespace CodeAlta.Frontend.Commands;

internal sealed class ShellCommandDispatcher : IShellCommandDispatcher
{
    private readonly ShellCommandRegistry _registry;
    private readonly IUiDispatcher? _uiDispatcher;

    public ShellCommandDispatcher(ShellCommandRegistry registry, IUiDispatcher? uiDispatcher = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
        _uiDispatcher = uiDispatcher;
    }

    public async ValueTask DispatchAsync(ShellCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (_uiDispatcher is not null && !_uiDispatcher.CheckAccess())
        {
            var task = await _uiDispatcher.InvokeAsync(() => DispatchCoreAsync(command, cancellationToken).AsTask());
            await task;
            return;
        }

        await DispatchCoreAsync(command, cancellationToken);
    }

    private ValueTask DispatchCoreAsync(ShellCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_registry.TryGetHandler(command, out var handler))
        {
            throw new InvalidOperationException($"No shell command handler is registered for {command.GetType().Name}.");
        }

        return handler(command, cancellationToken);
    }
}
