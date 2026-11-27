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
            else if (shape.Filled) marks.AddPath(shape.Outline!);
            else AddPolyline(marks, shape.Outline!);
        }
    }

    /// <summary>One thing a histogram draws: a bar, or the outline of its steps (filled or not).</summary>
    private readonly record struct HistogramShape(Rect? Bar, IReadOnlyList<Point>? Outline, bool Filled);

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
                double x0 = bins.EdgeAt(i);
                double barW = bins.BinWidth * series.RWidth;
                var span = Range1D.CentredIn(x0, bins.BinWidth, barW);
                var bar = PixelBox.OnFoot(Transform, new CategorySlot(span.Lo, span.Lo + barW / 2, span.Hi), 0, heights[i], rising: true, BarOrientation.Vertical).Rect;
                if (!bar.IsDrawable()) continue;
                yield return new HistogramShape(bar, null, false);
            }
        }
        else
        {
            // The step outline (Step = unfilled, StepFilled = filled polygon) traces the bars: one outline per run of bins
            // whose tops are there, so a bin whose top is missing breaks it. A top a log axis cannot place (an empty bin) lies a
            // thousand decades below it, as matplotlib's log scale puts it: the outline comes down to the edge there and is cut
            // at it. Without a missing top it is the one outline over every bin.
            var topLeft = new Point[n];
            var topRight = new Point[n];
            for (int i = 0; i < n; i++)
            {
                double x1 = bins.EdgeAt(i);
                double x2 = bins.EdgeAt(i + 1);
                topLeft[i] = Transform.DataToPixelUnclipped(x1, heights[i]);
                topRight[i] = Transform.DataToPixelUnclipped(x2, heights[i]);
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
        var baseColor = ColorOf(series);
        var fillColor = baseColor.WithOpacityGiven(series.Alpha);

        // A bin is one style: its fill, its edge and its hatch.
        var style = Context.Theme.PatchEdgesOf(series).StyleOf(fillColor, baseColor, series.Hatching);
        // A step histogram's outline is a patch's edge (matplotlib's hist draws it with fill, an unfilled Polygon): as wide as the
        // theme's patch.linewidth (1 pt under the matplotlib themes), a line's resting width under the library's own themes.
        var stepStroke = StrokeStyle.Solid(baseColor, Context.Theme.Looks.Patch.OutlineWidthOf(LineWidthRule.LineCollection));

        foreach (var shape in Shapes(series))
        {
            if (shape.Bar is { } bar)
            {
                // A bin's bar is a rectangle, which matplotlib's Agg puts on the pixel grid by its edge's width.
                Ctx.DrawSnappedRectangle(bar, style);
            }
            else if (shape.Filled)
                Ctx.DrawPolygon(shape.Outline!, style);
            else
                // Step: draw just the outline (no fill), broken where it was cut at the edge.
                DrawPolyline(shape.Outline!, stepStroke);
        }
    }

    /// <summary>The outline of one run of steps: standing on the baseline, over the tops of its bins and back to the baseline;
    /// closed on its start when the histogram is filled.</summary>
    private HistogramShape StepOutline(HistogramSeries series, HistogramBins bins, Point[] topLeft, Point[] topRight, IndexRange run)
    {
        var pts = new List<Point>();
        var start = Transform.DataToPixelClippedY(bins.EdgeAt(run.StartInclusive), 0);
        pts.Add(start);
        for (int i = run.StartInclusive; i < run.EndExclusive; i++)
        {
            pts.Add(topLeft[i]);
            pts.Add(topRight[i]);
        }
        pts.Add(Transform.DataToPixelClippedY(bins.EdgeAt(run.EndExclusive), 0));

        bool filled = series.HistType == HistType.StepFilled;
        if (filled) pts.Add(start);
        return new HistogramShape(null, Transform.CutBeyondTheStart(pts, closed: filled), filled);
    }

}
