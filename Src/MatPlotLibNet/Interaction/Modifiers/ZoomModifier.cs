// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Zooms the axes range centered on the cursor position in response to scroll-wheel events. The zoom is taken on
/// screen and read through the range and scale the axes are drawn with, so the point under the pointer stays there.</summary>
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
        int axesIndex = (_layout.HitTestAxes(args.X, args.Y)).GetValueOrDefault();
        var view = _layout.ViewOf(axesIndex);
        var area = _layout.GetPlotArea(axesIndex);

        // Negative deltaY = scroll up = zoom in (reduce range).
        double scale = args.DeltaY < 0
            ? 1.0 - ZoomFactor
            : 1.0 + ZoomFactor;

        // The edges move toward (or away from) the pointer on screen, and the new edges are read through the view: the
        // point under the pointer stays there, and on a log or symlog axis every decade narrows alike.
        double left   = args.X - (args.X - area.X) * scale;
        double right  = args.X + (area.X + area.Width - args.X) * scale;
        double bottom = args.Y + (area.Y + area.Height - args.Y) * scale;
        double top    = args.Y - (args.Y - area.Y) * scale;
        var low  = view.PixelToData(new Rendering.Point(left, bottom));
        var high = view.PixelToData(new Rendering.Point(right, top));

        _sink(new ZoomEvent(_chartId, axesIndex, low.X, high.X, low.Y, high.Y)
        {
            SecondaryRanges = SecondaryRangesFor(axesIndex, left, right, top, bottom),
        });
    }

    /// <summary>Every outward-stacked secondary Y axis' new range after this scroll — the SAME pixel edges the
    /// primary zoomed to, read through each secondary axis' OWN transform instead (ROADMAP.md row 100), so a log
    /// or symlog secondary axis narrows correctly too, not just linearly.</summary>
    private List<SecondaryAxisRange>? SecondaryRangesFor(int axesIndex, double left, double right, double top, double bottom)
    {
        var views = _layout.SecondaryViewsOf(axesIndex);
        if (views.Count == 0) return null;

        var ranges = new List<SecondaryAxisRange>(views.Count);
        foreach (var v in views)
        {
            var svLow  = v.Transform.PixelToData(new Rendering.Point(left, bottom));
            var svHigh = v.Transform.PixelToData(new Rendering.Point(right, top));
            ranges.Add(new SecondaryAxisRange(v.Side, v.SlotIndex, new DataRange(svLow.X, svHigh.X, svLow.Y, svHigh.Y)));
        }

        return ranges;
    }

    /// <inheritdoc />
    public bool HandlesKeyDown(KeyInputArgs args) => false;
    /// <inheritdoc />
    public void OnKeyDown(KeyInputArgs args) { }
}
