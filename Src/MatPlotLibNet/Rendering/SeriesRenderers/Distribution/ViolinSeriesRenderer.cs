// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ViolinSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ViolinSeriesRenderer : SeriesRenderer<ViolinSeries>
{
    /// <inheritdoc />
    public ViolinSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(ViolinSeries series, MarkObstacles.Builder marks)
    {
        foreach (var violin in Violins(series))
        {
            if (violin.Outline is not null) marks.AddPath(violin.Outline);
            foreach (var line in violin.Lines) AddSegment(marks, line.From, line.To);
        }
    }

    /// <summary>One line over a violin: its ends and the stroke it is drawn with.</summary>
    private readonly record struct ViolinLine(Point From, Point To, StrokeStyle Stroke);

    /// <summary>One violin as it stands on the plot: the outline of its body, when it can be drawn, and the lines over it.</summary>
    private readonly record struct ViolinPixels(IReadOnlyList<Point>? Outline, Color Fill, List<ViolinLine> Lines);

    /// <summary>The violins in order: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a legend.</summary>
    private IEnumerable<ViolinPixels> Violins(ViolinSeries series)
    {
        // The body's colour and the colour of the lines over it come from the slot, the one owner the legend swatch asks
        // too: the colour the series names, else the colour its theme gives a violin (e.g. 'y'=#BFBF00 and 'r'=#FF0000 in
        // MatplotlibClassic), else the theme's colour at the series' slot.
        var bodyColor  = ColorOf(series);
        var statsColor = Slot.StatsColorOf(series);
        // The lines over a body are the theme's violin look (ViolinLook): under the matplotlib themes each is a line of the stats, a
        // collection of lines (matplotlib's hlines) at lines.linewidth; the library's own themes mark the median and the mean their way.
        var look = Context.Theme.Looks.Violin;
        var statsStroke = StrokeOf(look.Extrema, statsColor);

        // The violin is drawn from the observations that are there: a missing one (NaN) and an infinity are left out, as they
        // are left out of the range, which reads these same bodies.
        var bodies = series.BodiesUnder(Context.Theme.Looks);
        for (int i = 0; i < bodies.Length; i++)
        {
            var data = bodies[i].Sample;
            if (data.Length == 0) continue;
            double pos = series.PositionIn(i, Context.Theme);
            double min = data[0], max = data[^1];

            // The body is matplotlib's fill_betweenx polygon and the lines are its LineCollections, every vertex placed by the
            // log scale's clip (a value a log axis cannot place a thousand decades beyond it): the body is cut where its outline
            // leaves the plot through the edge such an axis begins at, and a line that runs to such a value is cut on that edge
            // (a line wholly beyond it is not drawn). A body with a vertex the axes cannot place at all (inside a break, masked
            // by a logit axis) is skipped.
            var left = new List<Point>(); var right = new List<Point>();
            foreach (var slice in bodies[i].Outline)
            {
                // The look's share of the outline (the body's reach), on the sides the series draws.
                double halfWidth = slice.HalfWidth * look.BodyReach;
                left.Add(Transform.DataToPixelUnclipped(pos - series.BodyLeft(halfWidth), slice.Y));
                right.Add(Transform.DataToPixelUnclipped(pos + series.BodyRight(halfWidth), slice.Y));
            }
            right.Reverse();
            var body = new List<Point>(); body.AddRange(left); body.AddRange(right);
            var cut = Transform.CutBeyondTheStart(body, closed: true);
            IReadOnlyList<Point>? outline = cut.IsDrawable() && (ReferenceEquals(cut, body) || cut.Count > 0) ? cut : null;

            var lines = new List<ViolinLine>();

            // ShowExtrema: draw vertical bar + horizontal ticks at min/max using stats color.
            if (series.ShowExtrema)
            {
                double extHalfW = series.ExtremaHalfWidthUnder(look);
                // Vertical center bar spanning min→max
                AddLine(lines, pos, min, pos, max, statsStroke);
                // Horizontal tick at min
                AddLine(lines, pos - extHalfW, min, pos + extHalfW, min, statsStroke);
                // Horizontal tick at max
                AddLine(lines, pos - extHalfW, max, pos + extHalfW, max, statsStroke);
            }

            // ShowMedians: draw median line across full width
            if (series.ShowMedians)
            {
                double median = data.Percentile(50);
                double medHalfW = series.StatsHalfWidthUnder(look);
                // Made here, not beside the stats stroke: a local that lives across the yield is a field of this iterator.
                AddLine(lines, pos - medHalfW, median, pos + medHalfW, median, StrokeOf(look.Median, statsColor));
            }

            // ShowMeans: draw mean line across full width
            if (series.ShowMeans)
            {
                double mean = data.Average();
                double meanHalfW = series.StatsHalfWidthUnder(look);
                AddLine(lines, pos - meanHalfW, mean, pos + meanHalfW, mean, StrokeOf(look.Mean, statsColor));
            }

            yield return new ViolinPixels(outline, bodyColor, lines);
        }
    }

    /// <summary>The stroke of one line over a violin: the look's colour, else the stats colour; the width of the look's rule through the
    /// slot; the look's dash.</summary>
    private StrokeStyle StrokeOf(ViolinStatsLine line, Color statsColor) =>
        Slot.StrokeOf(line.Color ?? statsColor, line.Width, line.Style);

    /// <summary>Adds the line from (<paramref name="x1"/>, <paramref name="y1"/>) to (<paramref name="x2"/>, <paramref name="y2"/>)
    /// as the plot leaves it (<see cref="DataTransform.SegmentBetween"/>); nothing when no part of it is drawn.</summary>
    private void AddLine(List<ViolinLine> lines, double x1, double y1, double x2, double y2, StrokeStyle stroke)
    {
        if (Transform.SegmentBetween(x1, y1, x2, y2) is { } line) lines.Add(new ViolinLine(line.From, line.To, stroke));
    }

    /// <inheritdoc />
    public override void Render(ViolinSeries series)
    {
        // A body is a collection: under the matplotlib themes edged by the patch rule (black under classic, none under v2) at the body's
        // alpha, as a collection's alpha takes its edge too (the body's given edge carries it); outlined in its own colour, 1 pixel,
        // under the library's own themes.
        var edges = Context.Theme.PatchEdgesOf(series);
        foreach (var violin in Violins(series))
        {
            if (violin.Outline is not null)
            {
                Ctx.DrawPolygon(violin.Outline, edges.StyleOf(violin.Fill.WithOpacityGiven(series.Alpha), violin.Fill));
            }
            foreach (var line in violin.Lines) DrawSegment(line.From, line.To, line.Stroke);
        }
    }
}
