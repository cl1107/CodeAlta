namespace CodeAlta.LiveTool;

/// <summary>
/// Determines whether the host can inject the <c>alta</c> agent tool into sessions for a provider/runtime id.
/// </summary>
public interface IAltaSessionToolProviderPolicy
{
    /// <summary>
    /// Returns <see langword="true"/> when the provider supports host-injected <c>alta</c> tools.
    /// </summary>
    /// <param name="providerId">The model provider identifier.</param>
    /// <returns><see langword="true"/> when the live tool may be exposed to the provider.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="providerId"/> is empty.</exception>
    bool SupportsAltaSessionTool(string providerId);
}

/// <summary>
/// Static provider policy for hosts that know their dynamically registered provider capabilities.
/// </summary>
public sealed class AltaSessionToolProviderPolicy : IAltaSessionToolProviderPolicy
{
    private readonly HashSet<string> _providerIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="AltaSessionToolProviderPolicy"/> class.
    /// </summary>
    /// <param name="providerIds">Model provider identifiers that support host-injected <c>alta</c> tools.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="providerIds"/> is <see langword="null"/>.</exception>
    public AltaSessionToolProviderPolicy(IEnumerable<string> providerIds)
    {
        ArgumentNullException.ThrowIfNull(providerIds);
        _providerIds = new HashSet<string>(providerIds.Where(static id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public bool SupportsAltaSessionTool(string providerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        return _providerIds.Contains(providerId);
    }
}
