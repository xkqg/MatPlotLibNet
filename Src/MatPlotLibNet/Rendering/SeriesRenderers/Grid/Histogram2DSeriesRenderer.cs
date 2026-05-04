// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="Histogram2DSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class Histogram2DSeriesRenderer : SeriesRenderer<Histogram2DSeries>
{
    /// <inheritdoc />
    public Histogram2DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Histogram2DSeries series)
    {
        if (series.X.Length == 0 || series.Y.Length == 0) return;

        int binsX = series.BinsX, binsY = series.BinsY;
        var counts = series.ComputeBinCounts();

        double cellW = Area.PlotBounds.Width / binsX;
        double cellH = Area.PlotBounds.Height / binsY;

        var (cmap, norm, min, max) = ResolveColormapping(counts, series, series);

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
