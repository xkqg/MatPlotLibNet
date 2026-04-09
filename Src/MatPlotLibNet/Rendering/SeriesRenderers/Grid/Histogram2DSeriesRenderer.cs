// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

internal sealed class Histogram2DSeriesRenderer : SeriesRenderer<Histogram2DSeries>
{
    public Histogram2DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(Histogram2DSeries series)
    {
        if (series.X.Length == 0 || series.Y.Length == 0) return;

        int binsX = series.BinsX, binsY = series.BinsY;
        var counts = series.ComputeBinCounts();

        double cellW = Area.PlotBounds.Width / binsX;
        double cellH = Area.PlotBounds.Height / binsY;

        // Find min/max counts for color mapping
        double min = double.MaxValue, max = double.MinValue;
        for (int r = 0; r < binsY; r++)
        for (int c = 0; c < binsX; c++)
        {
            if (counts[r, c] < min) min = counts[r, c];
            if (counts[r, c] > max) max = counts[r, c];
        }
        if (min == max) max = min + 1;

        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        for (int r = 0; r < binsY; r++)
        for (int c = 0; c < binsX; c++)
        {
            var color = cmap.GetColor(norm.Normalize(counts[r, c], min, max));
            Ctx.DrawRectangle(
                new Rect(Area.PlotBounds.X + c * cellW, Area.PlotBounds.Y + r * cellH, cellW, cellH),
                color, null, 0);
        }
    }
}
