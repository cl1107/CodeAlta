using CodeAlta.Agent.LocalRuntime;

namespace CodeAlta.Agent.GoogleGenAI;

/// <summary>
/// Local-runtime backend for Google GenAI providers.
/// </summary>
public sealed class GoogleGenAIAgentBackend : IAgentBackend
{
    private readonly IAgentBackend _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleGenAIAgentBackend"/> class.
    /// </summary>
    /// <param name="options">The backend options.</param>
    public GoogleGenAIAgentBackend(GoogleGenAIAgentBackendOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Providers.Count == 0)
        {
            throw new ArgumentException("At least one provider registration is required.", nameof(options));
        }

        _inner = new LocalAgentBackend(
            AgentBackendIds.GoogleGenAI,
            "Google GenAI",
            new LocalAgentBackendOptions
            {
                StateRootPath = options.StateRootPath,
                Providers =
                [
                    .. options.Providers.Select(provider => new LocalAgentBackendProviderRegistration
                    {
                        Provider = new LocalAgentProviderDescriptor
                        {
                            ProtocolFamily = "google-genai",
                            ProviderKey = provider.ProviderKey.Trim(),
                            DisplayName = string.IsNullOrWhiteSpace(provider.DisplayName) ? provider.ProviderKey.Trim() : provider.DisplayName.Trim(),
                            BackendId = AgentBackendIds.GoogleGenAI,
                            TransportKind = provider.UseVertexAI ? LocalAgentTransportKind.GoogleVertexAI : LocalAgentTransportKind.GoogleGeminiApi,
                            BaseUri = provider.BaseUri,
                            IsDefault = provider.IsDefault,
                            Profile = provider.Profile ?? new LocalAgentProviderProfile
                            {
                                SupportsDeveloperRole = false,
                                SupportsReasoningEffort = true,
                                StreamsUsage = true,
                                SupportsThoughtSignatures = true,
                            },
                        },
                        TurnExecutor = new UnsupportedGoogleTurnExecutor(),
                    }),
                ],
            });
    }

    /// <inheritdoc />
    public AgentBackendId BackendId => _inner.BackendId;

    /// <inheritdoc />
    public string DisplayName => _inner.DisplayName;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default) => _inner.StartAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default) => _inner.StopAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
        => _inner.ListModelsAsync(cancellationToken);

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentSessionMetadata>> ListSessionsAsync(
        AgentSessionListFilter? filter = null,
        CancellationToken cancellationToken = default)
        => _inner.ListSessionsAsync(filter, cancellationToken);

    /// <inheritdoc />
    public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => _inner.DeleteSessionAsync(sessionId, cancellationToken);

    /// <inheritdoc />
    public Task<IAgentSession> CreateSessionAsync(
        AgentSessionCreateOptions options,
        CancellationToken cancellationToken = default)
        => _inner.CreateSessionAsync(options, cancellationToken);

    /// <inheritdoc />
    public Task<IAgentSession> ResumeSessionAsync(
        string sessionId,
        AgentSessionResumeOptions options,
        CancellationToken cancellationToken = default)
        => _inner.ResumeSessionAsync(sessionId, options, cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _inner.DisposeAsync();

    private sealed class UnsupportedGoogleTurnExecutor : ILocalAgentTurnExecutor
    {
        public Task<IReadOnlyList<AgentModelInfo>> ListModelsAsync(
            LocalAgentProviderDescriptor provider,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Google GenAI execution is not implemented yet.");

        public Task<LocalAgentTurnResponse> ExecuteTurnAsync(
            LocalAgentTurnRequest request,
            Func<LocalAgentTurnDelta, CancellationToken, ValueTask> onUpdate,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Google GenAI execution is not implemented yet.");
    }
}
