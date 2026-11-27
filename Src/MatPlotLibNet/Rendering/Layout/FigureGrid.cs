// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.ColorBarRendering;

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>The grid a figure lays its subplots out on: how many rows and columns it has, the cell each axes takes, which
/// edges of the grid that cell touches, and the plot area of each axes under a spacing. Worked out once per layout from the
/// figure, with no allocation, and read by the drawing, the layout that sizes the margins and the hit test alike: the cell an
/// axes is drawn in is the cell whose edges decide where the room it needs past its spines goes, so the two cannot disagree.
/// <para>A figure with a <see cref="GridSpec"/> has its rows and columns, and an axes takes its <see cref="Axes.GridPosition"/>
/// (the first cell when it has none). Without one, the grid is the largest an axes names with its rows and columns, an axes
/// stands at the index it names or else at its turn, and axes that name no grid at all stand side by side in one row.</para></summary>
internal readonly struct FigureGrid
{
    private readonly Figure _figure;

    /// <summary>The grid of <paramref name="figure"/> as it stands now.</summary>
    /// <param name="figure">The figure being laid out.</param>
    internal FigureGrid(Figure figure)
    {
        _figure = figure;
        if (figure.GridSpec is { } gs)
        {
            Rows = gs.Rows;
            Cols = gs.Cols;
        }
        else
        {
            int rows = 1, cols = 1;
            bool noGrid = true;
            foreach (var ax in figure.SubPlots)
            {
                if (ax.GridRows > 0) rows = Math.Max(rows, ax.GridRows);
                if (ax.GridCols > 0) cols = Math.Max(cols, ax.GridCols);
                noGrid &= ax.GridRows == 0;
            }

            Rows = noGrid ? 1 : rows;
            Cols = noGrid ? figure.SubPlots.Count : cols;
        }
    }

    /// <summary>How many rows the grid has.</summary>
    internal int Rows { get; }

    /// <summary>How many columns the grid has.</summary>
    internal int Cols { get; }

    /// <summary>The cell the axes at <paramref name="index"/> in <see cref="Figure.SubPlots"/> takes.</summary>
    /// <param name="index">The axes' index.</param>
    internal GridPosition CellOf(int index)
    {
        var axes = _figure.SubPlots[index];
        if (_figure.GridSpec is not null) return axes.GridPosition ?? GridPosition.Single(0, 0);

        int at = axes.GridIndex > 0 ? axes.GridIndex - 1 : index;
        return new PanelWrap(Rows, Cols).At(at);
    }

    /// <summary>The edges of the grid the cell of the axes at <paramref name="index"/> touches (<see cref="CellOf"/>, the cell
    /// it is drawn in): whether its need on each side goes to the figure's margin there or to the gap beside it.</summary>
    /// <param name="index">The axes' index.</param>
    internal Sides<bool> EdgeOf(int index) => EdgeIn(CellOf(index), Rows, Cols);

    /// <summary>Whether the axes at <paramref name="index"/> stands on the edge of the grid the figure's colour bar
    /// <paramref name="bar"/> stands at: the last column for a bar beside, the bottom row for one below. What the bar stands clear of
    /// and what an outside legend there stands past it are both read by it.</summary>
    /// <param name="index">The axes' index.</param>
    /// <param name="bar">The figure's colour bar.</param>
    internal bool StandsAtBarEdge(int index, ColorBar bar) => EdgeOf(index).EndAlong(bar.Beside);

    /// <summary>The height of the cell of the axes at <paramref name="index"/> on a figure with no margins and no gaps: the
    /// upper bound of its plot's height, which the layout measures an axes at before the plots are laid out.</summary>
    /// <param name="index">The axes' index.</param>
    internal double CellHeight(int index)
    {
        var cell = CellOf(index);
        return _figure.Height * (cell.RowEnd - cell.RowStart) / Rows;
    }

    /// <summary>The plot area of each axes, in <see cref="Figure.SubPlots"/> order, with the first row at
    /// <paramref name="plotAreaTop"/> under <paramref name="sp"/>: its cell, spanning the gaps between the rows and columns it
    /// spans, and an axes with an aspect fitted inside its cell (<see cref="PlotAspectExtensions"/>). Here, and not in a
    /// renderer: every reader of the layout (the drawing, the constrained layout, the hit test) reads these rectangles.</summary>
    /// <param name="plotAreaTop">The Y coordinate the first row starts at.</param>
    /// <param name="sp">The spacing in pixels.</param>
    /// <returns>One plot area for each axes.</returns>
    internal List<Rect> PlotRects(double plotAreaTop, SubPlotSpacing sp)
    {
        var cells = Cells(plotAreaTop, sp);
        var room = _figure.BarRoom;
        var figureBar = room.AnchorsAxes && _figure.FigureColorBar is { Visible: true } anchoring ? anchoring : null;
        for (int i = 0; i < cells.Count && i < _figure.SubPlots.Count; i++)
            cells[i] = _figure.SubPlots[i].FittedTo(cells[i], _figure.Theme, figureBar, room);
        return cells;
    }

    /// <summary>The cell of each axes, in <see cref="Figure.SubPlots"/> order, with the first row at
    /// <paramref name="plotAreaTop"/> under <paramref name="sp"/>, before any of it is given to the colour bar of the axes or
    /// fitted to an aspect: what a colour bar that takes its room from its axes stands in. When the figure's own colour bar
    /// takes its room from the subplots (<see cref="ColorBarRoomExtensions.FigureBarTakingRoom"/>), the cells
    /// are the ones it leaves, every one moved with the box that holds them (<see cref="Rect.Remapped"/>).</summary>
    /// <param name="plotAreaTop">The Y coordinate the first row starts at.</param>
    /// <param name="sp">The spacing in pixels.</param>
    /// <returns>One cell for each axes.</returns>
    internal List<Rect> Cells(double plotAreaTop, SubPlotSpacing sp)
    {
        var cells = LaidOutCells(plotAreaTop, sp);
        if (_figure.FigureBarTakingRoom() is { } bar && Rect.Enclosing(cells) is { Width: > 0, Height: > 0 } whole)
        {
            var kept = bar.KeptOf(whole);
            for (int i = 0; i < cells.Count; i++)
                cells[i] = cells[i].Remapped(whole, kept);
        }

        return cells;
    }

    /// <summary>The box that holds the cells of every subplot before the figure's own colour bar took its room: what that bar
    /// stands in (<see cref="ColorBarRoom.BodyOf"/>).</summary>
    /// <param name="plotAreaTop">The Y coordinate the first row starts at.</param>
    /// <param name="sp">The spacing in pixels.</param>
    /// <returns>The box; <see langword="null"/> when the figure's bar takes no room, or the figure has no subplot.</returns>
    internal Rect? FigureBarCell(double plotAreaTop, SubPlotSpacing sp) =>
        _figure.FigureBarTakingRoom() is null ? null : Rect.Enclosing(LaidOutCells(plotAreaTop, sp));

    /// <summary>Whether a colour bar, of some axes or of the figure, takes its room under the figure's theme, so a draw needs the
    /// cells beside the plot areas (<see cref="Cells"/>, <see cref="FigureBarCell"/>).</summary>
    internal bool AnyBarTakesRoom
    {
        get
        {
            var room = _figure.BarRoom;
            if (!room.TakesCell) return false;
            if (_figure.FigureColorBar is { Visible: true }) return true;
            // Walked by index: a foreach over the list boxes its enumerator.
            for (int i = 0; i < _figure.SubPlots.Count; i++)
                if (_figure.SubPlots[i].BarTakingRoom(room) is not null) return true;
            return false;
        }
    }

    /// <summary>The cell of each axes as the grid lays it out under <paramref name="sp"/>, whatever stands in it.</summary>
    private List<Rect> LaidOutCells(double plotAreaTop, SubPlotSpacing sp) =>
        _figure.GridSpec is { } gs ? SpecCells(gs, plotAreaTop, sp) : EqualCells(plotAreaTop, sp);

    private List<Rect> SpecCells(GridSpec gs, double plotAreaTop, SubPlotSpacing sp)
    {
        double totalWidth = _figure.Width - sp.MarginLeft - sp.MarginRight;
        double totalHeight = _figure.Height - plotAreaTop - sp.MarginBottom;
        double[] colWidths = RatioSizes(totalWidth, gs.Cols, sp.HorizontalGap, gs.WidthRatios);
        double[] rowHeights = RatioSizes(totalHeight, gs.Rows, sp.VerticalGap, gs.HeightRatios);

        double[] colX = new double[gs.Cols];
        colX[0] = sp.MarginLeft;
        for (int c = 1; c < gs.Cols; c++)
            colX[c] = colX[c - 1] + colWidths[c - 1] + sp.HorizontalGap;

        double[] rowY = new double[gs.Rows];
        rowY[0] = plotAreaTop;
        for (int r = 1; r < gs.Rows; r++)
            rowY[r] = rowY[r - 1] + rowHeights[r - 1] + sp.VerticalGap;

        var areas = new List<Rect>(_figure.SubPlots.Count);
        for (int i = 0; i < _figure.SubPlots.Count; i++)
        {
            var pos = CellOf(i);
            double width = 0;
            for (int c = pos.ColStart; c < pos.ColEnd; c++)
                width += colWidths[c];
            width += sp.HorizontalGap * (pos.ColEnd - pos.ColStart - 1);

            double height = 0;
            for (int r = pos.RowStart; r < pos.RowEnd; r++)
                height += rowHeights[r];
            height += sp.VerticalGap * (pos.RowEnd - pos.RowStart - 1);

            areas.Add(new Rect(colX[pos.ColStart], rowY[pos.RowStart], width, height));
        }

        return areas;
    }

    /// <summary>Shares <paramref name="totalSpace"/> less the gaps among <paramref name="count"/> cells by
    /// <paramref name="ratios"/>, or equally when there are none for each cell.</summary>
    private static double[] RatioSizes(double totalSpace, int count, double gap, double[]? ratios)
    {
        double available = totalSpace - gap * (count - 1);
        var sizes = new double[count];

        if (ratios is null || ratios.Length != count)
        {
            Array.Fill(sizes, available / count);
        }
        else
        {
            double ratioSum = 0;
            foreach (var r in ratios) ratioSum += r;
            for (int i = 0; i < count; i++)
                sizes[i] = available * ratios[i] / ratioSum;
        }

        return sizes;
    }

    private List<Rect> EqualCells(double plotAreaTop, SubPlotSpacing sp)
    {
        double totalWidth = _figure.Width - sp.MarginLeft - sp.MarginRight;
        double totalHeight = _figure.Height - plotAreaTop - sp.MarginBottom;
        double cellWidth = (totalWidth - sp.HorizontalGap * (Cols - 1)) / Cols;
        double cellHeight = (totalHeight - sp.VerticalGap * (Rows - 1)) / Rows;

        var areas = new List<Rect>(_figure.SubPlots.Count);
        for (int i = 0; i < _figure.SubPlots.Count; i++)
        {
            var cell = CellOf(i);
            areas.Add(new Rect(sp.MarginLeft + cell.ColStart * (cellWidth + sp.HorizontalGap),
                plotAreaTop + cell.RowStart * (cellHeight + sp.VerticalGap), cellWidth, cellHeight));
        }

        return areas;
    }

    /// <summary>The edges of a grid of <paramref name="rows"/> by <paramref name="columns"/> this cell touches: the layout reads
    /// them to decide where an axes' need goes (<see cref="Rendering.Layout.ConstrainedLayoutEngine.AxisMargins"/>: the margin of an edge it touches, the gap beside it
    /// otherwise). A cell whose end lies at or past the grid's last row or column touches that edge, so a position wider than the
    /// grid the figure measured still reaches the far side.</summary>
    /// <param name="rows">How many rows the grid has.</param>
    /// <param name="columns">How many columns the grid has.</param>
    /// <returns>For each side, whether the cell touches that edge; every edge it does not touch is an inside of the grid on that
    /// side.</returns>
    /// <param name="position">The <see cref="GridPosition"/> it works on.</param>
    internal static Sides<bool> EdgeIn(GridPosition position, int rows, int columns) =>
        new(Left: position.ColStart == 0, Bottom: position.RowEnd >= rows,
            Top: position.RowStart == 0, Right: position.ColEnd >= columns);
}
