// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Zooms the axes range centered on the cursor position in response to scroll-wheel events. The zoom is taken on
/// screen and read through the range and scale the axes are drawn with, so the point under the pointer stays there.</summary>
public sealed class ZoomModifier : InteractionModifier
{
    /// <summary>Creates a zoom modifier for the given chart.</summary>
    public ZoomModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink)
    {
    }

    /// <inheritdoc />
    public override bool HandlesScroll(ScrollInputArgs args) =>
        Layout.IsOnAxes(args.X, args.Y);

    /// <inheritdoc />
    public override void OnScroll(ScrollInputArgs args)
    {
        int axesIndex = Layout.AxesOrFirstAt(args.X, args.Y);
        var view = Layout.ViewOf(axesIndex);

        // One notch of the wheel, by matplotlib's factor: in when it turns up (a negative delta), out otherwise.
        double scale = ZoomNotch.ScaleOf(args.DeltaY);

        // The edges move toward (or away from) the pointer on screen, read through the view (DataTransform.ZoomedAbout, which takes
        // an axis drawn in parts in the space of its parts): the point under the pointer stays there, and on a log or symlog axis every
        // decade narrows alike.
        var pointer = new Point(args.X, args.Y);
        var zoomed = view.ZoomedAbout(pointer, scale);

        Emit(new ZoomEvent(ChartId, axesIndex, zoomed.XMin, zoomed.XMax, zoomed.YMin, zoomed.YMax)
        {
            // Every Y axis stacked past the primary is zoomed about the same pointer, by its own transform.
            SecondaryRanges = Layout.SecondaryRangesZoomedAbout(axesIndex, pointer, scale),
        });
    }
}
