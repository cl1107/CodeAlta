using CodeAlta.Agent;
using CodeAlta.Catalog;

namespace CodeAlta.App;

internal interface IRecoverableSessionSource
{
    IAsyncEnumerable<SessionViewDescriptor> ListRecoverableSessionsAsync(CancellationToken cancellationToken);

    IAsyncEnumerable<SessionViewDescriptor> ListRecoverableSessionsAsync(
        Func<ModelProviderId, bool>? shouldListProviderSessions,
        CancellationToken cancellationToken)
        => ListRecoverableSessionsAsync(cancellationToken);
}
