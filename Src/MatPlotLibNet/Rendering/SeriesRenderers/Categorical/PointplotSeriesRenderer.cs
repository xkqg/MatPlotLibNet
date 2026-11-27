// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="PointplotSeries"/> as mean point + confidence interval whisker per category, the means joined by a
/// line where the theme or the series says so.</summary>
internal sealed class PointplotSeriesRenderer : SeriesRenderer<PointplotSeries>
{
    /// <inheritdoc />
    public PointplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(PointplotSeries series, MarkObstacles.Builder marks)
    {
        foreach (var point in Points(series))
        {
            if (point.Mean.IsDrawable()) marks.AddPoint(point.Mean);
            if (point.Interval is { } interval) marks.AddPath([interval.From, interval.To]);
        }
    }

    /// <summary>One category's point: the mean (masked where the axes cannot place it, as a marker is), where the line through the
    /// means passes it (a mean a log axis cannot place a thousand decades beyond it), the vertical interval and its two caps, each
    /// as the plot leaves it (<see cref="DataTransform.SegmentBetween"/>; <see langword="null"/> when nothing of it is drawn).</summary>
    private readonly record struct PointPixels(Point Mean, Point Through, PixelSegment? Interval, PixelSegment? LowerCap, PixelSegment? UpperCap);

    /// <summary>The points in order: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a legend. The mean and
    /// the interval are the series' (<see cref="PointplotSeries.EstimateOf"/>), the same the range of the axes is made from.</summary>
    private IEnumerable<PointPixels> Points(PointplotSeries series)
    {
        double halfCap = series.CapSizeIn(Context.Theme) / 2.0;
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            if (series.EstimateOf(i, Context.Theme) is not { } estimate) continue;

            yield return new PointPixels(
                Transform.DataToPixel(i, estimate.Mean),
                Transform.DataToPixelUnclipped(i, estimate.Mean),
                Transform.SegmentBetween(i, estimate.Lower, i, estimate.Upper),
                Transform.SegmentBetween(i - halfCap, estimate.Lower, i + halfCap, estimate.Lower),
                Transform.SegmentBetween(i - halfCap, estimate.Upper, i + halfCap, estimate.Upper));
        }
    }

    /// <inheritdoc />
    public override void Render(PointplotSeries series)
    {
        var color = ColorOf(series);
        double r = series.DiameterUnder(Context.Theme, Slot) / 2.0;
        double width = series.LineWidthIn(Slot);
        double capSize = series.CapSizeIn(Context.Theme);
        var stroke = StrokeStyle.Solid(color, width);
        // seaborn's dots are the markers of its line: outlined as a Line2D marker is (markeredgewidth = linewidth * 0.75, the edge
        // 'auto': the line's colour, black under the classic style); under the library's own themes they have no outline.
        var dot = Context.Theme.Looks.Dots.LikeSeaborn
            ? Slot.LineMarkerStyleOf(MarkerStyle.Circle, color, null, null, width * 0.75)
            : ShapeStyle.Filled(color);
        var points = Points(series).ToList();

        // seaborn plots the line through the means first (ax.lines[0], with the dots as its markers) and each interval as a line of its own.
        // The line joins the means it can place; one a log axis cannot place takes it down to the edge and back, as a line.
        if (series.IsJoinedIn(Context.Theme))
        {
            var means = points.Select(point => point.Through).Where(mean => mean.IsDrawable()).ToList();
            if (means.FormsALine) DrawPolyline(Transform.CutBeyondTheStart(means, closed: false), stroke);
        }

        foreach (var point in points)
        {
            // An interval and a cap are Line2Ds of two vertices, horizontal or vertical, which Agg puts on the pixel grid with projecting ends.
            Stroke(point.Interval, stroke);
            if (capSize > 0)
            {
                Stroke(point.LowerCap, stroke);
                Stroke(point.UpperCap, stroke);
            }

            if (point.Mean.IsDrawable()) Ctx.DrawCircle(point.Mean, r, dot);
        }
    }

    private void Stroke(PixelSegment? line, StrokeStyle stroke)
    {
        if (line is not { } segment) return;
        var drawn = Ctx.OnPixelGrid(segment, stroke.Thickness, projectingEnds: true);
        Ctx.DrawLine(drawn.From, drawn.To, stroke);
    }
}
