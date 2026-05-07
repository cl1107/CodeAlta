namespace CodeAlta.Frontend.Commands;

internal sealed class ShellCommandRegistry
{
    private readonly Dictionary<Type, Func<ShellCommand, CancellationToken, ValueTask>> _handlers = new();

    public void Register<TCommand>(Func<TCommand, CancellationToken, ValueTask> handler)
        where TCommand : ShellCommand
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers[typeof(TCommand)] = (command, cancellationToken) => handler((TCommand)command, cancellationToken);
    }

    public bool TryGetHandler(ShellCommand command, out Func<ShellCommand, CancellationToken, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(command);
        return _handlers.TryGetValue(command.GetType(), out handler!);
    }
}
