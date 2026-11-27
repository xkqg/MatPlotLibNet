// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="RadarSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class RadarSeriesRenderer : SeriesRenderer<RadarSeries>
{
    /// <inheritdoc />
    public RadarSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(RadarSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(RadarSeries series)
    {
        var color = ColorOf(series);
        var fillColor = series.FillColor ?? color.WithOpacityGiven(series.Alpha);
        int n = series.Categories.Length;
        if (n < 3) return;
        var bounds = Area.PlotBounds;
        var frame = RoundFrame.In(bounds, RoundFrame.WebShare);
        double cx = frame.Centre.X, cy = frame.Centre.Y;
        double radius = frame.Radius;
        var theme = Context.Theme;
        // Under the matplotlib themes the radars of one axes share one web on one scale; the first draws the web, the last the frame.
        var share = ShareOf(series, Context.AxesSeries, theme);
        double maxVal = share.OuterValue;

        List<Point> Ring(double frac)
        {
            var points = new List<Point>(n);
            for (int i = 0; i < n; i++) { double a = 2 * Math.PI * i / n - Math.PI / 2; points.Add(new Point(cx, cy).AtAngle(radius * frac, a)); }
            return points;
        }

        // matplotlib's radar is a polar axes, and an axes draws its patch in axes.facecolor, behind everything else: here the polygon of
        // the web's outermost ring. A theme that leaves an axis-less axes unpainted (the classic theme: its figure is grey) leaves the web
        // without it, so the patch is drawn there; every other theme has painted the rectangle behind the web already.
        if (share.DrawsWeb && theme.Looks.Frame.NoBackgroundWithoutAxes)
            Ctx.DrawPolygon(Ring(1.0), ShapeStyle.Filled(theme.AxesBackground));

        // The web. Under the library's own themes the rings stand at fifths of the axis, unlabelled, in a light grey. Under the matplotlib
        // themes (the ones that name a tick locator) they stand where a polar axes rings itself: at the ticks of the radial axis'
        // locator over the span from 0 to the outer ring, labelled beside the first spoke, in the theme's grid, which the spokes wear too.
        bool ringsAtTicks = theme.Looks.Ticks.Ladder is not null;
        if (share.DrawsWeb)
        {
            var web = ringsAtTicks ? RadialScale.Over(new Range1D(0, maxVal), null, radius, theme.DataTextFont().Size, theme) : (RadialScale?)null;
            DrawWeb(series, web, maxVal, cx, cy, radius, Ring);
        }

        // A value that cannot be drawn (missing, or infinite: no ring holds it) is a skipped vertex, and the polygon closes
        // through the spokes on either side of it; with no value that can be drawn there is no polygon.
        var dataPts = new List<Point>(n);
        for (int i = 0; i < n; i++)
        {
            if (!series.Values[i].IsDrawable()) continue;
            double norm = Math.Min(series.Values[i] / maxVal, 1.0); double a = 2 * Math.PI * i / n - Math.PI / 2; dataPts.Add(new Point(cx, cy).AtAngle(radius * norm, a));
        }
        if (dataPts.Count > 0) Ctx.DrawPolygon(dataPts, ShapeStyle.Edged(fillColor, color, series.LineWidth));

        // The frame is the outer ring drawn once more over the data, in the style of a spine as a polar axes' circle is (matplotlib's
        // spines['polar']): the colour and width its theme gives a spine. The library's own themes draw no frame of their own.
        if (ringsAtTicks && share.DrawsFrame)
        {
            var spine = new SpineConfig().StrokeIn(theme);
            Ctx.DrawPolygon(Ring(1.0), ShapeStyle.Outlined(spine.Color, spine.Thickness));
        }
    }

    /// <summary>The web under the data: the rings (at the ticks of <paramref name="web"/> and labelled, or at fifths when the theme
    /// names no locator), then a spoke and a name for each category.</summary>
    private void DrawWeb(RadarSeries series, RadialScale? web, double maxVal, double cx, double cy, double radius, Func<double, List<Point>> ring)
    {
        var theme = Context.Theme;
        int n = series.Categories.Length;
        var webStroke = (web is null ? GridStyle.AtRest : theme.DefaultGrid).Stroke;
        if (web is { } scale)
        {
            var tickFont = theme.TickFont();
            for (int i = 0; i < scale.Rings.Length; i++)
            {
                double frac = scale.Rings[i] / maxVal;
                Ctx.DrawPolygonStroke(ring(frac), webStroke);
                // The label stands where the ring crosses the ray the polar axes labels along (rlabel_position, 22.5 degrees past the first
                // spoke, the way the categories run), a little above and right of that point, as the polar axes writes it.
                var onTheRing = LabelPoint(cx, cy, radius * frac, n);
                Ctx.Draw(TextLabel.Of(scale.Labels[i]), tickFont, new TextPlacement(new Point(onTheRing.X + 3, onTheRing.Y - 3), TextAlignment.Left));
            }
        }
        else
        {
            for (int r = 1; r <= 5; r++)
                Ctx.DrawPolygonStroke(ring(r / 5.0), webStroke);
        }

        var labelFont = theme.DataTextFont();
        for (int i = 0; i < n; i++)
        {
            double a = 2 * Math.PI * i / n - Math.PI / 2;
            Ctx.DrawLine(new Point(cx, cy), new Point(cx, cy).AtAngle(radius, a), webStroke);
            Ctx.Draw(TextLabel.Of(series.Categories[i]), labelFont, new TextPlacement(new Point(cx, cy).AtAngle(radius + 15, a), TextAlignment.Center) { Vertical = TextVerticalAlignment.Center });
        }
    }

    /// <summary>Where the ray that labels the rings (<see cref="RadialScale.LabelAngleDegrees"/> past the first spoke) crosses the polygon ring
    /// of circumradius <paramref name="ring"/>: on the edge between two vertices, at <c>apothem / cos(angle - the edge's middle)</c>.</summary>
    private static Point LabelPoint(double cx, double cy, double ring, int sides)
    {
        double sector = 2 * Math.PI / sides, past = RadialScale.LabelAngleDegrees.ToRadians();
        double reach = ring * Math.Cos(sector / 2) / Math.Cos(past % sector - sector / 2);
        double a = past - Math.PI / 2;
        return new Point(cx, cy).AtAngle(reach, a);
    }


    /// <summary>What one radar series draws of the web the radars of its axes share. Under the matplotlib themes the radars of one axes
    /// stand on one polar axes, which has one radial axis: one web, on one scale, the largest outer value of the radars of the axes, drawn
    /// by the first visible radar under the data, and one frame drawn by the last visible radar over it. Under the library's own themes
    /// every radar draws its own web on its own outer value, as it always has.</summary>
    /// <param name="OuterValue">The value the outer ring stands for.</param>
    /// <param name="DrawsWeb">Whether this series draws the web: the axes' patch, the rings and their labels, the spokes and the names.</param>
    /// <param name="DrawsFrame">Whether this series draws the frame over the data.</param>
    internal readonly record struct RadarShare(double OuterValue, bool DrawsWeb, bool DrawsFrame);

    /// <summary>The share of <paramref name="series"/> among the series of its axes.</summary>
    /// <param name="series">The radar being drawn.</param>
    /// <param name="axesSeries">The series of its axes in drawing order; <see langword="null"/> for a radar drawn on its own.</param>
    /// <param name="theme">The theme of the figure: one that names a tick locator (the matplotlib themes) shares the web.</param>
    /// <returns>The share.</returns>
    internal static RadarShare ShareOf(RadarSeries series, IReadOnlyList<ISeries>? axesSeries, Theme theme)
    {
        double own = series.OuterValue;
        if (theme.Looks.Ticks.Ladder is null || axesSeries is null)
            return new(own, DrawsWeb: true, DrawsFrame: true);

        double outer = own;
        int first = -1, last = -1, self = -1;
        for (int i = 0; i < axesSeries.Count; i++)
        {
            if (axesSeries[i] is not RadarSeries radar) continue;
            outer = Math.Max(outer, radar.OuterValue);
            if (ReferenceEquals(radar, series)) self = i;
            if (!radar.Visible) continue;
            if (first < 0) first = i;
            last = i;
        }

        return self < 0 ? new(own, DrawsWeb: true, DrawsFrame: true) : new(outer, self == first, self == last);
    }
}
