using CodeAlta.Agent;
using CodeAlta.Agent.Runtime;
using CodeAlta.Catalog;

namespace CodeAlta.Tests;

[TestClass]
public sealed class SessionViewJournalStoreTests
{
    [TestMethod]
    public async Task ListHeadersAsync_RetriesWhenJournalIsTemporarilyLocked()
    {
        using var temp = TestTempDirectory.Create();
        var options = new CatalogOptions { GlobalRoot = temp.Path };
        var store = new SessionViewJournalStore(options);
        var createdAt = DateTimeOffset.Parse("2026-07-12T10:00:00+00:00");
        var session = new SessionViewDescriptor
        {
            SessionId = "session-sharing-violation",
            Kind = SessionViewKind.GlobalSession,
            ProviderId = ModelProviderIds.OpenAIResponses.Value,
            ProviderKey = ModelProviderIds.OpenAIResponses.Value,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            LastActiveAt = createdAt,
        };
        await store.EnsureHeaderAsync(session).ConfigureAwait(false);
        var path = new AgentRuntimePathLayout(temp.Path).GetSessionFilePath(session.SessionId, session.CreatedAt);

        await using var exclusiveStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None, bufferSize: 4096, useAsync: true);
        var listTask = store.ListHeadersAsync();

        await Task.Delay(100).ConfigureAwait(false);
        Assert.IsFalse(listTask.IsCompleted);

        await exclusiveStream.DisposeAsync().ConfigureAwait(false);
        var headers = await listTask.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        Assert.AreEqual(session.SessionId, headers.Single().SessionId);
    }
}
