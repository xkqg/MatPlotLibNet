// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TableSeries"/> as a grid of text cells within the plot area.</summary>
internal sealed class TableSeriesRenderer : SeriesRenderer<TableSeries>
{
    /// <inheritdoc />
    public TableSeriesRenderer(SeriesRenderContext context) : base(context) { }

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
        int displayRows = rows + (hasColHeaders ? 1 : 0);
        double colW = displayCols > 0 ? totalW / displayCols : totalW;
        double rowH = series.CellHeight;

        double y = plotY;

        // Column headers
        if (hasColHeaders)
        {
            double x = plotX;
            if (hasRowHeaders)
            {
                Ctx.DrawRectangle(new Rect(x, y, colW, rowH), headerColor, borderColor, 1);
                x += colW;
            }
            for (int c = 0; c < series.ColumnHeaders!.Length && c < cols; c++)
            {
                Ctx.DrawRectangle(new Rect(x, y, colW, rowH), headerColor, borderColor, 1);
                Ctx.DrawText(series.ColumnHeaders[c], new Point(x + colW / 2, y + rowH / 2 + cellFont.Size / 3), cellFont, TextAlignment.Center);
                x += colW;
            }
            y += rowH;
        }

        // Data rows
        for (int r = 0; r < rows; r++)
        {
            double x = plotX;
            if (hasRowHeaders)
            {
                string rowLabel = r < series.RowHeaders!.Length ? series.RowHeaders[r] : "";
                Ctx.DrawRectangle(new Rect(x, y, colW, rowH), headerColor, borderColor, 1);
                Ctx.DrawText(rowLabel, new Point(x + colW / 2, y + rowH / 2 + cellFont.Size / 3), cellFont, TextAlignment.Center);
                x += colW;
            }
            for (int c = 0; c < cols; c++)
            {
                string cell = c < series.CellData[r].Length ? series.CellData[r][c] : "";
                Ctx.DrawRectangle(new Rect(x, y, colW, rowH), cellColor, borderColor, 1);
                Ctx.DrawText(cell, new Point(x + series.CellPadding, y + rowH / 2 + cellFont.Size / 3), cellFont, TextAlignment.Left);
                x += colW;
            }
            y += rowH;
        }
    }
}
