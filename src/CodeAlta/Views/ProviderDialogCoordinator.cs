using CodeAlta.App;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Geometry;

namespace CodeAlta.Views;

internal sealed class ProviderDialogCoordinator
{
    private readonly ProviderFrontendCoordinator _providerUi;
    private readonly Func<Rectangle?> _getBounds;
    private readonly Func<Visual?> _getFocusTarget;
    private ModelProvidersDialog? _dialog;

    public ProviderDialogCoordinator(
        ProviderFrontendCoordinator providerUi,
        Func<Rectangle?> getBounds,
        Func<Visual?> getFocusTarget)
    {
        ArgumentNullException.ThrowIfNull(providerUi);
        ArgumentNullException.ThrowIfNull(getBounds);
        ArgumentNullException.ThrowIfNull(getFocusTarget);

        _providerUi = providerUi;
        _getBounds = getBounds;
        _getFocusTarget = getFocusTarget;
    }

    public Task OpenAsync()
    {
        (_dialog ??= new ModelProvidersDialog(
            () => _providerUi.LoadProviderDefinitions(),
            definitions => _providerUi.SaveProviderDefinitionsAsync(definitions, CancellationToken.None),
            definition => _providerUi.TestProviderAsync(definition, CancellationToken.None),
            _getBounds,
            _getFocusTarget)).Show();
        return Task.CompletedTask;
    }
}
