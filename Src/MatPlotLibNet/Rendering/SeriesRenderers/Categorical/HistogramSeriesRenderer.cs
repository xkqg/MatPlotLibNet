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
    internal override void AddMarks(HistogramSeries series, MarkObstacles.Builder marks)
    {
        foreach (var shape in Shapes(series))
        {
            if (shape.Bar is { } bar) marks.AddBox(bar);
            else marks.AddPath(shape.Outline!);
        }
    }

    /// <summary>One thing a histogram draws: a bar, or the outline of its steps (filled or not).</summary>
    private readonly record struct HistogramShape(Rect? Bar, List<Point>? Outline, bool Filled);

    /// <summary>Everything the histogram draws, in drawing order: the one geometry <see cref="Render"/> draws and
    /// <see cref="AddMarks"/> hands to a legend.</summary>
    private IEnumerable<HistogramShape> Shapes(HistogramSeries series)
    {
        if (series.Data.Length == 0) yield break;
        var bins = series.ComputeBins();
        int n = bins.Counts.Length;

        // The heights the bars stand at (counts, weights, density, cumulative): the series' own rule, which its range reads too.
        double[] heights = series.Heights(bins);

        // A bin's bar stands on 0 and reaches its height. On a log axis 0 is clipped to the edge where the axis begins, as
        // matplotlib clips it; a height the axis cannot place (an empty bin there) is skipped.
        if (series.HistType == HistType.Bar)
        {
            for (int i = 0; i < n; i++)
            {
                double x0 = bins.Min + i * bins.BinWidth;
                double barW = bins.BinWidth * series.RWidth;
                double gap = (bins.BinWidth - barW) / 2;
                var bar = Transform.BoxOnFoot(new CategorySlot(x0 + gap, x0 + gap + barW / 2, x0 + gap + barW), 0, heights[i], rising: true, BarOrientation.Vertical).Rect;
                if (!bar.IsDrawable()) continue;
                yield return new HistogramShape(bar, null, false);
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
                yield return StepOutline(series, bins, topLeft, topRight, run);
        }
    }

    /// <inheritdoc />
    public override void Render(HistogramSeries series)
    {
        var baseColor = ResolveColor(series.Color);
        var fillColor = ApplyAlpha(baseColor, series.Alpha);

        // Use series-level EdgeColor if set; otherwise fall back to theme PatchEdgeColor (e.g. black in MatplotlibClassic).
        Color? edgeColor = series.EdgeColor ?? Context.Theme.PatchEdgeColor;
        double edgeWidth = edgeColor.HasValue ? 0.5 : 0;

        foreach (var shape in Shapes(series))
        {
            if (shape.Bar is { } bar)
                Ctx.DrawRectangle(bar, new ShapeStyle(fillColor, edgeColor, edgeWidth) { Hatch = series.Hatch, HatchColor = series.HatchColor });
            else if (shape.Filled)
                Ctx.DrawPolygon(shape.Outline!, new ShapeStyle(fillColor, edgeColor, edgeWidth > 0 ? edgeWidth : 1) { Hatch = series.Hatch, HatchColor = series.HatchColor });
            else
                // Step: draw just the outline (no fill).
                Ctx.DrawLines(shape.Outline!, new StrokeStyle(baseColor, 1.5, LineStyle.Solid));
        }
    }

    /// <summary>The outline of one run of steps: standing on the baseline, over the tops of its bins and back to the baseline;
    /// closed on its start when the histogram is filled.</summary>
    private HistogramShape StepOutline(HistogramSeries series, HistogramBins bins, Point[] topLeft, Point[] topRight, IndexRange run)
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

        bool filled = series.HistType == HistType.StepFilled;
        if (filled) pts.Add(start);
        return new HistogramShape(null, pts, filled);
    }

}
