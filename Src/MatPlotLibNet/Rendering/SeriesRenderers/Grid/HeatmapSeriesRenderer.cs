// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="HeatmapSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class HeatmapSeriesRenderer : SeriesRenderer<HeatmapSeries>
{
    /// <inheritdoc />
    public HeatmapSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    /// <remarks>The cells stand where their data coordinates put them, like every other series: column <c>c</c> covers X from
    /// <c>c</c> to <c>c + 1</c> and row <c>r</c> covers Y from <c>rows - r - 1</c> to <c>rows - r</c> (row 0 on top of an axis
    /// that counts upward), as the series' slots say (<see cref="HeatmapSeries.ColumnSlot"/>,
    /// <see cref="HeatmapSeries.RowSlot"/>), which the data table reads the names at. The axes' ticks, grid and month or row
    /// names all follow the range the axes show, so a heatmap whose range is not its grid's own (a limit set by hand, a
    /// zoom) must follow it too, or its cells stand under the wrong names. The cells outside the range are cut by the
    /// plot's clip.</remarks>
    public override void Render(HeatmapSeries series) => Paint(series, (r, c) =>
    {
        var column = HeatmapSeries.ColumnSlot(c);
        var row = series.RowSlot(r);
        return Rect.FromCorners(Transform.DataToPixel(column.Left, row.Right), Transform.DataToPixel(column.Right, row.Left));
    });

    /// <summary>Draws the cells to fill the area's plot rectangle exactly, whatever range the axes show: a heatmap that is a
    /// panel of a larger chart (a clustermap's matrix beside its dendrograms) or the backdrop of a filled contour is placed
    /// by the panel it stands in, not by the axes' data range.</summary>
    /// <param name="series">The matrix to draw.</param>
    internal void RenderInArea(HeatmapSeries series)
    {
        // An empty matrix divides by zero here and paints nothing: Paint returns before it asks for a cell.
        double cellW = Area.PlotBounds.Width / series.Data.GetLength(1), cellH = Area.PlotBounds.Height / series.Data.GetLength(0);
        Paint(series, (r, c) => new Rect(Area.PlotBounds.X + c * cellW, Area.PlotBounds.Y + r * cellH, cellW, cellH));
    }

    /// <summary>Draws one rectangle per visible cell, in the colour its value maps to, and the cell's value when the series
    /// shows them.</summary>
    /// <param name="series">The matrix to draw.</param>
    /// <param name="cellOf">Where cell (row, column) stands on the surface; a cell that cannot be placed is skipped.</param>
    private void Paint(HeatmapSeries series, Func<int, int, Rect> cellOf)
    {
        int rows = series.Data.GetLength(0), cols = series.Data.GetLength(1);
        if (rows == 0 || cols == 0) return;
        var (cmap, norm, min, max) = ResolveColormapping(series.Data, series, series);
        var df = Context.Theme.DefaultFont;
        var labelFont = new Font { Family = df.Family, Size = df.Size };
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            if (series.MaskMode.Hides(r, c)) continue;

            var cell = cellOf(r, c);
            if (!cell.IsDrawable()) continue;   // a cell with an edge that cannot be placed is skipped

            double value = series.Data[r, c];
            var color = cmap.ColorOf(value, norm, min, max);
            Ctx.DrawRectangle(cell, new ShapeStyle(color, null, 0));

            // A label states the cell's value; a missing cell has none to state.
            if (!series.ShowLabels || value.IsMissing()) continue;

            string text = value.ToString(series.LabelFormat ?? "F2", CultureInfo.InvariantCulture);
            var textColor = series.CellValueColor ?? color.ContrastingTextColor();
            var labelFontWithColor = labelFont with { Color = textColor };
            Ctx.DrawText(text, cell.Center, labelFontWithColor, TextAlignment.Center);
        }
    }

}
