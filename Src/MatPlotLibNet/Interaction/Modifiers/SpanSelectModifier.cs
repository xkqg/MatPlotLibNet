// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Alt+left-drag selects a horizontal X-range, fires <see cref="SpanSelectEvent"/>.
/// Exposes <see cref="ActiveSpan"/> for controls to draw a full-height shaded band overlay.</summary>
public sealed class SpanSelectModifier : RubberBandModifier
{
    /// <summary>Creates a span-select modifier: opened with Alt held, a drag shorter than two pixels across selects nothing.</summary>
    public SpanSelectModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink, ModifierKeys.Alt, DragFloor.AlongX)
    {
    }

    /// <summary>Returns the span state during an Alt+drag, or <c>null</c>.</summary>
    public SpanSelectState? ActiveSpan => Band.Showing((x0, _, x1, _, axes) => new SpanSelectState(x0, x1, axes, Layout.GetPlotArea(axes)));

    /// <inheritdoc />
    private protected override void Released(Drag drag)
    {
        var box = drag.DataBox;
        Emit(new SpanSelectEvent(ChartId, drag.AxesIndex, box.XMin, box.XMax));
    }
}
