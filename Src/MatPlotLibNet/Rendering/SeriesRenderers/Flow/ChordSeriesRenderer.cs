// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="ChordSeries"/>: a ring of arcs with the ribbons inside it, centred in the plot area, and the
/// names of the groups outside the ring. Each arc is one closed path, the ring wedge between its two bearings
/// (<see cref="RingWedge"/>); each ribbon is one closed path that leaves the ring, bends through the centre and arrives at the other group (two quadratic curves through the centre, raised exactly to
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
        var labelFont = Context.Theme.LabelFont();
        var bounds = Area.PlotBounds;

        // The names stand outside the ring, so the ring leaves the widest of them (and a line of text above and below) room.
        double room = Math.Max(series.Names.Where(name => !string.IsNullOrEmpty(name)).Select(name => Ctx.ExtentOf(TextLabel.Of(name), labelFont).Width).DefaultIfEmpty(0).Max() + NameGap + 2,
            labelFont.Size * 2);
        var frame = RoundFrame.In(bounds, RoundFrame.FullShare, room);
        var centre = frame.Centre;
        double outer = frame.Radius;
        if (!(outer > labelFont.Size)) return;

        double inner = outer * (1 - RingThickness);
        var layout = series.Layout();

        foreach (var arc in layout.Arcs)
        {
            if (arc.End > arc.Start)
                Ctx.DrawPath(ArcPath(centre, outer, inner, arc), ShapeStyle.Filled(PartColor(series.Colors, arc.Group)));
        }

        foreach (var ribbon in layout.Ribbons)
        {
            var colour = PartColor(series.Colors, ribbon.Source);
            var fill = colour.WithOpacityGiven(series.RibbonAlpha);
            Ctx.DrawPath(RibbonPath(centre, inner, ribbon), ShapeStyle.Edged(fill, Darker(colour), Slot.LineWidthOf(LineWidthRule.OwnHairline)));
        }

        foreach (var arc in layout.Arcs)
        {
            string name = series.Names[arc.Group];
            if (string.IsNullOrEmpty(name)) continue;

            var label = Label(arc);
            Ctx.Draw(TextLabel.Of(name), labelFont, new TextPlacement(At(centre, outer + NameGap, label.Angle), label.Alignment) { Rotation = label.Rotation });
        }
    }

    /// <summary>The point at <paramref name="radius"/> from the centre at an angle counted clockwise from twelve o'clock.</summary>
    private static Point At(Point centre, double radius, double angle) =>
        centre.AtBearing(radius, angle);

    /// <summary>The outline of one group's arc: the ring wedge between its two bearings.</summary>
    private static IReadOnlyList<PathSegment> ArcPath(Point centre, double outer, double inner, ChordArc arc) =>
        RingWedge.FromBearings(centre, inner, outer, arc.Start, arc.End).Path();

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
            segments.Add(RingWedge.FromBearings(centre, 0, radius, span.Start, span.End).OuterArc());
    }

    /// <summary>The quadratic curve from <paramref name="from"/> to <paramref name="to"/> with its control point at the centre,
    /// raised exactly to the cubic a path carries: each control point two thirds of the way from its end to the centre.</summary>
    private static BezierSegment ThroughTheCentre(Point from, Point centre, Point to) => new(
        new Point(from.X + 2.0 / 3 * (centre.X - from.X), from.Y + 2.0 / 3 * (centre.Y - from.Y)),
        new Point(to.X + 2.0 / 3 * (centre.X - to.X), to.Y + 2.0 / 3 * (centre.Y - to.Y)),
        to);

    /// <summary>The edge of a ribbon: its colour, a third darker, and opaque whatever the ribbon's own opacity is.</summary>
    private static Color Darker(Color colour) =>
        colour.Modulate(0.67).WithAlpha(byte.MaxValue);

    /// <summary>Where the label of this arc stands and how it is turned: aligned to start at the rim on the right half of the
    /// circle and to end there on the left half, turned by the angle less a quarter turn (and a half turn more on the left
    /// half), so that it reads outward and no label is ever upside down.</summary>
    /// <returns>The angle of the middle of the arc, the alignment and the turn in degrees.</returns>
    /// <param name="arc">The <see cref="ChordArc"/> it works on.</param>
    internal static ChordLabel Label(ChordArc arc)
    {
        double mid = arc.Mid;
        bool onTheLeft = mid > Math.PI;
        double rotation = mid.ToDegrees() - 90 + (onTheLeft ? 180 : 0);
        rotation = ((rotation + 180) % 360 + 360) % 360 - 180;
        return new ChordLabel(mid, onTheLeft ? TextAlignment.Right : TextAlignment.Left, rotation);
    }

    /// <summary>Where the label of an arc stands: the angle of its middle, how it is aligned at that point, and how far it is turned
    /// so that it reads outward from the rim and is never upside down.</summary>
    /// <param name="Angle">The middle of the arc, in radians clockwise from twelve o'clock.</param>
    /// <param name="Alignment">Left on the right half of the circle (the text starts at the rim), right on the left half (it ends there).</param>
    /// <param name="Rotation">The turn in degrees, within -90 and 90.</param>
    internal readonly record struct ChordLabel(double Angle, TextAlignment Alignment, double Rotation);
}
