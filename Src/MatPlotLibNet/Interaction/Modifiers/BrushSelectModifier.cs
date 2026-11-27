// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Fires a <see cref="BrushSelectEvent"/> when the user Shift+left-drags a rubber-band rectangle.
/// Fire-and-forget: the figure is never mutated.</summary>
public sealed class BrushSelectModifier : RubberBandModifier
{
    /// <summary>Creates a brush-select modifier for the given chart: opened with Shift held; every drag selects, however short.</summary>
    public BrushSelectModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink, ModifierKeys.Shift, DragFloor.None)
    {
    }

    /// <summary>Returns the current rubber-band rectangle state while a drag is in progress,
    /// or <c>null</c> when no brush select is active.</summary>
    public BrushSelectState? ActiveBrush =>
        Band.Showing(static (x0, y0, x1, y1, axes) => new BrushSelectState(x0, y0, x1, y1, axes));

    /// <inheritdoc />
    /// <remarks>The selected box is normalised, low ends first, whichever way the pointer was dragged.</remarks>
    private protected override void Released(Drag drag)
    {
        var box = drag.DataBox;
        Emit(new BrushSelectEvent(ChartId, drag.AxesIndex, box.XMin, box.YMin, box.XMax, box.YMax));
    }
}
