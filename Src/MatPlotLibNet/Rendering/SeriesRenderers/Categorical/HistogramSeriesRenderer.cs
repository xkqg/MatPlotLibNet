// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models.Series.XY;
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

        // The heights the bars stand at (counts, weights, density, cumulative): the series' own rule, which its range reads too.
        double[] heights = series.Heights(bins);

        // Use series-level EdgeColor if set; otherwise fall back to theme PatchEdgeColor (e.g. black in MatplotlibClassic).
        Color? edgeColor = series.EdgeColor ?? Context.Theme.PatchEdgeColor;
        double edgeWidth = edgeColor.HasValue ? 0.5 : 0;

        // A bin's bar stands on 0 and reaches its height. On a log axis 0 is clipped to the edge where the axis begins, as
        // matplotlib clips it; a height the axis cannot place (an empty bin there) is skipped.
        if (series.HistType == HistType.Bar)
        {
            for (int i = 0; i < n; i++)
            {
                double x0 = bins.Min + i * bins.BinWidth;
                double barW = bins.BinWidth * series.RWidth;
                double gap = (bins.BinWidth - barW) / 2;
                var bar = Rect.FromCorners(Transform.DataToPixel(x0 + gap, heights[i]), Transform.DataToPixelClippedY(x0 + gap + barW, 0));
                if (!bar.IsDrawable()) continue;
                Ctx.DrawRectangle(bar, new ShapeStyle(fillColor, edgeColor, edgeWidth) { Hatch = series.Hatch, HatchColor = series.HatchColor });
            }
        }
        else
        {
            // The step outline (Step = unfilled, StepFilled = filled polygon) traces the bars: one outline per run of bins
            // whose tops can be placed, so a bin whose top cannot breaks it. Without such a bin it is the one outline over
            // every bin.
            var topLeft = new Point[n];
            var topRight = new Point[n];
            for (int i = 0; i < n; i++)
            {
                double x1 = bins.Min + i * bins.BinWidth;
                double x2 = x1 + bins.BinWidth;
                topLeft[i] = Transform.DataToPixel(x1, heights[i]);
                topRight[i] = Transform.DataToPixel(x2, heights[i]);
            }

            double[] left = Array.ConvertAll(topLeft, p => p.X);
            double[] top = Array.ConvertAll(topLeft, p => p.Y);
            double[] right = Array.ConvertAll(topRight, p => p.X);
            foreach (var run in left.DrawableIndexRuns(top, right))
            {
                RenderStepOutline(series, bins, topLeft, topRight, run, fillColor, edgeColor, edgeWidth, baseColor);
            }
        }
    }

    /// <summary>Renders the step outline of the bins in <paramref name="run"/>: up from the base at the run's left edge,
    /// along each bin's top, down to the base at its right edge (and back to the start when filled).</summary>
    private void RenderStepOutline(HistogramSeries series, HistogramBins bins, Point[] topLeft, Point[] topRight,
        IndexRange run, Color fillColor, Color? edgeColor, double edgeWidth, Color baseColor)
    {
        var pts = new List<Point>();
        var start = Transform.DataToPixelClippedY(bins.Min + run.StartInclusive * bins.BinWidth, 0);
        pts.Add(start);
        for (int i = run.StartInclusive; i < run.EndExclusive; i++)
        {
            pts.Add(topLeft[i]);
            pts.Add(topRight[i]);
        }
        pts.Add(Transform.DataToPixelClippedY(bins.Min + run.EndExclusive * bins.BinWidth, 0));

        if (series.HistType == HistType.StepFilled)
        {
            pts.Add(start);
            Ctx.DrawPolygon(pts, new ShapeStyle(fillColor, edgeColor, edgeWidth > 0 ? edgeWidth : 1) { Hatch = series.Hatch, HatchColor = series.HatchColor });
        }
        else
        {
            // Step: draw just the outline (no fill).
            Ctx.DrawLines(pts, new StrokeStyle(baseColor, 1.5, LineStyle.Solid));
        }
    }

}
