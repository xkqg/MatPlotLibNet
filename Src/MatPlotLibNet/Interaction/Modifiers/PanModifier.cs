// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Translates the visible axes range when the user drags with the left mouse button
/// (without Shift, which is claimed by <see cref="BrushSelectModifier"/>).</summary>
public sealed class PanModifier : IInteractionModifier
{
    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;

    private bool   _active;
    private int    _activeAxes;
    /// <summary>Where the pointer was when the last pan was sent (or the button went down): the next move pans by the
    /// distance from here.</summary>
    private double _lastPixelX;
    private double _lastPixelY;

    /// <summary>Creates a pan modifier for the given chart.</summary>
    public PanModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        _chartId = chartId;
        _layout  = layout;
        _sink    = sink;
    }

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args) =>
        args.Button == PointerButton.Left &&
        (args.Modifiers & ModifierKeys.Shift) == 0 &&
        _layout.HitTestAxes(args.X, args.Y) is not null;

    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args)
    {
        int axesIndex = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, axesIndex);
        if (coords is null) return;

        _active     = true;
        _activeAxes = axesIndex;
        _lastPixelX = args.X;
        _lastPixelY = args.Y;
    }

    /// <inheritdoc />
    /// <remarks>The pointer's move on screen becomes a shift of the range in the space each axis is drawn in (data
    /// units on a linear axis, decades on a log axis), the unit <see cref="PanEvent"/> takes: the point that was under
    /// the pointer moves with it, on every scale. Each move sends the distance since the last one, so a move that
    /// arrives before the control has repainted still pans by exactly that move.</remarks>
    public void OnPointerMoved(PointerInputArgs args)
    {
        if (!_active) return;

        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, _activeAxes);
        if (coords is null) return;

        var view = _layout.ViewOf(_activeAxes);
        var area = _layout.GetPlotArea(_activeAxes);
        // Drawn units per pixel; negative on a reversed axis, so the shift still follows the pointer.
        double dx = (_lastPixelX - args.X) / area.Width * (view.DataXMax - view.DataXMin);
        double dy = (args.Y - _lastPixelY) / area.Height * (view.DataYMax - view.DataYMin);

        if (dx == 0 && dy == 0) return;

        _sink(new PanEvent(_chartId, _activeAxes, dx, dy) { InView = _layout.GetDataRange(_activeAxes) });

        _lastPixelX = args.X;
        _lastPixelY = args.Y;
    }

    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args) => _active = false;

    /// <inheritdoc />
    public bool HandlesScroll(ScrollInputArgs args) => false;
    /// <inheritdoc />
    public void OnScroll(ScrollInputArgs args) { }

    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args) { }
}
