// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Fires a <see cref="BrushSelectEvent"/> when the user Shift+left-drags a rubber-band rectangle.
/// Fire-and-forget: the figure is never mutated.</summary>
public sealed class BrushSelectModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;

    private bool   _active;
    private int    _activeAxes;
    private double _startDataX;
    private double _startDataY;
    private double _startPixelX;
    private double _startPixelY;
    private double _currentPixelX;
    private double _currentPixelY;

    /// <summary>Creates a brush-select modifier for the given chart.</summary>
    public BrushSelectModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        _chartId = chartId;
        _layout  = layout;
        _sink    = sink;
    }

    /// <summary>Returns the current rubber-band rectangle state while a drag is in progress,
    /// or <c>null</c> when no brush select is active.</summary>
    public BrushSelectState? ActiveBrush =>
        _active
            ? new BrushSelectState(_startPixelX, _startPixelY, _currentPixelX, _currentPixelY, _activeAxes)
            : null;

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args) =>
        args.Button == PointerButton.Left &&
        (args.Modifiers & ModifierKeys.Shift) != 0 &&
        _layout.HitTestAxes(args.X, args.Y) is not null;

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        int axesIndex = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, axesIndex);
        if (coords is null) return;

        _active       = true;
        _activeAxes   = axesIndex;
        _startDataX   = coords.Value.DataX;
        _startDataY   = coords.Value.DataY;
        _startPixelX  = args.X;
        _startPixelY  = args.Y;
        _currentPixelX = args.X;
        _currentPixelY = args.Y;
    }

    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args)
    {
        if (!_active) return;
        _currentPixelX = args.X;
        _currentPixelY = args.Y;
    }

    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args)
    {
        if (!_active) return;
        _active = false;

        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, _activeAxes);
        if (coords is null) return;

        double endDataX = coords.Value.DataX;
        double endDataY = coords.Value.DataY;

        // Normalize so x1 < x2 and y1 < y2 regardless of drag direction.
        double x1 = Math.Min(_startDataX, endDataX);
        double x2 = Math.Max(_startDataX, endDataX);
        double y1 = Math.Min(_startDataY, endDataY);
        double y2 = Math.Max(_startDataY, endDataY);

        _sink(new BrushSelectEvent(_chartId, _activeAxes, x1, y1, x2, y2));
    }

    /// <inheritdoc />
    public bool HandlesScroll(ScrollInputArgs args) => false;
    /// <inheritdoc />
    public void OnScroll(ScrollInputArgs args) { }

    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args) { }
}
