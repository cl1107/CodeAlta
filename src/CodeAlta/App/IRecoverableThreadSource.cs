using CodeAlta.Agent;
using CodeAlta.Catalog;

namespace CodeAlta.App;

internal interface IRecoverableThreadSource
{
    IAsyncEnumerable<WorkThreadDescriptor> StreamRecoverableThreadsAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<WorkThreadDescriptor> StreamRecoverableThreadsAsync(
        Func<AgentBackendId, bool>? shouldListBackendSessions,
        CancellationToken cancellationToken)
        => StreamRecoverableThreadsAsync(cancellationToken);

    Task<IReadOnlyList<WorkThreadDescriptor>> ListRecoverableThreadsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkThreadDescriptor>> ListRecoverableThreadsAsync(
        Func<AgentBackendId, bool>? shouldListBackendSessions,
        CancellationToken cancellationToken)
        => ListRecoverableThreadsAsync(cancellationToken);
}
