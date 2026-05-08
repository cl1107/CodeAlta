using CodeAlta.Agent;
using CodeAlta.Catalog;
using CodeAlta.Views;
using XenoAtom.Logging;

namespace CodeAlta.App;

internal interface IPluginAgentEventObserver
{
    Task ObserveAgentEventAsync(WorkThreadDescriptor thread, AgentEvent agentEvent, CancellationToken cancellationToken = default);
}

internal sealed class PluginAgentEventObserver : IPluginAgentEventObserver
{
    private readonly PluginHostBridge? _pluginHostBridge;

    public PluginAgentEventObserver(PluginHostBridge? pluginHostBridge)
        => _pluginHostBridge = pluginHostBridge;

    public async Task ObserveAgentEventAsync(WorkThreadDescriptor thread, AgentEvent agentEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(thread);
        ArgumentNullException.ThrowIfNull(agentEvent);

        if (_pluginHostBridge is null)
        {
            return;
        }

        try
        {
            await _pluginHostBridge.ObserveAgentEventAsync(thread, agentEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            if (LogManager.IsInitialized && CodeAltaApp.UiLogger.IsEnabled(LogLevel.Error))
            {
                CodeAltaApp.UiLogger.Error(ex, $"Plugin agent event observer failed for thread {thread.ThreadId}");
            }
        }
    }
}
