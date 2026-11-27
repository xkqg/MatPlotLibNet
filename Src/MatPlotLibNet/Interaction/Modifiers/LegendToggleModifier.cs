// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Fires a <see cref="LegendToggleEvent"/> when the user clicks a legend entry.
/// Relies on <see cref="IChartLayout.HitTestLegendItem"/> to determine which legend item was
/// clicked based on the pixel-space bounds computed by the renderer.</summary>
public sealed class LegendToggleModifier : InteractionModifier
{
    /// <summary>Creates a legend-toggle modifier for the given chart.</summary>
    public LegendToggleModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink)
    {
    }

    /// <inheritdoc />
    public override bool HandlesPointerPressed(PointerInputArgs args) =>
        Layout.LeftPressAxes(args) is { } axesIndex && Layout.HitTestLegendItem(args.X, args.Y, axesIndex) is not null;

    /// <inheritdoc />
    public override void OnPointerPressed(PointerInputArgs args)
    {
        int axesIndex  = Layout.AxesOrFirstAt(args.X, args.Y);
        int? seriesIdx = Layout.HitTestLegendItem(args.X, args.Y, axesIndex);
        if (seriesIdx is null) return;
        Emit(new LegendToggleEvent(ChartId, axesIndex, seriesIdx.Value));
    }
}
