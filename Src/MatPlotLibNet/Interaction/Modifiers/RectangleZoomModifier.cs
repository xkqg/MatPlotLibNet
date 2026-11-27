// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Interaction;

/// <summary>Ctrl+left-drag draws a zoom rectangle; on release, zooms the axes to that rectangle.
/// Exposes <see cref="ActiveZoomRect"/> for overlay rendering by platform controls.</summary>
public sealed class RectangleZoomModifier : RubberBandModifier
{
    /// <summary>Creates a rectangle-zoom modifier: opened with Ctrl held, a drag shorter than two pixels both ways does not zoom.</summary>
    public RectangleZoomModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink, ModifierKeys.Ctrl, DragFloor.Box)
    {
    }

    /// <summary>Returns the current zoom rectangle while a Ctrl+drag is in progress, or <c>null</c>.</summary>
    public RectangleZoomState? ActiveZoomRect =>
        Band.Showing(static (x0, y0, x1, y1, axes) => new RectangleZoomState(x0, y0, x1, y1, axes));

    /// <inheritdoc />
    /// <remarks>Every Y axis stacked past the primary zooms to the SAME two pixel corners the primary zoomed to, read through that
    /// axis' own transform.</remarks>
    private protected override void Released(Drag drag)
    {
        var box = drag.DataBox;
        Emit(new RectangleZoomEvent(ChartId, drag.AxesIndex, box.XMin, box.XMax, box.YMin, box.YMax)
        {
            SecondaryRanges = Layout.SecondaryRangesBetween(drag.AxesIndex, drag.StartPixel, drag.EndPixel, DataRange.Between),
        });
    }
}
