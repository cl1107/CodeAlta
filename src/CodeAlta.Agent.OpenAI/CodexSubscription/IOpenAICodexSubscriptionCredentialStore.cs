namespace CodeAlta.Agent.OpenAI.CodexSubscription;

internal interface IOpenAICodexSubscriptionCredentialStore
{
    ValueTask<OpenAICodexSubscriptionCredential?> LoadAsync(
        string providerKey,
        CancellationToken cancellationToken = default);

    ValueTask SaveAsync(
        string providerKey,
        OpenAICodexSubscriptionCredential credential,
        CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(
        string providerKey,
        CancellationToken cancellationToken = default);
}
