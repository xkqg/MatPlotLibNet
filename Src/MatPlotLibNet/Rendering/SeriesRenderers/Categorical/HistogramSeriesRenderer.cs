// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="HistogramSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class HistogramSeriesRenderer : SeriesRenderer<HistogramSeries>
{
    /// <inheritdoc />
    public HistogramSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(HistogramSeries series)
    {
        var baseColor = ResolveColor(series.Color);
        var fillColor = ApplyAlpha(baseColor, series.Alpha);
        if (series.Data.Length == 0) return;
        var bins = series.ComputeBins();
        int n = bins.Counts.Length;

        // Build weighted counts (double[] to allow density and weights).
        double[] heights = BuildHeights(series, bins);

        // Use series-level EdgeColor if set; otherwise fall back to theme PatchEdgeColor (e.g. black in MatplotlibClassic).
        Color? edgeColor = series.EdgeColor ?? Context.Theme?.PatchEdgeColor;
        double edgeWidth = edgeColor.HasValue ? 0.5 : 0;

        if (series.HistType == HistType.Bar)
        {
            for (int i = 0; i < n; i++)
            {
                double x0 = bins.Min + i * bins.BinWidth;
                double barW = bins.BinWidth * series.RWidth;
                double gap = (bins.BinWidth - barW) / 2;
                var tl = Transform.DataToPixel(x0 + gap, heights[i]);
                var br = Transform.DataToPixel(x0 + gap + barW, 0);
                Ctx.DrawRectangle(new Rect(tl.X, tl.Y, br.X - tl.X, br.Y - tl.Y), fillColor, edgeColor, edgeWidth);
            }
        }
        else
        {
            // Build step outline points: Step = unfilled, StepFilled = filled polygon.
            var pts = new List<Point>();
            pts.Add(Transform.DataToPixel(bins.Min, 0));
            for (int i = 0; i < n; i++)
            {
                double x1 = bins.Min + i * bins.BinWidth;
                double x2 = x1 + bins.BinWidth;
                pts.Add(Transform.DataToPixel(x1, heights[i]));
                pts.Add(Transform.DataToPixel(x2, heights[i]));
            }
            pts.Add(Transform.DataToPixel(bins.Min + n * bins.BinWidth, 0));

            if (series.HistType == HistType.StepFilled)
            {
                pts.Add(Transform.DataToPixel(bins.Min, 0));
                Ctx.DrawPolygon(pts, fillColor, edgeColor, edgeWidth > 0 ? edgeWidth : 1);
            }
            else
            {
                // Step: draw just the outline (no fill).
                Ctx.DrawLines(pts, baseColor, 1.5, LineStyle.Solid);
            }
        }
    }

    private static double[] BuildHeights(HistogramSeries series, HistogramBins bins)
    {
        int n = bins.Counts.Length;
        var heights = new double[n];

        if (series.Weights is not null && series.Weights.Length == series.Data.Length)
        {
            // Weighted binning: accumulate weights per bin.
            double min = bins.Min;
            double binWidth = bins.BinWidth;
            for (int j = 0; j < series.Data.Length; j++)
            {
                int idx = Math.Min((int)((series.Data[j] - min) / binWidth), n - 1);
                heights[idx] += series.Weights[j];
            }
        }
        else
        {
            for (int i = 0; i < n; i++)
                heights[i] = bins.Counts[i];
        }

        if (series.Density)
        {
            double total = heights.Sum() * bins.BinWidth;
            if (total > 0)
                for (int i = 0; i < n; i++)
                    heights[i] /= total;
        }

        if (series.Cumulative)
        {
            for (int i = 1; i < n; i++)
                heights[i] += heights[i - 1];
        }

        return heights;
    }
}
