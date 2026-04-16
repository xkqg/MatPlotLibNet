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
    private double _startPixelX;
    private double _startPixelY;
    private double _startDataX;
    private double _startDataY;

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

        _active      = true;
        _activeAxes  = axesIndex;
        _startPixelX = args.X;
        _startPixelY = args.Y;
        _startDataX  = coords.Value.DataX;
        _startDataY  = coords.Value.DataY;
    }

    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args)
    {
        if (!_active) return;

        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, _activeAxes);
        if (coords is null) return;

        // Delta in data space: how far the cursor moved since press.
        // We want to drag the data so the original point stays under cursor →
        // shift axes by minus the delta.
        double dx = _startDataX - coords.Value.DataX;
        double dy = _startDataY - coords.Value.DataY;

        if (dx == 0 && dy == 0) return;

        _sink(new PanEvent(_chartId, _activeAxes, dx, dy));

        // Update start so each move emits an incremental delta, not cumulative.
        _startPixelX = args.X;
        _startPixelY = args.Y;
        _startDataX  = coords.Value.DataX + dx;  // re-anchor to new position
        _startDataY  = coords.Value.DataY + dy;
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
