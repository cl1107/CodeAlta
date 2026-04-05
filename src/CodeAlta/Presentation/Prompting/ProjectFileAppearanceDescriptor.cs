using XenoAtom.Terminal.UI;

namespace CodeAlta.Presentation.Prompting;

internal sealed record ProjectFileAppearanceDescriptor(
    string Icon,
    Color Foreground,
    string? Category = null);
