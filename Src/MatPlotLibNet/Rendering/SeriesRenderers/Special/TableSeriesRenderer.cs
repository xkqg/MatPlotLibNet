// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TableSeries"/> as a grid of text cells, in the plot area or below the axes (<see cref="TableGrid"/>).</summary>
internal sealed class TableSeriesRenderer : SeriesRenderer<TableSeries>
{
    /// <inheritdoc />
    public TableSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>Draws one cell: its fill and a one point rule round it, put on the pixel grid of a raster export under the matplotlib
    /// themes, as Agg draws the <c>Rectangle</c> patch each cell of matplotlib's <c>ax.table</c> is.</summary>
    /// <param name="area">The cell's rectangle.</param>
    /// <param name="fill">The cell's colour.</param>
    private void DrawCell(Rect area, Color fill) =>
        Ctx.DrawRectangleOnPixelGrid(area, _rule.StyleOf(fill));

    /// <summary>Where the text of a cell stands: centred on its cell's middle line, at the cell's middle when it is centred, else
    /// <paramref name="pad"/> in from the edge it is aligned to (matplotlib's <c>Cell._set_text_position</c>).</summary>
    /// <param name="cell">The cell's rectangle.</param>
    /// <param name="alignment">How the text is aligned in it.</param>
    /// <param name="pad">How far a left or right aligned text stands in from its edge, in pixels.</param>
    /// <returns>The placement.</returns>
    private static TextPlacement TextIn(Rect cell, TextAlignment alignment, double pad)
    {
        var middle = cell.Center;
        double x = alignment switch
        {
            TextAlignment.Center => middle.X,
            TextAlignment.Right => cell.X + cell.Width - pad,
            _ => cell.X + pad,
        };
        return new TextPlacement(new Point(x, middle.Y), alignment) { Vertical = TextVerticalAlignment.Center };
    }

    /// <summary>The colour and the width of the rule round every cell, worked out once in <see cref="Render"/>: a rule is a pixel wide; under the
    /// matplotlib themes it is matplotlib's <c>patch.linewidth</c> (a <c>Cell</c> is a <c>Rectangle</c>), 1 pt in both styles.</summary>
    private PatchEdge _rule;

    /// <inheritdoc />
    /// <remarks>The cells stand where <see cref="TableGrid"/> stands them, the rule the layout keeps room for them by: in the plot, or below
    /// the axes with the row headers left of the plot.</remarks>
    public override void Render(TableSeries series)
    {
        if (series.CellData.Length == 0 || TableGrid.Of(series, Area.PlotBounds, Ctx, Context.Theme, Context.Stretch) is not { } grid) return;

        int cols = series.ColumnCount;
        bool hasColHeaders = series.ColumnHeaders is { Length: > 0 };
        bool hasRowHeaders = series.RowHeaders is { Length: > 0 };

        // The cells as given, else as the theme cells a table (TableLook: matplotlib's ax.table under the matplotlib themes).
        var look = Context.Theme.Looks.Table;
        var headerColor = look.HeaderFillOver(series.HeaderColor);
        var cellColor = look.CellFillOver(series.CellColor);
        var border = look.BorderOver(series.BorderColor);
        _rule = new PatchEdge(border, Context.Theme.PatchEdgeOf(series, border).Width);

        // A text stands on the ground its own cell paints: the headers on the header colour, the data on the cell colour.
        double fontSize = grid.TextSize(series, Ctx, Context.Theme);
        var headerFont = look.FontOn(Context.Theme, headerColor, fontSize);
        var cellFont = look.FontOn(Context.Theme, cellColor, fontSize);

        var colWidths = grid.Widths;
        double rowH = grid.RowHeight;
        double y = grid.Top;

        // Column headers
        if (hasColHeaders)
        {
            double x = grid.Left;
            int shown = 0;
            if (hasRowHeaders)
            {
                // The corner above the row headers: a header cell under the library's looks, none under matplotlib's.
                if (look.CornerCell) DrawCell(new Rect(x, y, colWidths[shown], rowH), headerColor);
                x += colWidths[shown++];
            }
            for (int c = 0; c < series.ColumnHeaders!.Length && c < cols; c++)
            {
                double colW = colWidths[shown++];
                var cellArea = new Rect(x, y, colW, rowH);
                DrawCell(cellArea, headerColor);
                // A column's header is centred under every theme (matplotlib's colLoc='center').
                Ctx.Draw(TextLabel.Of(series.ColumnHeaders[c]), headerFont, TextIn(cellArea, TextAlignment.Center, 0));
                x += colW;
            }
            y += rowH;
        }

        // Data rows
        for (int r = 0; r < series.CellData.Length; r++)
        {
            double x = grid.Left;
            int shown = 0;
            if (hasRowHeaders)
            {
                double headerW = colWidths[shown++];
                string rowLabel = series.RowHeaders.ElementOr(r, "");
                var cellArea = new Rect(x, y, headerW, rowH);
                DrawCell(cellArea, headerColor);
                Ctx.Draw(TextLabel.Of(rowLabel), headerFont, TextIn(cellArea, look.RowHeaderAlignment, look.PadOver(series.GivenCellPadding, headerW)));
                x += headerW;
            }
            for (int c = 0; c < cols; c++)
            {
                double colW = colWidths[shown++];
                string cell = c < series.CellData[r].Length ? series.CellData[r][c] : "";
                var cellArea = new Rect(x, y, colW, rowH);
                DrawCell(cellArea, cellColor);
                Ctx.Draw(TextLabel.Of(cell), cellFont, TextIn(cellArea, look.AlignmentOf(series.ColumnAlignments, c), look.PadOver(series.GivenCellPadding, colW)));
                x += colW;
            }
            y += rowH;
        }
    }
}
