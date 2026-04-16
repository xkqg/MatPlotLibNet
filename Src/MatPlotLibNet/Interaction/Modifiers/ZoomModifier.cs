// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Zooms the axes range centered on the cursor position in response to scroll-wheel events.</summary>
public sealed class ZoomModifier : IInteractionModifier
{
    private const double ZoomFactor = 0.15; // 15 % zoom per scroll notch

    private readonly string _chartId;
    private readonly IChartLayout _layout;
    private readonly Action<FigureInteractionEvent> _sink;

    /// <summary>Creates a zoom modifier for the given chart.</summary>
    public ZoomModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
    {
        _chartId = chartId;
        _layout  = layout;
        _sink    = sink;
    }

    /// <inheritdoc />
    public bool HandlesPointerPressed(PointerInputArgs args) => false;
    /// <inheritdoc />
    public void OnPointerPressed(PointerInputArgs args) { }
    /// <inheritdoc />
    public void OnPointerMoved(PointerInputArgs args) { }
    /// <inheritdoc />
    public void OnPointerReleased(PointerInputArgs args) { }

    /// <inheritdoc />
    public bool HandlesScroll(ScrollInputArgs args) =>
        _layout.HitTestAxes(args.X, args.Y) is not null;

    /// <inheritdoc />
    public void OnScroll(ScrollInputArgs args)
    {
        int axesIndex = _layout.HitTestAxes(args.X, args.Y) ?? 0;
        var coords = ((ChartLayout)_layout).PixelToData(args.X, args.Y, axesIndex);
        if (coords is null) return;

        var (xMin, xMax, yMin, yMax) = _layout.GetDataRange(axesIndex);

        // Negative deltaY = scroll up = zoom in (reduce range).
        double scale = args.DeltaY < 0
            ? 1.0 - ZoomFactor
            : 1.0 + ZoomFactor;

        double cx = coords.Value.DataX;
        double cy = coords.Value.DataY;

        double newXMin = cx - (cx - xMin) * scale;
        double newXMax = cx + (xMax - cx) * scale;
        double newYMin = cy - (cy - yMin) * scale;
        double newYMax = cy + (yMax - cy) * scale;

        _sink(new ZoomEvent(_chartId, axesIndex, newXMin, newXMax, newYMin, newYMax));
    }

    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args) { }
}
