using CodeAlta.Agent;
using CodeAlta.Orchestration.Runtime;

namespace CodeAlta.Orchestration.Tests;

[TestClass]
public sealed class AgentHubTests
{
    [TestMethod]
    public async Task UsesSharedSessionMetadataStoreAsync_UsesRegistrationMetadataWithoutStartingBackend()
    {
        var backend = new BlockingBackend("local");
        var factory = new AgentBackendFactory();
        factory.Register(
            "local",
            () => backend,
            AgentBackendRegistrationOptions.SharedSessionMetadataStore);
        await using var hub = new AgentHub(factory);

        var usesSharedStore = await hub.UsesSharedSessionMetadataStoreAsync(new AgentBackendId("local"));

        Assert.IsTrue(usesSharedStore);
        Assert.AreEqual(0, backend.StartCallCount);
    }

    private sealed class BlockingBackend(string backendId) : IAgentBackend
    {
        public TaskCompletionSource StartEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource AllowStart { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int StartCallCount { get; private set; }

        public int ListModelsCallCount { get; private set; }

        public bool Disposed { get; private set; }

        public AgentBackendId BackendId { get; } = new(backendId);

        public string DisplayName => "Blocking";

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCallCount++;
            StartEntered.TrySetResult();
            await AllowStart.Task.WaitAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<AgentModelInfo>> ListModelsAsync(CancellationToken cancellationToken = default)
        {
            ListModelsCallCount++;
            return Task.FromResult<IReadOnlyList<AgentModelInfo>>([new AgentModelInfo("test-model", "Test Model")]);
        }

        public async IAsyncEnumerable<AgentSessionMetadata> ListSessionsAsync(
            AgentSessionListFilter? filter = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            yield break;
        }

        public Task<IAgentSession> CreateSessionAsync(
            AgentSessionCreateOptions options,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IAgentSession> ResumeSessionAsync(
            string sessionId,
            AgentSessionResumeOptions options,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
