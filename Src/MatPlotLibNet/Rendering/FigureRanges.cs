// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.ColorBarRendering;

namespace MatPlotLibNet.Rendering;

/// <summary>The data range of each subplot of a figure for one draw (<see cref="AxesRangeExtensions"/>' rule): worked out the
/// first time the draw asks for it, then read by every renderer the draw makes for that subplot, the ones the layout measures
/// it with and the one that draws it, so a subplot is measured and drawn by one range and the range is worked out once a draw.
/// One is made for each draw (<see cref="ChartRenderer.FrameOf"/>, carried by <see cref="Rendering.ChartRenderer.FigureFrame"/>) and goes with it:
/// nothing carries over to the next draw, and a draw writes nothing into the model, so a range cannot go stale while it is
/// read.</summary>
/// <remarks>An axes that was not a subplot of the figure when the draw began (an inset) is kept from the first ask until another
/// inset is asked for: its parent draws the inset's zoom indicator under it from that range, before the inset's own render reads
/// it. The draw's layout leaves the cell of each subplot here too when a colour bar takes its room (<see cref="CellOf"/>), and where the
/// figure's own bar stands (<see cref="FigureBar"/>), so every renderer of the draw reads them from the one object it already
/// holds.</remarks>
/// <param name="figure">The figure being drawn: its subplots and the theme whose margins pad their ranges.</param>
internal sealed class FigureRanges(Figure figure)
{
    /// <summary>The range of each subplot the figure held when the draw began, in <see cref="Figure.SubPlots"/> order, once it
    /// has been asked for.</summary>
    private readonly AxesRange?[] _ranges = new AxesRange?[figure.SubPlots.Count];

    /// <summary>An inset and its range for this draw.</summary>
    /// <param name="Axes">The inset.</param>
    /// <param name="Range">Its range.</param>
    private sealed record KeptInset(Axes Axes, AxesRange Range);

    /// <summary>The inset asked for last and its range. Every ask for one inset comes before the next inset is asked for: the
    /// parent asks when it draws the inset's zoom, then the inset's render asks while it draws, and its own insets are drawn
    /// after it. So one inset is kept at a time, made when it is first asked for: a draw without an inset allocates nothing for
    /// it.</summary>
    private KeptInset? _inset;

    /// <summary>What the colour bars stand in, kept together so a draw with no bar that takes its room and no bar of the figure holds
    /// one empty reference for all of it and a range object keeps its size.</summary>
    private sealed class HeldCells
    {
        internal List<Rect>? Cells;
        internal FigureBarStand? FigureBar;
        internal Dictionary<Axes, Rect>? InsetCells;
        internal List<Rect>? MadeOn;
    }

    private HeldCells? _room;

    private HeldCells Room => _room ??= new();

    /// <summary>The cell of each subplot before the colour bar of the axes took its room or an aspect was fitted
    /// (<see cref="Layout.FigureGrid.Cells"/>), in <see cref="Figure.SubPlots"/> order, set by the layout of the draw when a bar
    /// takes its room (<see cref="ChartRenderer.FrameOf"/>); <see langword="null"/> otherwise, so a draw without such a bar
    /// allocates nothing for it.</summary>
    internal List<Rect>? Cells
    {
        get => _room?.Cells;
        set => Room.Cells = value;
    }

    /// <summary>Where the figure's own colour bar stands in this draw (<see cref="FigureBarStand"/>): its drawing reads it, and so does
    /// a legend outside an axes on its edge, which stands past it. Set by the layout of the draw when the figure has a visible bar
    /// (<see cref="ChartRenderer.FrameOf"/>); <see langword="null"/> otherwise.</summary>
    internal FigureBarStand? FigureBar
    {
        get => _room?.FigureBar;
        set => Room.FigureBar = value;
    }

    /// <summary>The plot of each subplot where the figure's subplot parameters put it, in <see cref="Figure.SubPlots"/> order: where
    /// matplotlib makes what keeps a share of its axes' height (a table's rows, <see cref="Styling.TableLook.CellHeightOver"/>) before a
    /// layout engine moves the axes. Set by the draw when a tight or constrained layout lays out a figure that draws such a table
    /// (<see cref="ChartRenderer.FrameOf"/>, <see cref="SeriesRenderers.TableGrid.AnyStretchIn"/>); <see langword="null"/> otherwise, and
    /// nothing is stretched (<see cref="StretchOf"/>).</summary>
    internal List<Rect>? MadeOn
    {
        get => _room?.MadeOn;
        set => Room.MadeOn = value;
    }

