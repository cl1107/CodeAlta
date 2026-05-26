using CodeAlta.Agent;
using CodeAlta.Plugins;

namespace CodeAlta.Orchestration.Hosting;

/// <summary>
/// Registers plugin-contributed agent backends in shared host composition.
/// </summary>
public static class CodeAltaHostPluginBackendRegistrar
{
    /// <summary>
    /// Registers applicable plugin-contributed backends.
    /// </summary>
    /// <param name="backendFactory">The backend factory to update.</param>
    /// <param name="modelProviderRegistry">The model provider registry to update.</param>
    /// <param name="pluginRuntime">The active plugin runtime.</param>
    /// <param name="options">Plugin operation options used for applicability and backend creation.</param>
    /// <returns>Descriptors for plugin-contributed backends registered by this call.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="backendFactory"/>, <paramref name="pluginRuntime"/>, or <paramref name="options"/>
    /// is <see langword="null"/>.
    /// </exception>
    public static IReadOnlyList<ModelProviderDescriptor> RegisterPluginBackends(
        AgentBackendFactory backendFactory,
        ModelProviderRegistry modelProviderRegistry,
        PluginRuntimeManager pluginRuntime,
        PluginAdapterOperationOptions options)
    {
        ArgumentNullException.ThrowIfNull(backendFactory);
        ArgumentNullException.ThrowIfNull(modelProviderRegistry);
        ArgumentNullException.ThrowIfNull(pluginRuntime);
        ArgumentNullException.ThrowIfNull(options);

        var descriptors = new List<ModelProviderDescriptor>();
        foreach (var pluginBackend in pluginRuntime.Adapter.GetAgentBackends(options))
        {
            var backendId = new AgentBackendId(pluginBackend.Name);
            IAgentBackend CreateBackend() => pluginRuntime.Adapter.CreateAgentBackendAsync(pluginRuntime.ActivePlugins, pluginBackend.Name, options, CancellationToken.None)
                .AsTask()
                .GetAwaiter()
                .GetResult()
                .Backend
                ?? throw new InvalidOperationException($"Plugin backend '{pluginBackend.Name}' did not create a backend instance.");

            var descriptor = new ModelProviderDescriptor(backendId, pluginBackend.DisplayName ?? pluginBackend.Name);
            backendFactory.RegisterOrReplace(backendId, CreateBackend);
            modelProviderRegistry.RegisterOrReplaceBackendRuntime(descriptor, CreateBackend);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }
}
