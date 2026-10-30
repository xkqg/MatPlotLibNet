// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="ChordSeries"/>: a ring of arcs with the ribbons inside it, centred in the plot area, and the
/// names of the groups outside the ring. Each arc is one closed path on the ring; each ribbon is one closed path that leaves the
/// ring, bends through the centre and arrives at the other group (two quadratic curves through the centre, raised exactly to
/// the cubics a path carries), filled in the colour of the group that sends more of the two, a little transparent so that the
/// ones behind it show, with a darker thin edge. The geometry is <see cref="ChordLayout"/>'s.</summary>
internal sealed class ChordSeriesRenderer : SeriesRenderer<ChordSeries>
{
    /// <summary>How thick the ring of arcs is, as a share of its outer radius.</summary>
    private const double RingThickness = 0.07;

    /// <summary>The room, in pixels, between the rim and the name of a group.</summary>
    private const double NameGap = 6;

    /// <inheritdoc />
    public ChordSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(ChordSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(ChordSeries series)
    {
        var labelFont = ThemedFontProvider.LabelFont(Context.Theme);
        var bounds = Area.PlotBounds;
        var centre = new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

        // The names stand outside the ring, so the ring leaves the widest of them (and a line of text above and below) room.
        double room = Math.Max(series.Names.Where(name => !string.IsNullOrEmpty(name)).Select(name => Ctx.MeasureText(name, labelFont).Width).DefaultIfEmpty(0).Max() + NameGap + 2,
            labelFont.Size * 2);
        double outer = Math.Min(bounds.Width, bounds.Height) / 2 - room;
        if (!(outer > labelFont.Size)) return;

        double inner = outer * (1 - RingThickness);
        var layout = series.Layout();

        foreach (var arc in layout.Arcs)
        {
            if (arc.End > arc.Start)
                Ctx.DrawPath(ArcPath(centre, outer, inner, arc), new ShapeStyle(PartColor(series.Colors, arc.Group), null, 0));
        }

        foreach (var ribbon in layout.Ribbons)
        {
            var colour = PartColor(series.Colors, ribbon.Source);
            var fill = ApplyAlpha(colour, series.RibbonAlpha);
            Ctx.DrawPath(RibbonPath(centre, inner, ribbon), new ShapeStyle(fill, Darker(colour), 0.5));
        }

        foreach (var arc in layout.Arcs)
        {
            string name = series.Names[arc.Group];
            if (string.IsNullOrEmpty(name)) continue;

            var label = arc.Label();
            Ctx.DrawText(name, At(centre, outer + NameGap, label.Angle), labelFont, label.Alignment, label.Rotation);
        }
    }

    /// <summary>The point at <paramref name="radius"/> from the centre at an angle counted clockwise from twelve o'clock.</summary>
    private static Point At(Point centre, double radius, double angle) =>
        new(centre.X + radius * Math.Sin(angle), centre.Y - radius * Math.Cos(angle));

    /// <summary>An angle in the convention of <see cref="ArcSegment"/>: degrees from the +X axis, clockwise on a screen whose Y
    /// points down, so twelve o'clock is -90.</summary>
    private static double Degrees(double angle) => angle * 180 / Math.PI - 90;

    private static IReadOnlyList<PathSegment> ArcPath(Point centre, double outer, double inner, ChordArc arc) =>
    [
        new MoveToSegment(At(centre, outer, arc.Start)),
        new ArcSegment(centre, outer, outer, Degrees(arc.Start), Degrees(arc.End)),
        new LineToSegment(At(centre, inner, arc.End)),
        new ArcSegment(centre, inner, inner, Degrees(arc.End), Degrees(arc.Start)),
        new CloseSegment(),
    ];

    private static IReadOnlyList<PathSegment> RibbonPath(Point centre, double radius, ChordRibbon ribbon)
    {
        var segments = new List<PathSegment> { new MoveToSegment(At(centre, radius, ribbon.SourceSpan.Start)) };
        AddEnd(segments, centre, radius, ribbon.SourceSpan);

        if (ribbon.Source == ribbon.Target)
        {
            // A loop: its one end on the ring, and one curve through the centre back to where it began.
            segments.Add(ThroughTheCentre(At(centre, radius, ribbon.SourceSpan.End), centre, At(centre, radius, ribbon.SourceSpan.Start)));
        }
        else
        {
            segments.Add(ThroughTheCentre(At(centre, radius, ribbon.SourceSpan.End), centre, At(centre, radius, ribbon.TargetSpan.Start)));
            AddEnd(segments, centre, radius, ribbon.TargetSpan);
            segments.Add(ThroughTheCentre(At(centre, radius, ribbon.TargetSpan.End), centre, At(centre, radius, ribbon.SourceSpan.Start)));
        }

        segments.Add(new CloseSegment());
        return segments;
    }

    /// <summary>The end of a ribbon along the ring; an end of no width is a point and adds nothing.</summary>
    private static void AddEnd(List<PathSegment> segments, Point centre, double radius, ChordSpan span)
    {
        if (span.End > span.Start)
            segments.Add(new ArcSegment(centre, radius, radius, Degrees(span.Start), Degrees(span.End)));
    }

    /// <summary>The quadratic curve from <paramref name="from"/> to <paramref name="to"/> with its control point at the centre,
    /// raised exactly to the cubic a path carries: each control point two thirds of the way from its end to the centre.</summary>
    private static BezierSegment ThroughTheCentre(Point from, Point centre, Point to) => new(
        new Point(from.X + 2.0 / 3 * (centre.X - from.X), from.Y + 2.0 / 3 * (centre.Y - from.Y)),
        new Point(to.X + 2.0 / 3 * (centre.X - to.X), to.Y + 2.0 / 3 * (centre.Y - to.Y)),
        to);

    /// <summary>The edge of a ribbon: its colour, a third darker.</summary>
    private static Color Darker(Color colour) =>
        new((byte)(colour.R * 0.67), (byte)(colour.G * 0.67), (byte)(colour.B * 0.67));
}