    /// <summary>Whether the draw keeps where the subplot parameters put each subplot (<see cref="MadeOn"/>), so a layout that moves the
    /// axes stretches what keeps a share of their height.</summary>
    internal bool Stretches => _room?.MadeOn is not null;

    /// <summary>How far the layout stretched <paramref name="axes"/> from where the subplot parameters put it: <paramref name="height"/>,
    /// the height of the plot it is laid at, over its height at the subplot parameters (<see cref="MadeOn"/>); 1 when the draw keeps no
    /// such place, and for an axes that is not a subplot of the figure (an inset is stretched as its parent is,
    /// <see cref="AxesRenderer.Stretch"/>).</summary>
    /// <param name="axes">The axes.</param>
    /// <param name="height">The height of the plot it is laid at, in pixels.</param>
    /// <returns>The stretch.</returns>
    internal double StretchOf(Axes axes, double height)
    {
        if (_room?.MadeOn is not { } made) return 1;
        var subPlots = figure.SubPlots;
        int held = Math.Min(subPlots.Count, made.Count);
        for (int i = 0; i < held; i++)
            if (ReferenceEquals(subPlots[i], axes)) return made[i].Height > 0 ? height / made[i].Height : 1;
        return 1;
    }

    /// <summary>Keeps the rectangle an inset is given in this draw, which its colour bar stands in
    /// (<see cref="ColorBarRendering.ColorBarRoomExtensions"/>): an inset is no subplot of the draw's grid, so its cell is known
    /// only when the parent places it. Made when the first such inset is drawn, so a draw without one allocates nothing for it.</summary>
    /// <param name="inset">The inset.</param>
    /// <param name="cell">The rectangle the parent gave it, before an aspect was fitted in it.</param>
    internal void HoldCellOf(Axes inset, Rect cell) => (Room.InsetCells ??= [])[inset] = cell;

    /// <summary>The cell the layout gave <paramref name="axes"/> in this draw, which a colour bar that takes its room from the axes
    /// stands in (<see cref="ColorBarRendering.ColorBarRoomExtensions"/>): a subplot's cell, or the rectangle the parent gave an
    /// inset (<see cref="HoldCellOf"/>); <see langword="null"/> when the draw kept none, and the bar takes the cell its plot area
    /// was kept from.</summary>
    /// <param name="axes">The axes.</param>
    internal Rect? CellOf(Axes axes)
    {
        if (_room is not { } room) return null;
        if (room.Cells is { } cells)
        {
            var subPlots = figure.SubPlots;
            int held = Math.Min(subPlots.Count, cells.Count);
            for (int i = 0; i < held; i++)
                if (ReferenceEquals(subPlots[i], axes)) return cells[i];
        }

        return room.InsetCells is { } insets && insets.TryGetValue(axes, out var inset) ? inset : null;
    }

    /// <summary>The room the colour bars of the figure being drawn take (<see cref="ColorBarRoom"/>), by its theme and layout.</summary>
    internal ColorBarRoom BarRoom => figure.BarRoom;

    /// <summary>Whether the draw is laid out by a layout that works its margins out from what is drawn and keeps room for it (the
    /// tight or the constrained layout); the default layout keeps the margins it is given.</summary>
    internal bool KeepsRoom => figure.Spacing.TightLayout || figure.Spacing.ConstrainedLayout;

    /// <summary>The data range of <paramref name="axes"/> for this draw, with whether its patches can reach past the plot, both from
    /// the one pass over what asks for the range.</summary>
    /// <param name="axes">The axes: a subplot of the figure, or an inset.</param>
    /// <returns>The range the axes is drawn over and the clip of its patches.</returns>
    internal AxesRange RangeOf(Axes axes)
    {
        var subPlots = figure.SubPlots;
        int held = Math.Min(subPlots.Count, _ranges.Length);
        for (int i = 0; i < held; i++)
        {
            if (ReferenceEquals(subPlots[i], axes))
                return _ranges[i] ??= axes.RangeOf(figure.Theme);
        }

        if (_inset is { } kept && ReferenceEquals(kept.Axes, axes)) return kept.Range;
        var range = axes.RangeOf(figure.Theme);
        _inset = new KeptInset(axes, range);
        return range;
    }
}
