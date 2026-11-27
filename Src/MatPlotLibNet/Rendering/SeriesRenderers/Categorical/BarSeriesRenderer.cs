// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="BarSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class BarSeriesRenderer : SeriesRenderer<BarSeries>
{
    /// <inheritdoc />
    public BarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <summary>How far a label stands from the end of its bar, in pixels.</summary>
    private const double LabelPad = 4;

    /// <summary>The labels of the bars, each with the point it would stand at: beyond the end of its bar, the way the bar points
    /// in the data. A bar whose value is zero or more points towards the larger values, a bar below zero towards the smaller;
    /// where those lie on the screen depends on whether the axis runs backwards, so a label never stands inside its own bar
    /// because the axis was turned. Above the end the baseline stands the pad above it; below it, the pad and the height the
    /// letters rise above their baseline below it; right of a horizontal bar's end the text starts the pad after it, left of
    /// it the text ends the pad before it. A bar whose end the axes do not show (<see cref="Rect.Shows"/>) has no label, as
    /// matplotlib's <c>bar_label</c>, an annotation of that end, draws none.</summary>
    private List<LabelCandidate> LabelCandidates(BarSeries series)
    {
        var candidates = new List<LabelCandidate>();
        if (!series.ShowLabels) return candidates;

        var font = Context.Theme.DataTextFont();
        bool backwards = ValueRunsBackwards(Transform, series.Orientation);
        foreach (var pixel in PixelBars(series, Transform))
        {
            string label = series.LabelOf(series.Values[pixel.Index]);
            bool towardsHigher = pixel.Rising != backwards;   // the end lies above (right of) the foot on the screen
            if (!series.Orientation.ValuesAlongX)
            {
                double centreX = (pixel.From.X + pixel.To.X) / 2;
                double end = pixel.EndAlong(series.Orientation.ValuesAlongX);
                // matplotlib's bar_label annotates the bar's end: a label whose end the axes do not show is not drawn.
                if (!Area.PlotBounds.Shows(new Point(centreX, end))) continue;
                // Past the end that rises, on the baseline; past the end that falls, hung from the pad.
                candidates.Add(towardsHigher
                    ? new LabelCandidate(new Point(centreX, end - LabelPad), label, font, TextAlignment.Center)
                    : new LabelCandidate(new Point(centreX, end + LabelPad), label, font, TextAlignment.Center) { VerticalAlignment = TextVerticalAlignment.Top });
            }
            else
            {
                double centreY = (pixel.From.Y + pixel.To.Y) / 2;
                double end = pixel.EndAlong(series.Orientation.ValuesAlongX);
                if (!Area.PlotBounds.Shows(new Point(end, centreY))) continue;
                candidates.Add(towardsHigher
                    ? new LabelCandidate(new Point(end + LabelPad, centreY), label, font, TextAlignment.Left) { VerticalAlignment = TextVerticalAlignment.Center }
                    : new LabelCandidate(new Point(end - LabelPad, centreY), label, font, TextAlignment.Right) { VerticalAlignment = TextVerticalAlignment.Center });
            }
        }
        return candidates;
    }

    /// <inheritdoc />
    internal override void AddMarks(BarSeries series, MarkObstacles.Builder marks)
    {
        foreach (var pixel in PixelBars(series, Transform)) marks.AddBox(pixel.Bar);

        // A label is a text among the marks, where the label layout puts it.
        foreach (var placed in PlaceLabels(LabelCandidates(series), Area.PlotBounds))
        {
            marks.AddBox(new TextBox(new TextPlacement(placed.FinalPoint, placed.Alignment) { Vertical = placed.VerticalAlignment },
                Ctx.ExtentOf(TextLabel.Of(placed.Text), placed.Font)).Bounds());
        }
    }

    /// <inheritdoc />
    public override void Render(BarSeries series)
    {
        var baseColor = ColorOf(series);
        var fillColor = baseColor.WithOpacityGiven(series.Alpha);
        // Every bar is drawn in one style: its fill, its edge and its hatch; a gradient is laid under the hatch of each.
        var style = Context.Theme.PatchEdgesOf(series).StyleOf(fillColor, baseColor, series.Hatching);

        Color? tipColor = series.GradientColor is { } tip ? tip.ScaleAlpha(series.Alpha) : null;
        foreach (var pixel in PixelBars(series, Transform))
        {
            var barStyle = tipColor is { } end ? style with { Gradient = pixel.FootToEnd(series.Orientation, fillColor, end) } : style;
            // A bar is a rectangle of horizontal and vertical sides, which matplotlib's Agg puts on the pixel grid by its edge's width.
            Ctx.DrawSnappedRectangle(pixel.Bar, barStyle);
        }

        // The labels are placed in one batch after all the bars are drawn, so the layout can shift the few that would
        // otherwise stand on a neighbouring bar's label (dense category axes, narrow bars, long labels).
        DrawPlacedLabels(PlaceLabels(LabelCandidates(series), Area.PlotBounds));
    }

    /// <summary>Whether the axis a bar's value runs along (the Y axis of a vertical bar, the X axis of a horizontal one) runs
    /// backwards: its limits were set high to low, so a larger value stands lower (or further left) in the picture.</summary>
    /// <param name="orientation">The bar's orientation.</param>
    /// <returns><see langword="true"/> when the axis runs backwards.</returns>
    /// <param name="transform">The <see cref="DataTransform"/> it works on.</param>
    internal static bool ValueRunsBackwards(DataTransform transform, BarOrientation orientation)
    {
        var drawn = transform.DrawnRange;
        var values = drawn.Along(orientation.ValuesAlongX);
        return values.Hi < values.Lo;
    }

    /// <summary>The bars of the series that can be drawn, in order, as they stand under <paramref name="transform"/>.
    /// A bar stands on its foot and reaches its end. A foot the axis cannot place (0 on a log axis) is clipped to the
    /// edge where the axis begins, as matplotlib clips it; a bar whose end, position or missing foot the axes cannot
    /// place is left out (and its label with it). A rising bar's end is its top, a falling bar's its bottom.</summary>
    /// <param name="transform">The transform the bars are drawn with.</param>
    /// <returns>The bars that stand on the plot.</returns>
    /// <param name="series">The <see cref="BarSeries"/> it works on.</param>
    private static IEnumerable<PixelBar> PixelBars(BarSeries series, DataTransform transform)
    {
        for (int i = 0; i < series.Categories.Length; i++)
        {
            // Where the bar stands along its category axis: in its category's slot, or around its numeric x
            // (CategorySlot, the one rule the data range and the category labels read too).
            var slot = series.SlotOf(i);

            double baseline = series.Bottoms is { } bottoms ? bottoms[i] : 0;
            double value = series.Values[i];
            bool rising = value >= 0;
            var box = PixelBox.OnFoot(transform, slot, baseline, baseline + value, rising, series.Orientation);
            var pixel = new PixelBar(i, box.From, box.To, rising);
            if (pixel.Bar.IsDrawable()) yield return pixel;
        }
    }

    /// <summary>One bar as it stands on the plot in pixels.</summary>
    /// <param name="Index">The bar's position in the series: which category's value it is.</param>
    /// <param name="From">The corner at the data's lower-left of the bar (its left edge and the end of a rising bar; its left
    /// edge and the foot of a falling one, for a vertical bar), as the renderer computes it: the rectangle runs from here to
    /// <paramref name="To"/>.</param>
    /// <param name="To">The opposite corner.</param>
    /// <param name="Rising"><see langword="true"/> when the bar's value is zero or more.</param>
    private readonly record struct PixelBar(int Index, Point From, Point To, bool Rising)
    {
        /// <summary>The rectangle the bar fills: it keeps a positive size on an axis that runs backwards.</summary>
        internal Rect Bar => Rect.FromCorners(From, To);

        /// <summary>Where the end of a bar whose value runs along X (<paramref name="alongX"/>: it lies down) or along Y stands, as a pixel
        /// along that axis: the corner its value reaches, whichever way it points. <see cref="Rendering.SeriesRenderers.BarSeriesRenderer.PixelBar.From"/> and <see cref="Rendering.SeriesRenderers.BarSeriesRenderer.PixelBar.To"/> stand at the
        /// screen's start and end; the corner that holds the high value is the one <c>AtScreenEnd</c> names, and a rising bar ends there.</summary>
        /// <param name="alongX">Whether the bar's value runs along X.</param>
        /// <returns>The pixel coordinate of its end.</returns>
        internal double EndAlong(bool alongX) => Rising ? alongX.AtScreenEnd(From.CoordinateAlong(alongX), To.CoordinateAlong(alongX))
            : alongX.AtScreenStart(From.CoordinateAlong(alongX), To.CoordinateAlong(alongX));

        /// <summary>Where the foot of a bar stands along the axis its value runs along: the corner its value starts from
        /// (<see cref="Rendering.SeriesRenderers.BarSeriesRenderer.PixelBar.EndAlong"/>'s other).</summary>
        /// <param name="alongX">Whether the bar's value runs along X.</param>
        /// <returns>The pixel coordinate of its foot.</returns>
        internal double FootAlong(bool alongX) => Rising ? alongX.AtScreenStart(From.CoordinateAlong(alongX), To.CoordinateAlong(alongX))
            : alongX.AtScreenEnd(From.CoordinateAlong(alongX), To.CoordinateAlong(alongX));

        /// <summary>The blend that runs through the middle of the bar from its foot to its end: <paramref name="footColour"/>
        /// where the bar stands, <paramref name="endColour"/> where its value reaches. The end is the corner the value reaches
        /// (the top of a bar that points up, the bottom of one that points down), whichever way the axis runs.</summary>
        /// <param name="orientation">The axis the bar's value runs along.</param>
        /// <param name="footColour">The colour at the foot.</param>
        /// <param name="endColour">The colour at the end.</param>
        /// <returns>The gradient, in the pixels of the figure.</returns>
        internal LinearGradient FootToEnd(BarOrientation orientation, Color footColour, Color endColour)
        {
            bool alongX = orientation.ValuesAlongX;
            double middle = (From.CoordinateAcross(alongX) + To.CoordinateAcross(alongX)) / 2;
            return new LinearGradient(Point.Along(alongX, FootAlong(alongX), middle), Point.Along(alongX, EndAlong(alongX), middle), footColour, endColour);
        }
    }
}
