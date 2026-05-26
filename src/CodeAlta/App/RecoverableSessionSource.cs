using CodeAlta.Agent;
using CodeAlta.Catalog;
using CodeAlta.Orchestration.Runtime;

namespace CodeAlta.App;

internal sealed class RecoverableSessionSource : IRecoverableSessionSource
{
    private readonly SessionRuntimeService _runtimeService;

    public RecoverableSessionSource(SessionRuntimeService runtimeService)
    {
        ArgumentNullException.ThrowIfNull(runtimeService);
        _runtimeService = runtimeService;
    }

    public Func<ModelProviderId, bool>? ShouldListProviderSessions { get; set; }

    public IAsyncEnumerable<SessionViewDescriptor> ListRecoverableSessionsAsync(CancellationToken cancellationToken)
        => _runtimeService.ListRecoverableSessionsAsync(
            providerId => ShouldListProviderSessions?.Invoke(providerId) ?? true,
            cancellationToken);

    public IAsyncEnumerable<SessionViewDescriptor> ListRecoverableSessionsAsync(
        Func<ModelProviderId, bool>? shouldListProviderSessions,
        CancellationToken cancellationToken)
        => _runtimeService.ListRecoverableSessionsAsync(
            providerId => (ShouldListProviderSessions?.Invoke(providerId) ?? true) &&
                          (shouldListProviderSessions?.Invoke(providerId) ?? true),
            cancellationToken);
}
