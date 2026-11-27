// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Where the cells of a table stand on a plot, the one rule the drawing (<see cref="TableSeriesRenderer"/>) and the layout
/// (<see cref="WithTablesOf"/>, the room an axes keeps below and beside its plot) both read: the left edge of the first displayed column,
/// the top of the first row, the width of each displayed column (the row headers' first, when the table has them), the height of a row
/// and how many rows are drawn. In the plot (<see cref="TableLocation.InPlot"/>) every displayed column shares the plot's width from its
/// top left corner; below the axes (<see cref="TableLocation.BelowAxes"/>, matplotlib's <c>loc='bottom'</c>: <c>Table._update_positions</c>
/// moves the grid of the data cells by <c>ox = -l</c>, <c>oy = -(b + h)</c>) the data columns share the plot's width from its bottom left
/// corner down and the row headers hang left of it, as wide as their widest text needs (<c>auto_set_column_width(-1)</c>) unless
/// <see cref="TableSeries.ColumnWidths"/> gives that column a width. A row is as high as the look says (<see cref="TableLook.CellHeightOver"/>):
/// under matplotlib's look a share of the axes' height that a layout engine stretches with the axes (<see cref="AnyStretchIn"/>).</summary>
/// <param name="Left">The left edge of the first displayed column, in pixels.</param>
/// <param name="Top">The top of the first row, the header row when there is one, in pixels.</param>
/// <param name="Widths">The width of each displayed column, the row headers' first when the table has them, in pixels.</param>
/// <param name="RowHeight">The height of every row, in pixels.</param>
/// <param name="RowCount">How many rows are drawn, the header row included.</param>
/// <param name="FittedRowHeaders">Whether the row headers' column was made as wide as its text, which the fit of the text leaves out
/// (matplotlib's auto-sized columns).</param>
internal readonly record struct TableGrid(double Left, double Top, double[] Widths, double RowHeight, int RowCount, bool FittedRowHeaders)
{
    /// <summary>The bottom of the last row, in pixels.</summary>
    internal double Bottom => Top + RowCount * RowHeight;

    /// <summary>Where <paramref name="series"/> stands on <paramref name="plot"/> under <paramref name="theme"/>; <see langword="null"/> for a
    /// table with no cells to draw.</summary>
    /// <param name="series">The table.</param>
    /// <param name="plot">The plot area of its axes.</param>
    /// <param name="ctx">The surface its row headers are measured on.</param>
    /// <param name="theme">The theme it is drawn under.</param>
    /// <param name="stretch">The height of the axes the table is laid at over its height at the figure's subplot parameters
    /// (<see cref="FigureRanges.StretchOf"/>): 1 where nothing moved the axes.</param>
    /// <returns>The grid, or <see langword="null"/>.</returns>
    internal static TableGrid? Of(TableSeries series, Rect plot, IRenderContext ctx, Theme theme, double stretch)
    {
        int columns = series.ColumnCount;
        if (columns == 0) return null;
        var look = theme.Looks.Table;
        bool rowHeaders = series.RowHeaders is { Length: > 0 };
        int rows = series.CellData.Length + (series.ColumnHeaders is { Length: > 0 } ? 1 : 0);
        double rowHeight = look.CellHeightOver(series.GivenCellHeight, stretch);
        if (!series.StandsOutsideThePlot(theme))
            return new TableGrid(plot.X, plot.Y, Shared(series.ColumnWidths, 0, columns + (rowHeaders ? 1 : 0), plot.Width, 0), rowHeight, rows, false);

        // Below the axes the data columns share the plot's width by their own weights; the row headers' column, before them in the
        // weights, is as wide as its weight says against theirs, or, given no weights, as its widest text needs.
        int first = rowHeaders ? 1 : 0;
        var widths = Shared(series.ColumnWidths, first, columns, plot.Width, first);
        bool fitted = rowHeaders && series.ColumnWidths is null;
        if (rowHeaders)
            widths[0] = fitted ? FittedHeaderWidth(series, look, ctx, theme)
                : plot.Width * series.ColumnWidths.ElementOr(0, 1) / WeightSum(series.ColumnWidths, first, columns);

        return new TableGrid(plot.X - (rowHeaders ? widths[0] : 0), plot.Y + plot.Height, widths, rowHeight, rows, fitted);
    }

    /// <summary>The widths of <paramref name="count"/> columns from the weight at <paramref name="first"/> on, sharing
    /// <paramref name="total"/> in proportion to their weights (a column without a weight counting for 1, so no weights at all is equal
    /// columns), from <paramref name="lead"/> on in an array that keeps that many places before them.</summary>
    private static double[] Shared(double[]? weights, int first, int count, double total, int lead)
    {
        var widths = new double[lead + count];
        double sum = WeightSum(weights, first, count);
        for (int i = 0; i < count; i++) widths[lead + i] = total * weights.ElementOr(first + i, 1) / sum;
        return widths;
    }

    /// <summary>The sum of the weights of <paramref name="count"/> columns from the one at <paramref name="first"/> on, a column without a
    /// weight counting for 1.</summary>
    private static double WeightSum(double[]? weights, int first, int count)
    {
        double sum = 0;
        for (int i = 0; i < count; i++) sum += weights.ElementOr(first + i, 1);
        return sum;
    }

    /// <summary>How wide the row headers' column is made when it is fitted to its text: the widest row header and its pads as the look
    /// fits a text (<see cref="TableLook.FittedWidth"/>), at the size the table's text is set in.</summary>
    private static double FittedHeaderWidth(TableSeries series, TableLook look, IRenderContext ctx, Theme theme)
    {
        var font = look.FontOn(theme, look.HeaderFillOver(series.HeaderColor), series.TextSizeUnder(theme));
        double widest = 0;
        for (int r = 0; r < series.CellData.Length; r++)
            widest = Math.Max(widest, ctx.ExtentOf(TextLabel.Of(series.RowHeaders.ElementOr(r, "")), font).Width);
        return look.FittedWidth(widest, series.GivenCellPadding);
    }

    /// <summary>The size the text of every cell is set in: the table's text size, made smaller under a look that fits a table's text to
    /// its cells (<see cref="TableLook.FitsTextToCells"/>, matplotlib's <c>Table._auto_set_font_size</c>): a point at a time, down to
    /// 1 pt, until the text of each cell and its pads fit the cell's width, and the smallest size any cell needs set in all of them. A
    /// column fitted to its text is left out of the measure, as matplotlib leaves out its auto-sized columns.</summary>
    /// <param name="series">The table.</param>
    /// <param name="ctx">The surface its text is measured on.</param>
    /// <param name="theme">The theme it is drawn under.</param>
    /// <returns>The size, in pixels.</returns>
    internal double TextSize(TableSeries series, IRenderContext ctx, Theme theme)
    {
        double size = series.TextSizeUnder(theme);
        var look = theme.Looks.Table;
        if (!look.FitsTextToCells) return size;
        var font = look.FontOn(theme, look.CellFillOver(series.CellColor), size);
        double step = 1.0.PointsInPixels;
        int first = Widths.Length - series.ColumnCount;
        double fitted = size;
        for (int c = 0; c < series.ColumnCount; c++)
        {
            double width = Widths[first + c];
            if (series.ColumnHeaders is { } headers && c < headers.Length) fitted = Math.Min(fitted, Fit(headers[c], width));
            foreach (var row in series.CellData) fitted = Math.Min(fitted, Fit(row.ElementOr(c, ""), width));
        }

        if (first == 1 && !FittedRowHeaders)
            for (int r = 0; r < series.CellData.Length; r++) fitted = Math.Min(fitted, Fit(series.RowHeaders.ElementOr(r, ""), Widths[0]));
        return fitted;

        // The size this text fits a cell of the width at: its width scales with its size.
        double Fit(string text, double cellWidth)
        {
            double atSize = ctx.ExtentOf(TextLabel.Of(text), font).Width;
            double fit = size;
            while (fit > step && look.FittedWidth(atSize * fit / size, series.GivenCellPadding) > cellWidth) fit -= step;
            return fit;
        }
    }

    /// <summary><paramref name="bands"/> with what the tables of <paramref name="axes"/> that stand below it reach past the plot: the rows
    /// below its bottom edge (<see cref="EdgeBands.HungBelow"/>) and the row headers' column left of its left edge
    /// (<see cref="EdgeBands.HungLeft"/>), the room a layout keeps for them as matplotlib's keeps it for a table in the axes' tight box;
    /// <paramref name="bands"/> itself when no table stands there.</summary>
    /// <param name="axes">The axes.</param>
    /// <param name="bands">What its renderer measured past its spines.</param>
    /// <param name="plot">Its plot area; only its X and width are read.</param>
    /// <param name="ctx">The surface text is measured on.</param>
    /// <param name="theme">The theme it is drawn under.</param>
    /// <param name="stretch">The height of the axes the tables are laid at over its height at the subplot parameters.</param>
    /// <returns>The bands.</returns>
    internal static EdgeBands WithTablesOf(Axes axes, EdgeBands bands, Rect plot, IRenderContext ctx, Theme theme, double stretch)
    {
        double below = bands.HungBelow, left = bands.HungLeft;
        var all = axes.Series;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] is not TableSeries table || !table.StandsOutsideThePlot(theme) || Of(table, plot, ctx, theme, stretch) is not { } grid) continue;
            below = Math.Max(below, grid.Bottom - plot.Y - plot.Height);
            left = Math.Max(left, plot.X - grid.Left);
        }

        return below == bands.HungBelow && left == bands.HungLeft ? bands : bands with { HungBelow = below, HungLeft = left };
    }

    /// <summary>Whether a table of <paramref name="figure"/>, on a subplot or on an inset of one, has rows that keep their share of the axes
    /// the table was made on (<see cref="TableLook.Stretches"/>): only then does a layout engine that moves the axes move what the table
    /// draws, and the draw keeps where the subplot parameters put each axes (<see cref="FigureRanges.MadeOn"/>).</summary>
    /// <param name="figure">The figure.</param>
    /// <returns>Whether such a table is drawn.</returns>
    internal static bool AnyStretchIn(Figure figure)
    {
        var look = figure.Theme.Looks.Table;
        if (!look.RowsKeepTheirShare) return false;
        // Walked by index: a foreach over the list boxes its enumerator.
        for (int i = 0; i < figure.SubPlots.Count; i++)
            if (Stretches(figure.SubPlots[i], look, depth: 0)) return true;
        return false;

        // An inset is drawn three deep at most (ChartRenderer.DrawWithInsets).
        static bool Stretches(Axes axes, TableLook look, int depth)
        {
            for (int i = 0; i < axes.Series.Count; i++)
                if (axes.Series[i] is TableSeries table && look.Stretches(table.GivenCellHeight)) return true;
            if (depth >= 3) return false;
            for (int i = 0; i < axes.Insets.Count; i++)
                if (Stretches(axes.Insets[i], look, depth + 1)) return true;
            return false;
        }
    }
}
