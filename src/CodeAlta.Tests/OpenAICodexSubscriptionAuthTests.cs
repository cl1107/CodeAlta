using CodeAlta.Agent.OpenAI.CodexSubscription;

namespace CodeAlta.Tests;

[TestClass]
public sealed class OpenAICodexSubscriptionAuthTests
{
    [TestMethod]
    public async Task FileCredentialStore_RoundTripsCredentialAndDeletes()
    {
        using var temp = TempDirectory.Create();
        var store = new FileOpenAICodexSubscriptionCredentialStore(temp.Path);
        var credential = new OpenAICodexSubscriptionCredential
        {
            AccessToken = "access-secret",
            RefreshToken = "refresh-secret",
            IdToken = "id-secret",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            AccountId = "acct_123",
            AccountLabel = "Workspace",
            IsFedRamp = true,
            Scopes = ["openid", "offline_access"],
        };

        await store.SaveAsync("codex/subscription", credential).ConfigureAwait(false);
        var loaded = await store.LoadAsync("codex/subscription").ConfigureAwait(false);

        Assert.IsNotNull(loaded);
        Assert.AreEqual("access-secret", loaded!.AccessToken);
        Assert.AreEqual("refresh-secret", loaded.RefreshToken);
        Assert.AreEqual("id-secret", loaded.IdToken);
        Assert.AreEqual("acct_123", loaded.AccountId);
        Assert.AreEqual("Workspace", loaded.AccountLabel);
        Assert.IsTrue(loaded.IsFedRamp);
        CollectionAssert.AreEqual(new[] { "openid", "offline_access" }, loaded.Scopes);

        await store.DeleteAsync("codex/subscription").ConfigureAwait(false);
        Assert.IsNull(await store.LoadAsync("codex/subscription").ConfigureAwait(false));
    }

    [TestMethod]
    public void SecretRedactor_RedactsCredentialSecretsAndBearerTokens()
    {
        var credential = new OpenAICodexSubscriptionCredential
        {
            AccessToken = "access-secret",
            RefreshToken = "refresh-secret",
            IdToken = "id-secret",
        };

        var redacted = OpenAICodexSubscriptionSecretRedactor.Redact(
            "Authorization: Bearer access-secret refresh-secret id-secret",
            credential);

        Assert.IsFalse(redacted.Contains("access-secret", StringComparison.Ordinal));
        Assert.IsFalse(redacted.Contains("refresh-secret", StringComparison.Ordinal));
        Assert.IsFalse(redacted.Contains("id-secret", StringComparison.Ordinal));
        StringAssert.Contains(redacted, OpenAICodexSubscriptionSecretRedactor.Redacted);
    }

    private sealed class TempDirectory(string path) : IDisposable
    {
        public string Path { get; } = path;

        public static TempDirectory Create()
        {
            var path = System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "openai-codex-subscription-auth-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TempDirectory(path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
