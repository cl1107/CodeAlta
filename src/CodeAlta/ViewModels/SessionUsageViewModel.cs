using CodeAlta.Agent;
using XenoAtom.Terminal.UI;

namespace CodeAlta.ViewModels;

internal sealed partial class SessionUsageViewModel
{
    public SessionUsageViewModel()
    {
        BackendName = string.Empty;
    }

    [Bindable]
    public partial AgentSessionUsage? Usage { get; set; }

    [Bindable]
    public partial string BackendName { get; set; }

    [Bindable]
    public partial string? ModelName { get; set; }
}
