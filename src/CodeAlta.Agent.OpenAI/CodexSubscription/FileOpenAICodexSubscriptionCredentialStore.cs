using System.Text;
using System.Text.Json;

namespace CodeAlta.Agent.OpenAI.CodexSubscription;

internal sealed class FileOpenAICodexSubscriptionCredentialStore : IOpenAICodexSubscriptionCredentialStore
{
    private readonly string _rootPath;

    public FileOpenAICodexSubscriptionCredentialStore(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _rootPath = rootPath;
    }

    public async ValueTask<OpenAICodexSubscriptionCredential?> LoadAsync(
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        var path = GetCredentialPath(providerKey);
        if (!File.Exists(path))
        {
            return null;
        }

        var protectedBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        var jsonBytes = Unprotect(protectedBytes);
        return JsonSerializer.Deserialize(
            jsonBytes,
            OpenAICodexSubscriptionJsonSerializerContext.Default.OpenAICodexSubscriptionCredential);
    }

    public async ValueTask SaveAsync(
        string providerKey,
        OpenAICodexSubscriptionCredential credential,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        ArgumentNullException.ThrowIfNull(credential);

        var path = GetCredentialPath(providerKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(
            credential,
            OpenAICodexSubscriptionJsonSerializerContext.Default.OpenAICodexSubscriptionCredential);
        var protectedBytes = Protect(jsonBytes);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await File.WriteAllBytesAsync(temporaryPath, protectedBytes, cancellationToken).ConfigureAwait(false);
        TryRestrictFilePermissions(temporaryPath);
        File.Move(temporaryPath, path, overwrite: true);
        TryRestrictFilePermissions(path);
    }

    public ValueTask DeleteAsync(
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetCredentialPath(providerKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return ValueTask.CompletedTask;
    }

    private string GetCredentialPath(string providerKey)
    {
        var safeProviderKey = string.Concat(providerKey.Trim().Select(static ch =>
            char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.' ? ch : '_'));
        return Path.Combine(_rootPath, "auth", "openai-codex-subscription", safeProviderKey + ".credential");
    }

    private static byte[] Protect(byte[] bytes)
        => Encoding.UTF8.GetBytes(Convert.ToBase64String(bytes));

    private static byte[] Unprotect(byte[] bytes)
        => Convert.FromBase64String(Encoding.UTF8.GetString(bytes));

    private static void TryRestrictFilePermissions(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (PlatformNotSupportedException)
        {
        }
    }
}
