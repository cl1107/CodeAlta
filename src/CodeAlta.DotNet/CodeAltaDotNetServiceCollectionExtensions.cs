using Microsoft.Extensions.DependencyInjection;

namespace CodeAlta.DotNet;

/// <summary>
/// Extension methods for registering .NET first-class services.
/// </summary>
public static class CodeAltaDotNetServiceCollectionExtensions
{
    /// <summary>
    /// Registers core .NET workspace/symbol/diagnostics services.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configure">Optional options callback.</param>
    /// <returns><paramref name="services"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddCodeAltaDotNet(
        this IServiceCollection services,
        Action<DotNetOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DotNetOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<DotNetWorkspaceService>();
        services.AddSingleton<SymbolIndexService>();
        services.AddSingleton<DotNetContextProvider>();
        services.AddSingleton<DotNetDiagnosticsService>();
        services.AddSingleton<DotNetIndexService>();
        return services;
    }
}
