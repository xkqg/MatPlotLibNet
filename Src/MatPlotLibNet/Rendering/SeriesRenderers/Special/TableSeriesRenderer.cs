// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TableSeries"/> as a grid of text cells within the plot area.</summary>
internal sealed class TableSeriesRenderer : SeriesRenderer<TableSeries>
{
    /// <inheritdoc />
    public TableSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>The pixel width of each displayed column: <paramref name="total"/> shared among them in proportion to
    /// <paramref name="weights"/>, a column without a weight counting for 1, so no weights at all is equal columns.</summary>
    /// <param name="weights">The relative widths in the order of the displayed columns, or <see langword="null"/>.</param>
    /// <param name="count">How many columns are displayed.</param>
    /// <param name="total">The width to share.</param>
    private static double[] ColumnPixelWidths(double[]? weights, int count, double total)
    {
        var widths = new double[count];
        double sum = 0;
        for (int i = 0; i < count; i++)
        {
            widths[i] = weights is not null && i < weights.Length ? weights[i] : 1;
            sum += widths[i];
        }

        for (int i = 0; i < count; i++)
        {
            widths[i] = total * widths[i] / sum;
        }

        return widths;
    }

    /// <inheritdoc />
    public override void Render(TableSeries series)
    {
        if (series.CellData.Length == 0) return;

        int rows = series.CellData.Length;
        int cols = series.CellData.Max(r => r.Length);
        if (cols == 0) return;

        bool hasColHeaders = series.ColumnHeaders is { Length: > 0 };
        bool hasRowHeaders = series.RowHeaders is { Length: > 0 };

        var headerColor = series.HeaderColor ?? new Color(200, 200, 200);
        var cellColor = series.CellColor ?? new Color(255, 255, 255);
        var borderColor = series.BorderColor ?? new Color(80, 80, 80);

        var cellFont = new Font { Family = "sans-serif", Size = series.FontSize };

        double plotX = Area.PlotBounds.X;
        double plotY = Area.PlotBounds.Y;
        double totalW = Area.PlotBounds.Width;
        double totalH = Area.PlotBounds.Height;

        int displayCols = cols + (hasRowHeaders ? 1 : 0);
        double[] colWidths = ColumnPixelWidths(series.ColumnWidths, displayCols, totalW);
        double rowH = series.CellHeight;

        double y = plotY;

        // Column headers
        if (hasColHeaders)
        {
            double x = plotX;
            int shown = 0;
            if (hasRowHeaders)
            {
                Ctx.DrawRectangle(new Rect(x, y, colWidths[shown], rowH), new ShapeStyle(headerColor, borderColor, 1));
                x += colWidths[shown++];
            }
            for (int c = 0; c < series.ColumnHeaders!.Length && c < cols; c++)
            {
                double colW = colWidths[shown++];
                Ctx.DrawRectangle(new Rect(x, y, colW, rowH), new ShapeStyle(headerColor, borderColor, 1));
                Ctx.DrawText(series.ColumnHeaders[c], new Point(x + colW / 2, y + rowH / 2 + cellFont.Size / 3), cellFont, TextAlignment.Center);
                x += colW;
            }
            y += rowH;
        }

        // Data rows
        for (int r = 0; r < rows; r++)
        {
            double x = plotX;
            int shown = 0;
            if (hasRowHeaders)
            {
                double headerW = colWidths[shown++];
                string rowLabel = r < series.RowHeaders!.Length ? series.RowHeaders[r] : "";
                Ctx.DrawRectangle(new Rect(x, y, headerW, rowH), new ShapeStyle(headerColor, borderColor, 1));
                Ctx.DrawText(rowLabel, new Point(x + headerW / 2, y + rowH / 2 + cellFont.Size / 3), cellFont, TextAlignment.Center);
                x += headerW;
            }
            for (int c = 0; c < cols; c++)
            {
                double colW = colWidths[shown++];
                string cell = c < series.CellData[r].Length ? series.CellData[r][c] : "";
                Ctx.DrawRectangle(new Rect(x, y, colW, rowH), new ShapeStyle(cellColor, borderColor, 1));
                var alignment = series.ColumnAlignments is { } alignments && c < alignments.Length ? alignments[c] : TextAlignment.Left;
                double textX = alignment switch
                {
                    TextAlignment.Center => x + colW / 2,
                    TextAlignment.Right => x + colW - series.CellPadding,
                    _ => x + series.CellPadding,
                };
                Ctx.DrawText(cell, new Point(textX, y + rowH / 2 + cellFont.Size / 3), cellFont, alignment);
                x += colW;
            }
            y += rowH;
        }
    }
}
