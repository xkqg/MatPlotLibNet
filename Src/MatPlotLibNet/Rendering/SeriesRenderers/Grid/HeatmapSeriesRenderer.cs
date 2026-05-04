// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="HeatmapSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class HeatmapSeriesRenderer : SeriesRenderer<HeatmapSeries>
{
    /// <inheritdoc />
    public HeatmapSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(HeatmapSeries series)
    {
        int rows = series.Data.GetLength(0), cols = series.Data.GetLength(1);
        if (rows == 0 || cols == 0) return;
        double cellW = Area.PlotBounds.Width / cols, cellH = Area.PlotBounds.Height / rows;
        var (cmap, norm, min, max) = ResolveColormapping(series.Data, series, series);
        var df = Context.Theme.DefaultFont;
        var labelFont = new Font { Family = df.Family, Size = df.Size };
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            if (series.MaskMode.Hides(r, c)) continue;

            var color = cmap.GetColor(norm.Normalize(series.Data[r, c], min, max));
            double x = Area.PlotBounds.X + c * cellW;
            double y = Area.PlotBounds.Y + r * cellH;
            Ctx.DrawRectangle(new Rect(x, y, cellW, cellH), color, null, 0);

            if (!series.ShowLabels) continue;

            string text = series.Data[r, c].ToString(series.LabelFormat ?? "F2", CultureInfo.InvariantCulture);
            var textColor = series.CellValueColor ?? color.ContrastingTextColor();
            var labelFontWithColor = labelFont with { Color = textColor };
            var centre = new Point(x + cellW / 2, y + cellH / 2);
            Ctx.DrawText(text, centre, labelFontWithColor, TextAlignment.Center);
        }
    }

}
