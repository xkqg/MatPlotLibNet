// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Interaction;

/// <summary>Fires a <see cref="HoverEvent"/> when the cursor moves over a plot area with no button held, and names the data point
/// under it in <see cref="ActiveTooltip"/>. Fire-and-forget notification: the figure is never mutated.</summary>
public sealed class HoverModifier : InteractionModifier
{
    /// <summary>The figure whose points the tooltip names, or <see langword="null"/> for a hover that names none.</summary>
    private readonly Figure? _figure;

    private HoverTooltipContent? _tooltip;

    /// <summary>Creates a hover modifier for the given chart that sends the hover events and shows no tooltip.</summary>
    public HoverModifier(string chartId, IChartLayout layout, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink)
    {
    }

    /// <summary>Creates a hover modifier for the given chart that also names the point of <paramref name="figure"/> nearest the
    /// pointer in <see cref="ActiveTooltip"/>.</summary>
    /// <param name="chartId">The identifier of the chart the events it sends are addressed to.</param>
    /// <param name="layout">The layout of the rendered figure.</param>
    /// <param name="figure">The figure whose points the tooltip names.</param>
    /// <param name="sink">Receives every event the modifier sends.</param>
    public HoverModifier(string chartId, IChartLayout layout, Figure figure, Action<FigureInteractionEvent> sink)
        : base(chartId, layout, sink) => _figure = figure;

    /// <inheritdoc />
    /// <remarks>The tooltip of the point nearest the pointer over a plot area, through the same search a click on a point uses,
    /// with that point's indices. A move off every plot area clears it; a move with a button held (a drag) leaves it as it was.</remarks>
    public override HoverTooltipContent? ActiveTooltip => _tooltip;

    /// <inheritdoc />
    /// <remarks>The same as <see cref="OnPointerTracked"/>: a hover is told of every move.</remarks>
    public override void OnPointerMoved(PointerInputArgs args) => OnPointerTracked(args);

    /// <inheritdoc />
    public override void OnPointerTracked(PointerInputArgs args)
    {
        if (args.Button != PointerButton.None) return;

        int? axesIndex = Layout.HitTestAxes(args.X, args.Y);
        if (axesIndex is null)
        {
            _tooltip = null;
            return;
        }

        var coords = Layout.PixelToData(args.X, args.Y, axesIndex.Value)!.Value;
        _tooltip = TooltipAt(args, axesIndex.Value, coords);

        Emit(new HoverEvent(ChartId, axesIndex.Value, coords.DataX, coords.DataY));
    }

    /// <summary>The tooltip of the point nearest <paramref name="coords"/> in the axes, or <see langword="null"/> when the figure has
    /// none there or the hover was made without the figure.</summary>
    private HoverTooltipContent? TooltipAt(PointerInputArgs args, int axesIndex, DataPoint coords)
    {
        if (_figure is null) return null;

        var nearest = NearestPointFinder.Find(_figure, axesIndex, coords.DataX, coords.DataY, Layout);
        if (nearest is null) return null;

        return new HoverTooltipContent(
            nearest.Detail is { } detail
                ? $"{nearest.SeriesLabel}: {detail}"
                : string.Invariant($"{nearest.SeriesLabel}: ({nearest.DataX:G5}, {nearest.DataY:G5})"),
            args.X, args.Y)
        {
            // A host that highlights the row under the pointer needs the same two numbers a click carries.
            PointIndex = nearest.PointIndex,
            SeriesIndex = nearest.SeriesIndex,
        };
    }
}
