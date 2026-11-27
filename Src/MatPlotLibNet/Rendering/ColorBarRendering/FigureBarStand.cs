// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>Where the figure's own colour bar stands in one draw: the box it stands by (<see cref="Box"/>), the box of every cell it
/// takes its room from under matplotlib's slot (<see cref="Whole"/>), and how far it stands clear of what the subplots on its edge draw
/// there (<see cref="ClearanceWith"/>, <see cref="ColorBarRoomExtensions"/>' <c>FigureBarClearance</c>), measured once a draw by the
/// first that asks. Its drawing reads it (<see cref="ChartRenderer.RenderFigureColorBar"/>), and so does a legend outside an axes on
/// its edge, which stands past it (<see cref="PastPlotOf"/>, read by <see cref="AxesEdgeNeeds.DrawnPast"/>) as it stands past the
/// axes' own bar. Made by the draw's layout (<see cref="ChartRenderer.FrameOf"/>) and kept with its ranges
/// (<see cref="FigureRanges.FigureBar"/>).</summary>
internal sealed class FigureBarStand
{
    private readonly Figure _figure;
    private readonly FigureGrid _grid;
    private readonly List<Rect> _plots;
    private readonly FigureRanges? _ranges;
    private double? _clearance;

    private FigureBarStand(Figure figure, FigureGrid grid, List<Rect> plots, Rect box, Rect? whole, FigureRanges? ranges)
    {
        _figure = figure;
        _grid = grid;
        _plots = plots;
        Box = box;
        Whole = whole;
        _ranges = ranges;
    }

    /// <summary>The box the bar stands by: the one that holds every plot area, or on a figure with no subplot the one the margins
    /// leave.</summary>
    internal Rect Box { get; }

    /// <summary>The box the bar takes its room from when its room takes the cells (<see cref="ColorBarRoom.TakesCell"/>): the one
    /// that holds the cells of every subplot (<see cref="FigureGrid.FigureBarCell"/>), else, on a figure with no subplot,
    /// <see cref="Box"/>; <see langword="null"/> when the room takes no cell or the cells are not known.</summary>
    internal Rect? Whole { get; }

    /// <summary>The figure's colour bar when it is drawn; <see langword="null"/> when it is hidden.</summary>
    internal ColorBar? Bar => _figure.FigureColorBar is { Visible: true } bar ? bar : null;

    /// <summary>Where the figure's bar stands with its subplots at <paramref name="plots"/>.</summary>
    /// <param name="figure">The figure being drawn.</param>
    /// <param name="grid">The grid its subplots stand on.</param>
    /// <param name="plots">The plot area of each subplot, in <see cref="Figure.SubPlots"/> order.</param>
    /// <param name="spacing">The spacing the subplots were laid out with, whose margins give the box on a figure with no subplot.</param>
    /// <param name="cell">The box that holds the cells of every subplot (<see cref="FigureGrid.FigureBarCell"/>);
    /// <see langword="null"/> when it is not known.</param>
    /// <param name="ranges">The data ranges of the draw, which the renderers that measure the subplots on the bar's edge read;
    /// <see langword="null"/> outside a draw.</param>
    /// <returns>The stand.</returns>
    internal static FigureBarStand Of(Figure figure, FigureGrid grid, List<Rect> plots, SubPlotSpacing spacing, Rect? cell, FigureRanges? ranges)
    {
        // Without a subplot there is nothing to stand beside or take the room from: the box is the one the margins leave.
        var plotted = Rect.Enclosing(plots);
        var box = plotted ?? new Rect(spacing.MarginLeft, spacing.MarginTop, figure.Width - spacing.MarginLeft - spacing.MarginRight,
            figure.Height - spacing.MarginTop - spacing.MarginBottom);
        Rect? whole = figure.BarRoom.TakesCell ? cell ?? (plotted is null ? box : null) : null;
        return new FigureBarStand(figure, grid, plots, box, whole, ranges);
    }

    /// <summary>How far past <see cref="Box"/> the bar stands clear of what the subplots on its edge draw there
    /// (<see cref="ColorBarRoomExtensions"/>' <c>FigureBarClearance</c>): each such subplot's renderer is asked for only the bands the
    /// room stands past, the first time it is asked in the draw; a bar in the cells' slot stands past nothing, and no renderer is
    /// made.</summary>
    /// <param name="ctx">The context the subplots' bands are measured with.</param>
    /// <returns>The clearance, in pixels.</returns>
    internal double ClearanceWith(IRenderContext ctx) =>
        _clearance ??= _figure.FigureBarClearance(_plots, _grid,
            i => AxesRenderer.Create(_figure.SubPlots[i], _plots[i], ctx, _figure.Theme, null, _ranges));

    /// <summary>How far past the edge of <paramref name="plot"/> on the bar's side the figure's bar reaches with its tick labels and its
    /// label, as it is drawn (<see cref="ColorBarRoom.PastPlot"/>), for <paramref name="axes"/> on that edge of the grid (the last column
    /// for a bar beside, the bottom row for one below); 0 for any other axes, an inset, and a figure whose bar is hidden.</summary>
    /// <param name="axes">The axes whose plot it is.</param>
    /// <param name="plot">Its plot area.</param>
    /// <param name="ctx">The context text is measured with.</param>
    /// <returns>The reach past the plot's edge, in pixels; never below 0.</returns>
    internal double PastPlotOf(Axes axes, Rect plot, IRenderContext ctx)
    {
        if (Bar is not { } bar) return 0;
        var subPlots = _figure.SubPlots;
        // Walked by index: a foreach over the list boxes its enumerator.
        int index = -1;
        for (int i = 0; i < subPlots.Count && index < 0; i++)
            if (ReferenceEquals(subPlots[i], axes)) index = i;
        if (index < 0 || !_grid.StandsAtBarEdge(index, bar)) return 0;
        var room = _figure.BarRoom;
        double reach = room.PastPlot(bar, Box, Whole, plot, subPlots.SelectMany(each => each.Series), ctx, _figure.Theme, ClearanceWith(ctx));
        return Math.Max(0, reach);
    }
}
