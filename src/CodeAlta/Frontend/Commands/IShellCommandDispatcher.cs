namespace CodeAlta.Frontend.Commands;

internal interface IShellCommandDispatcher
{
    ValueTask DispatchAsync(ShellCommand command, CancellationToken cancellationToken = default);
}
