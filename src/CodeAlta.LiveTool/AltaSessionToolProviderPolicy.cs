namespace CodeAlta.LiveTool;

/// <summary>
/// Determines whether the host can inject the <c>alta</c> agent tool into sessions for a provider/runtime id.
/// </summary>
public interface IAltaSessionToolProviderPolicy
{
    /// <summary>
    /// Returns <see langword="true"/> when the host may expose the <c>alta</c> live tool to the provider.
    /// </summary>
    /// <param name="providerId">The model provider identifier.</param>
    /// <returns><see langword="true"/> when the live tool may be exposed to the provider.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="providerId"/> is empty.</exception>
    bool SupportsAltaSessionTool(string providerId);
}

/// <summary>
/// Provider-agnostic policy that exposes the <c>alta</c> live tool to every configured provider.
/// </summary>
public sealed class AltaSessionToolProviderPolicy : IAltaSessionToolProviderPolicy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AltaSessionToolProviderPolicy"/> class.
    /// </summary>
    public AltaSessionToolProviderPolicy()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AltaSessionToolProviderPolicy"/> class.
    /// </summary>
    /// <param name="providerIds">Legacy provider identifiers. The set is accepted for compatibility and no longer restricts tool exposure.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="providerIds"/> is <see langword="null"/>.</exception>
    public AltaSessionToolProviderPolicy(IEnumerable<string> providerIds)
    {
        ArgumentNullException.ThrowIfNull(providerIds);
    }

    /// <inheritdoc />
    public bool SupportsAltaSessionTool(string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        return true;
    }
}
