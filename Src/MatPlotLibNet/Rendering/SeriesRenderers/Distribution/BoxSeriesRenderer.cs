// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="BoxSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class BoxSeriesRenderer : SeriesRenderer<BoxSeries>
{
    /// <inheritdoc />
    public BoxSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(BoxSeries series, MarkObstacles.Builder marks)
    {
        foreach (var box in Boxes(series))
        {
            if (box.Box.IsDrawable()) marks.AddBox(box.Box);
            AddSegment(marks, box.Median.From, box.Median.To);
            AddSegment(marks, box.WhiskerUp.From, box.WhiskerUp.To);
            AddSegment(marks, box.WhiskerDown.From, box.WhiskerDown.To);
            if (box.CapUp is { } capUp) AddSegment(marks, capUp.From, capUp.To);
            if (box.CapDown is { } capDown) AddSegment(marks, capDown.From, capDown.To);
            foreach (var flier in box.Fliers)
                if (flier.IsDrawable()) marks.AddPoint(flier);
            if (box.Mean is { } mean && mean.IsDrawable()) marks.AddPoint(mean);
        }
    }

    /// <summary>One box as it stands on the plot: its rectangle, its median and whiskers, the caps across the whisker ends and the
    /// observations beyond the whiskers where the theme draws them, and its mean when it shows one.</summary>
    private readonly record struct BoxPixels(Rect Box, PixelSegment Median, PixelSegment WhiskerUp, PixelSegment WhiskerDown,
        PixelSegment? CapUp, PixelSegment? CapDown, Point[] Fliers, Point? Mean);

    /// <summary>The boxes in order: the one geometry <see cref="Render"/> draws and <see cref="AddMarks"/> hands to a legend.</summary>
    private IEnumerable<BoxPixels> Boxes(BoxSeries series)
    {
        double halfW = series.HalfWidthIn(Context.Theme);
        var look = Context.Theme.Looks.Box;
        double capShare = look.Caps?.Share ?? 0;
        for (int i = 0; i < series.Datasets.Length; i++)
        {
            // The box is drawn from the observations that are there: a missing one (NaN) and an infinity are left out,
            // as they are left out of the range.
            var data = series.Datasets[i].SortedDrawableValues();
            if (data.Length == 0) continue;
            double pos = series.PositionIn(i, Context.Theme);
            var stats = StatisticsOf(data, series.Whis);
            double q1 = stats.Q1, median = stats.Median, q3 = stats.Q3, whisLo = stats.WhiskerLow, whisHi = stats.WhiskerHigh;
            double? mean = series.ShowMeans ? data.Average() : null;
            double capHalf = halfW * capShare;
            var fliers = look.Fliers is not null && series.ShowOutliers ? stats.Fliers : [];

            // The box and each whisker stand on their lower statistic and reach the upper one, as a bar stands on its
            // foot: a lower statistic the axis cannot place (0 or below on a log axis) is clipped to the edge where the
            // axis begins, as matplotlib clips it, and a part whose upper statistic it cannot place lies wholly beyond the
            // axis and is skipped. The median and the mean stand at one value: skipped when it cannot be placed. The box is
            // the rectangle between its two corners (Rect.FromCorners), of positive size on an axis that runs backwards. A cap
            // stands across the end of its whisker, and a flier at its value on the box's own position. A box that lies down
            // (Vert false) is the upright one with its axes swapped: its values run along X.
            bool valueAlongX = !series.Vert;
            yield return new BoxPixels(
                PixelBox.OnFoot(Transform, CategorySlot.Around(pos, 2 * halfW), q1, q3, rising: true, BarOrientation.ValuesAlong(valueAlongX)).Rect,
                new PixelSegment(Pixel(valueAlongX, median, pos - halfW), Pixel(valueAlongX, median, pos + halfW)),
                new PixelSegment(BasePixel(valueAlongX, q3, pos), Pixel(valueAlongX, whisHi, pos)),
                new PixelSegment(Pixel(valueAlongX, q1, pos), BasePixel(valueAlongX, whisLo, pos)),
                look.Caps is null ? null : new PixelSegment(Pixel(valueAlongX, whisHi, pos - capHalf), Pixel(valueAlongX, whisHi, pos + capHalf)),
                look.Caps is null ? null : new PixelSegment(Pixel(valueAlongX, whisLo, pos - capHalf), Pixel(valueAlongX, whisLo, pos + capHalf)),
                FlierPixels(fliers, pos, valueAlongX),
                mean is { } m ? Pixel(valueAlongX, m, pos) : null);
        }
    }

    /// <summary>Where a value of a box stands, at <paramref name="place"/> across: the value along X for a box that lies down, along
    /// Y for one that stands up.</summary>
    private Point Pixel(bool valueAlongX, double value, double place)
    {
        var at = Point.Along(valueAlongX, value, place);
        return Transform.DataToPixel(at.X, at.Y);
    }

    /// <summary>Where a value a part of a box stands on stands (<see cref="Pixel"/>), pinned where its axis begins when the axis cannot
    /// place it (<see cref="DataTransform.DataToPixelClippedAlong"/>).</summary>
    private Point BasePixel(bool valueAlongX, double value, double place)
    {
        var at = Point.Along(valueAlongX, value, place);
        return Transform.DataToPixelClippedAlong(valueAlongX, at.X, at.Y);
    }

    /// <summary>Where the observations beyond the whiskers stand: at their value on the box's own position.</summary>
    private Point[] FlierPixels(double[] values, double position, bool valueAlongX)
    {
        if (values.Length == 0) return [];
        var pixels = new Point[values.Length];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Pixel(valueAlongX, values[i], position);
        return pixels;
    }

    /// <summary>How the parts of a box are stroked: the theme's look (<see cref="BoxLook"/>: matplotlib's <c>boxplot.*</c> under the
    /// matplotlib themes, the cycle colour, a red median two pixels wide and whiskers a pixel wide under the library's own). A part
    /// whose look names no colour wears the series' own, and no caps are drawn where the look names none.</summary>
    private readonly record struct BoxStyles(ShapeStyle Box, StrokeStyle Median, StrokeStyle Whisker, StrokeStyle? Cap);

    private BoxStyles StylesOf(BoxSeries series, BoxLook look)
    {
        // A colour the series names colours the box, its whiskers and its caps; the median keeps its own. A box in a colour group
        // (a verb's box standing with its violin and points) wears the group's colour as one that names it: the box of the group is
        // not black beside parts of the group that are coloured. A part whose look names no colour wears the series' own.
        Color? named = series.Color ?? (series.ColorGroup is null ? null : SeriesColor);
        return new BoxStyles(look.Outline.OutlineOver(named, SeriesColor), look.Median.StrokeOver(series.MedianColor, SeriesColor),
            look.Whisker.StrokeOver(named, SeriesColor), look.Caps?.Line.StrokeOver(named, SeriesColor));
    }

    /// <inheritdoc />
    public override void Render(BoxSeries series)
    {
        var look = Context.Theme.Looks.Box;
        var styles = StylesOf(series, look);
        foreach (var box in Boxes(series))
        {
            DrawBox(box.Box, styles.Box);
            DrawStraight(box.Median, styles.Median);
            DrawStraight(box.WhiskerUp, styles.Whisker);
            DrawStraight(box.WhiskerDown, styles.Whisker);
            if (styles.Cap is { } cap)
            {
                if (box.CapUp is { } capUp) DrawStraight(capUp, cap);
                if (box.CapDown is { } capDown) DrawStraight(capDown, cap);
            }

            if (look.Fliers is { } fliers) DrawFliers(box.Fliers, fliers);
            if (box.Mean is { } mean) DrawMean(mean, look.Mean);
        }
    }

    /// <summary>Draws the box when it can be drawn: a box whose third quartile the axes cannot place is skipped. Its corners stand on
    /// the pixel grid of a surface that has one, where the theme asks, as matplotlib's Agg snaps the path of a rectangle.</summary>
    private void DrawBox(Rect box, ShapeStyle style)
    {
        if (box.IsDrawable()) Ctx.DrawSnappedRectangle(box, style);
    }

    /// <summary>Strokes a horizontal or vertical segment of the box (the median, a whisker, a cap) when the axes can place both of its
    /// ends, on the pixel grid of a surface that has one where the theme asks, as matplotlib's Agg snaps a <c>Line2D</c> of one
    /// rectilinear segment; butt ends.</summary>
    private void DrawStraight(PixelSegment segment, StrokeStyle stroke)
    {
        if (!segment.From.CanDrawLineTo(segment.To)) return;
        var line = Ctx.OnPixelGrid(segment, stroke.Thickness, projectingEnds: false);
        Ctx.DrawLine(line.From, line.To, stroke);
    }

    /// <summary>Draws the mean as the look marks it (matplotlib's <c>boxplot.meanprops</c> under the matplotlib themes) when the axes can
    /// place it.</summary>
    private void DrawMean(Point at, BoxMarker mean)
    {
        if (at.IsDrawable())
            MarkerShape.Of(mean.Marker, Context.Theme).Draw(Ctx, at, mean.Size.PointsInPixels, ShapeStyle.Edged(mean.Face, mean.Edge, mean.EdgeWidth));
    }

    /// <summary>Draws the observations beyond the whiskers as the look marks them (matplotlib's <c>boxplot.flierprops</c> under the
    /// matplotlib themes): a marker made of strokes (a plus) in the edge colour, any other open or filled and outlined in it.</summary>
    private void DrawFliers(Point[] fliers, BoxMarker look)
    {
        if (fliers.Length == 0) return;
        var marker = MarkerShape.Of(look.Marker, Context.Theme);
        double size = look.Size.PointsInPixels;
        var style = marker.StyleOf(look.Face, look.Edge, look.EdgeWidth);
        foreach (var flier in fliers)
            if (flier.IsDrawable()) marker.Draw(Ctx, flier, size, style);
    }

    /// <summary>What one box of a <see cref="Models.Series.BoxSeries"/> shows, as matplotlib's <c>cbook.boxplot_stats</c> works it out: the quartiles, the
    /// two whiskers and the observations beyond them. A whisker reaches to the most extreme observation within <c>whis</c> times the
    /// interquartile range past its box, and stops at the box where no observation lies between the two; the fliers are the
    /// observations outside the whiskers.</summary>
    /// <param name="Q1">The first quartile.</param>
    /// <param name="Median">The median.</param>
    /// <param name="Q3">The third quartile.</param>
    /// <param name="WhiskerLow">Where the lower whisker ends; not a number when the reach is not a number.</param>
    /// <param name="WhiskerHigh">Where the upper whisker ends; not a number when the reach is not a number.</param>
    /// <param name="Fliers">The observations below the lower whisker and above the upper one, in ascending order.</param>
    internal readonly record struct BoxStatistics(double Q1, double Median, double Q3, double WhiskerLow, double WhiskerHigh, double[] Fliers);

    /// <summary>The statistics of a set of observations.</summary>
    /// <param name="sorted">The observations that can be drawn, in ascending order, at least one.</param>
    /// <param name="whis">How far a whisker reaches past its box, as a multiple of the interquartile range.</param>
    /// <returns>The statistics. A reach that is not a number gives no whisker and no flier; an infinite one reaches the extremes.</returns>
    internal static BoxStatistics StatisticsOf(double[] sorted, double whis)
    {
        double q1 = sorted.Percentile(25), median = sorted.Percentile(50), q3 = sorted.Percentile(75);
        if (double.IsNaN(whis)) return new(q1, median, q3, double.NaN, double.NaN, []);
        if (double.IsPositiveInfinity(whis)) return new(q1, median, q3, sorted[0], sorted[^1], []);

        double reach = whis * (q3 - q1);
        double upperFence = q3 + reach, lowerFence = q1 - reach;
        double high = q3, low = q1;

        // The last observation within the upper fence and the first within the lower one, when they stand beyond the box.
        for (int i = sorted.Length - 1; i >= 0; i--)
        {
            if (sorted[i] <= upperFence) { high = Math.Max(sorted[i], q3); break; }
        }

        foreach (double value in sorted)
        {
            if (value >= lowerFence) { low = Math.Min(value, q1); break; }
        }

        return new(q1, median, q3, low, high, FliersOutside(sorted, low, high));
    }

    /// <summary>The observations below <paramref name="low"/> and above <paramref name="high"/>, in ascending order.</summary>
    private static double[] FliersOutside(double[] sorted, double low, double high)
    {
        int below = 0;
        while (below < sorted.Length && sorted[below] < low) below++;
        int above = sorted.Length;
        while (above > below && sorted[above - 1] > high) above--;
        int count = below + (sorted.Length - above);
        if (count == 0) return [];
        var fliers = new double[count];
        Array.Copy(sorted, 0, fliers, 0, below);
        Array.Copy(sorted, above, fliers, below, sorted.Length - above);
        return fliers;
    }
}
