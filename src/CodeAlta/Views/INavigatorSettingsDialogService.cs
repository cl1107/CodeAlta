using CodeAlta.Catalog;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Geometry;

namespace CodeAlta.Views;

internal interface INavigatorSettingsDialogService
{
    Rectangle? GetDialogBounds();

    Visual? GetDialogFocusTarget();

    Task SaveNavigatorSettingsAsync(NavigatorSettings settings);
}
