using CodeAlta.Frontend.Commands;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Controls;

namespace CodeAlta.Tests;

[TestClass]
public sealed class ShellCommandSurfaceCoordinatorTests
{
    [TestMethod]
    public void CommandPalettePopupStyle_IsBottomCenteredHalfWidth()
    {
        var style = ShellCommandSurfaceCoordinator.CommandPalettePopupStyle;

        Assert.AreEqual(50d, style.PopupWidthPercent.GetValueOrDefault());
        Assert.AreEqual(int.MaxValue, style.MaxWidth);
        Assert.AreEqual(Align.Center, style.PopupHorizontalAlignment);
        Assert.AreEqual(Align.End, style.PopupVerticalAlignment);
        Assert.AreEqual(-2, style.PopupOffsetY);
    }

    [TestMethod]
    public void ConfigureCommandPaletteForShow_SetsSlashQueryWithoutClearing()
    {
        var palette = new CommandPalette();

        ShellCommandSurfaceCoordinator.ConfigureCommandPaletteForShow(
            palette,
            ShellCommandSurfaceCoordinator.SlashCommandPaletteQuery);

        Assert.AreEqual(ShellCommandSurfaceCoordinator.SlashCommandPaletteQuery, palette.QueryText);
        Assert.IsFalse(palette.ClearQueryOnShow);

        ShellCommandSurfaceCoordinator.ConfigureCommandPaletteForShow(palette, initialQuery: null);

        Assert.IsTrue(palette.ClearQueryOnShow);
    }
}
