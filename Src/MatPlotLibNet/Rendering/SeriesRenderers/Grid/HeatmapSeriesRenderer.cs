// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

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
        double min = double.MaxValue, max = double.MinValue;
        foreach (double v in series.Data) { min = Math.Min(min, v); max = Math.Max(max, v); }
        if (min == max) max = min + 1;
        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        var norm = series.Normalizer ?? LinearNormalizer.Instance;
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var color = cmap.GetColor(norm.Normalize(series.Data[r, c], min, max));
            Ctx.DrawRectangle(new Rect(Area.PlotBounds.X + c * cellW, Area.PlotBounds.Y + r * cellH, cellW, cellH), color, null, 0);
        }
    }
}
