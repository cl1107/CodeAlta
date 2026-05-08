using XenoAtom.Terminal.UI;

namespace CodeAlta.Views;

internal sealed record ThreadWorkspaceChromeController(
    Func<Visual> BuildSessionUsageIndicatorVisual,
    Action<Visual> ToggleThreadInfoPopup,
    Action OpenModelProviders)
{
    public static ThreadWorkspaceChromeController Create(
        Func<Visual> buildSessionUsageIndicatorVisual,
        Action<Visual> toggleThreadInfoPopup,
        Action openModelProviders)
    {
        ArgumentNullException.ThrowIfNull(buildSessionUsageIndicatorVisual);
        ArgumentNullException.ThrowIfNull(toggleThreadInfoPopup);
        ArgumentNullException.ThrowIfNull(openModelProviders);
        return new ThreadWorkspaceChromeController(buildSessionUsageIndicatorVisual, toggleThreadInfoPopup, openModelProviders);
    }

    public static ThreadWorkspaceChromeController Empty { get; } = new(
        static () => new XenoAtom.Terminal.UI.Controls.TextBlock(string.Empty),
        static _ => { },
        static () => { });
}
