using System.Collections.Generic;
using XenoAtom.Terminal.UI;
using XenoAtom.Terminal.UI.Geometry;
using XenoAtom.Terminal.UI.Layout;
using XenoAtom.Terminal.UI.Rendering;
using XenoAtom.Terminal.UI.Threading;

namespace CodeAlta.Views;

internal sealed class BindableObserver<T> : Visual
{
    private readonly Func<T> _read;
    private readonly Action<T> _onChanged;
    private readonly IEqualityComparer<T> _comparer;
    private bool _initialized;
    private T? _lastValue;

    public BindableObserver(
        Func<T> read,
        Action<T> onChanged,
        IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(onChanged);

        _read = read;
        _onChanged = onChanged;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        IsVisible = false;
        IsEnabled = false;
    }

    protected override void PrepareChildren()
    {
        var value = _read();
        if (!_initialized)
        {
            _lastValue = value;
            _initialized = true;
            return;
        }

        if (_comparer.Equals(_lastValue!, value))
        {
            return;
        }

        _lastValue = value;
        Dispatcher.Current.Post(() => _onChanged(value));
    }

    protected override SizeHints MeasureCore(in LayoutConstraints constraints)
        => SizeHints.Fixed(constraints.Clamp(new Size(0, 0)));

    protected override void ArrangeCore(in Rectangle finalRect)
    {
        _ = finalRect;
    }

    protected override void RenderOverride(CellBuffer buffer)
    {
        _ = buffer;
    }
}
