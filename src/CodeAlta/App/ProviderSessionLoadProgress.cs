using CodeAlta.Agent;

namespace CodeAlta.App;

internal sealed record ProviderSessionLoadProgress(
    AgentBackendId BackendId,
    string ProviderDisplayName,
    int CompletedProviderCount,
    int TotalProviderCount,
    IReadOnlyList<string> LoadingProviderDisplayNames);

internal interface IKnownProjectImporterWithProgress : IKnownProjectImporter
{
    Task ImportAsync(Action<ProviderSessionLoadProgress> reportProgress, CancellationToken cancellationToken);
}
