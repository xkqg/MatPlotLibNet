// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Interaction;

/// <summary>Fires a <see cref="HoverEvent"/> when the cursor moves over a plot area with no button held.
/// Fire-and-forget notification: the figure is never mutated.</summary>
public sealed class HoverModifier : InteractionModifier
{
    /// <summary>Creates a hover modifier for the given chart.</summary>
    public HoverModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink)
    {
    }

    /// <inheritdoc />
    public override void OnPointerMoved(PointerInputArgs args)
    {
        if (args.Button != PointerButton.None) return;

        int? axesIndex = Layout.HitTestAxes(args.X, args.Y);
        if (axesIndex is null) return;

        var coords = Layout.PixelToData(args.X, args.Y, axesIndex.Value);

        Emit(new HoverEvent(ChartId, axesIndex.Value, coords!.Value.DataX, coords.Value.DataY));
    }
}
