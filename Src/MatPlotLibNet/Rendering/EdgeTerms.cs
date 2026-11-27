// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Everything that differs between the four edges of the plot an axis hangs its ticks from, worked out once per edge
/// (<see cref="TickSide"/>): whether the edge runs along X, which way is out, where an edge stands beside the plot, which spine it
/// meets, how a tick label is anchored upright and turned, where a label and the offset text stand, how far a turned label reaches
/// away from it, which ink of a title faces its tick labels and which series list numbers the series of an axis stacked past the
/// primary on it. The tick drawing, the X axes, every Y axis slot and the title rules read these terms and never ask which edge they
/// are on, so the four edges are drawn as each other's mirror image.</summary>
internal sealed record EdgeTerms
{
    /// <summary>The bottom edge, the primary X axis' and a linked X axis' below the plot: out is down, a label hangs from the mark and
    /// the pad, a turned one from its end.</summary>
    internal static EdgeTerms Bottom { get; } = new()
    {
        Side = TickSide.Bottom,
        AlongX = true,
        Outward = 1,
        EdgeFraction = 1,
        Upright = TextAlignment.Center,
        Turned = TextAlignment.Right,
        LabelVertical = TextVerticalAlignment.Top,
        Ink = new FacingInk(FacingInk.Ascent, FacingInk.Descent),
        SpineEdge = SpineEdge.Bottom,
        OutwardPair = SeriesAxisPair.Primary,
        OffsetEnd = 1,
        OffsetAlignment = TextAlignment.Right,
        TitleWithoutTickLabels = BottomTitleWithoutTickLabels,
        OppositeSide = TickSide.Top,
        Title = EdgeTitle.Upright,
        ContainerSide = AxisSide.Bottom,
    };

    /// <summary>The top edge, the secondary X axis' and the mirrored X ticks': out is up, a label stands with its baseline at the
    /// mark and the pad, a turned one hangs from its start (the point reflection of a bottom label).</summary>
    internal static EdgeTerms Top { get; } = new()
    {
        Side = TickSide.Top,
        AlongX = true,
        Outward = -1,
        EdgeFraction = 0,
        Upright = TextAlignment.Center,
        Turned = TextAlignment.Left,
        // matplotlib sets the top X label on its baseline labelpad above the tick labels (axis.py: {'top': 'baseline'}, bbox.y1 +
        // labelpad): the baseline faces them, its descent dips into the gap.
        Ink = new FacingInk(0, FacingInk.Ascent, FacingInk.Descent),
        SpineEdge = SpineEdge.Top,
        OutwardPair = SeriesAxisPair.SecondaryX,
        OffsetEnd = 1,
        OffsetAlignment = TextAlignment.Right,
        OppositeSide = TickSide.Bottom,
        Title = EdgeTitle.Upright,
    };

    /// <summary>The left edge, the primary Y axis' and every left slot's: out is towards smaller x, a label ends at its
    /// anchor.</summary>
    internal static EdgeTerms Left { get; } = new()
    {
        Side = TickSide.Left,
        AlongX = false,
        Outward = -1,
        EdgeFraction = 0,
        Upright = TextAlignment.Right,
        Turned = TextAlignment.Right,
        LabelVertical = TextVerticalAlignment.Center,
        Ink = new FacingInk(FacingInk.TurnedSink, FacingInk.Ascent),
        SpineEdge = SpineEdge.Left,
        OutwardPair = SeriesAxisPair.SecondaryLeftY,
        OffsetEnd = 0,
        OffsetAlignment = TextAlignment.Left,
        TitleWithoutTickLabels = YTitleWithoutTickLabels,
        OppositeSide = TickSide.Right,
        Title = EdgeTitle.Turned,
        ContainerSide = AxisSide.Left,
    };

    /// <summary>The right edge, every right slot's and the mirrored Y ticks': out is towards larger x, a label starts at its
    /// anchor.</summary>
    internal static EdgeTerms Right { get; } = new()
    {
        Side = TickSide.Right,
        AlongX = false,
        Outward = 1,
        EdgeFraction = 1,
        Upright = TextAlignment.Left,
        Turned = TextAlignment.Left,
        LabelVertical = TextVerticalAlignment.Center,
        Ink = new FacingInk(FacingInk.TurnedRise, FacingInk.Descent),
        SpineEdge = SpineEdge.Right,
        OutwardPair = SeriesAxisPair.SecondaryY,
        OffsetEnd = 1,
        OffsetAlignment = TextAlignment.Right,
        TitleWithoutTickLabels = YTitleWithoutTickLabels,
        OppositeSide = TickSide.Left,
        Title = EdgeTitle.Turned,
        ContainerSide = AxisSide.Right,
    };


    /// <summary>Where the baseline of a horizontal title below a bottom edge stands when no tick label is drawn there, in pixels from
    /// the edge.</summary>
    private const double BottomTitleWithoutTickLabels = 35;

    /// <summary>Where the anchor line of a turned title beside a left or a right edge stands when no tick label is drawn there, in
    /// pixels from the edge.</summary>
    private const double YTitleWithoutTickLabels = 45;

    /// <summary>The terms of <paramref name="side"/>: the one place an edge is turned into how it is drawn.</summary>
    /// <param name="side">The edge.</param>
    /// <returns>Its terms.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="side"/> is no edge.</exception>
    internal static EdgeTerms Of(TickSide side) => side switch
    {
        TickSide.Bottom => Bottom,
        TickSide.Top => Top,
        TickSide.Left => Left,
        TickSide.Right => Right,
        _ => throw side.NotAMember(nameof(side), "Not an edge of the plot."),
    };

    /// <summary>The edge past a strip that runs along a direction, where a colour bar hangs its marks: the foot of one that runs along
    /// X, the right side of one that runs along Y.</summary>
    /// <param name="alongX">Whether the strip runs along X.</param>
    /// <returns>The terms of that edge.</returns>
    internal static EdgeTerms Past(bool alongX) => alongX ? Bottom : Right;

    /// <summary>The terms of the edge the axes of <paramref name="side"/> of the container stand on.</summary>
    /// <param name="side">The side of the container.</param>
    /// <returns>Its terms.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="side"/> is no side of the container.</exception>
    internal static EdgeTerms Of(AxisSide side) => side switch
    {
        AxisSide.Left => Left,
        AxisSide.Right => Right,
        AxisSide.Bottom => Bottom,
        _ => throw side.NotAMember(nameof(side), "Not a side of the Y axis container."),
    };

    /// <summary>The edge these terms are of.</summary>
    internal TickSide Side { get; private init; }

    /// <summary>Whether the edge runs along X: a top or a bottom edge.</summary>
    internal bool AlongX { get; private init; }

    /// <summary>Which way is out, away from the plot, along the coordinate across the edge: +1 where it grows away from the plot
    /// (down below a bottom edge, rightward beside a right one), -1 otherwise.</summary>
    internal double Outward { get; private init; }

    /// <summary>Where the plot's own edge on this side stands, as a fraction of the plot's extent across the edge from its start:
    /// 0 on the left and at the top, 1 on the right and at the bottom.</summary>
    internal double EdgeFraction { get; private init; }

    /// <summary>How an upright tick label is anchored at its point.</summary>
    internal TextAlignment Upright { get; private init; }

    /// <summary>How a turned tick label is anchored at its point, so it grows away from the plot.</summary>
    internal TextAlignment Turned { get; private init; }

    /// <summary>How a tick label stands on its anchor, as matplotlib's tick labels stand (<c>va</c>): hung from it below a bottom edge
    /// ('top'), on its baseline above a top edge ('baseline'), centred on its tick beside a left or a right edge. The box
    /// (<see cref="TextBox"/>) turns it into the baseline the label is drawn on.</summary>
    internal TextVerticalAlignment LabelVertical { get; private init; }

    /// <summary>The ink of a title beside this edge: the part of its font size that faces the tick labels and the part that reaches
    /// away from them (<see cref="FacingInk"/>).</summary>
    internal FacingInk Ink { get; private init; }

    /// <summary>The edge whose spine an axis on this side meets.</summary>
    internal SpineEdge SpineEdge { get; private init; }

    /// <summary>The series list that numbers the series of the axes stacked past the primary on this edge
    /// (<see cref="SeriesSlotExtensions.SlotIndex"/>); a linked axis below the plot holds none, the bottom edge names the primary's.</summary>
    internal SeriesAxisPair OutwardPair { get; private init; }

    /// <summary>Where along the plot's width the offset text of an axis on this edge stands, as a fraction from its left: at the
    /// right end, except beside a left edge.</summary>
    internal double OffsetEnd { get; private init; }

    /// <summary>How the offset text is anchored there.</summary>
    internal TextAlignment OffsetAlignment { get; private init; }

    /// <summary>Where an axis title beside this edge stands when no tick label is drawn there (hidden ticks, axis-less series), in pixels
    /// from the edge (<see cref="TitleClearance.Beside"/>): 35 below a bottom edge, 45 beside a left or a right one; <see langword="null"/>
    /// above a top edge, where the title keeps the gap from the edge itself.</summary>
    internal double? TitleWithoutTickLabels { get; private init; }

    /// <summary>The edge across the plot from this one: where the axis on this edge mirrors its ticks, and where a theme that marks every
    /// side marks them.</summary>
    internal EdgeTerms Opposite => Of(OppositeSide);

    /// <summary>The side across the plot from this edge.</summary>
    private TickSide OppositeSide { get; init; }

    /// <summary>How an axis title stands beside this edge: upright across a top or a bottom edge, turned a quarter beside a left or a
    /// right one (<see cref="EdgeTitle"/>).</summary>
    internal EdgeTitle Title { get; private init; } = EdgeTitle.Upright;

    /// <summary>The side of the Y axis container whose axes stand on this edge (<see cref="Of(AxisSide)"/> read back): the left, the right
    /// or the bottom; <see langword="null"/> for the top edge, where the top X axis stands, which is no axis of the container.</summary>
    internal AxisSide? ContainerSide { get; private init; }

    /// <summary>Draws the title of <paramref name="axis"/> beside this edge by the edge's title rule (<see cref="Title"/>): clear of tick
    /// labels <paramref name="tallestTickLabel"/> past <paramref name="edgeLine"/>, its style's pad on top.</summary>
    /// <param name="ctx">The surface drawn on.</param>
    /// <param name="axes">The axes the axis belongs to: the Y axis container's gap.</param>
    /// <param name="axis">The axis whose title is drawn.</param>
    /// <param name="theme">The active theme.</param>
    /// <param name="plot">The plot area: the title is centred along it.</param>
    /// <param name="edgeLine">The coordinate across the edge the axis' ticks hang from.</param>
    /// <param name="tallestTickLabel">How far the tick labels drawn there reach past the mark and the pad; 0 when none was drawn.</param>
    internal void DrawTitle(IRenderContext ctx, Axes axes, Axis axis, Theme theme, Rect plot, double edgeLine, double tallestTickLabel) =>
        Title.Draw(ctx, axes, axis, this, theme, plot, edgeLine, tallestTickLabel);

    /// <summary>How a tick label drawn at <paramref name="rotation"/> degrees is anchored at its point: upright or turned.</summary>
    /// <param name="rotation">The rotation the label is drawn at.</param>
    /// <returns>The alignment.</returns>
    internal TextAlignment AlignmentFor(double rotation) => rotation == 0 ? Upright : Turned;

    /// <summary>The coordinate across the edge (a y on a top or a bottom edge, an x on a left or a right one) of an edge standing
    /// <paramref name="offset"/> pixels out past the plot's edge on this side.</summary>
    /// <param name="plot">The plot area.</param>
    /// <param name="offset">How far out the edge stands: 0 for the plot's own edge.</param>
    /// <returns>The coordinate, in pixels.</returns>
    internal double EdgeAt(Rect plot, double offset) =>
        plot.StartAlong(!AlongX) + EdgeFraction * plot.LengthAlong(!AlongX) + Outward * offset;

    /// <summary>The spine on this edge, read from the axes' spines: the one a mark meets half a stroke into.</summary>
    /// <param name="spines">The axes' spines.</param>
    /// <returns>The spine.</returns>
    internal SpineConfig SpineOf(SpinesConfig spines) => spines.On(SpineEdge);

    /// <summary>Where the spine on this edge stands across it: on the plot's edge, or where it was moved to (a data value, a fraction
    /// of the axes).</summary>
    /// <param name="spines">The axes' spines.</param>
    /// <param name="plot">The plot area.</param>
    /// <param name="transform">The transform a spine moved to a data value is placed with.</param>
    /// <returns>The coordinate, in pixels.</returns>
    internal double SpineAt(SpinesConfig spines, Rect plot, DataTransform transform) =>
        SpineOf(spines).PlacedAcross(AlongX, plot, EdgeAt(plot, 0), transform);

    /// <summary>How far the spine on this edge stands in from the plot's edge (<see cref="SpineAt"/>): 0 on the edge, less than 0 moved
    /// out past it, more moved into the plot.</summary>
    /// <param name="spines">The axes' spines.</param>
    /// <param name="plot">The plot area.</param>
    /// <param name="transform">The transform a spine moved to a data value is placed with.</param>
    /// <returns>The inset, in pixels.</returns>
    internal double InsetOf(SpinesConfig spines, Rect plot, DataTransform transform) =>
        Outward * (EdgeAt(plot, 0) - SpineAt(spines, plot, transform));

    /// <summary>A tick drawing turned to face this edge: its side, and its labels anchored for the rotation they are drawn at.</summary>
    /// <param name="draw">How the ticks read (formatter, fonts, rotation).</param>
    /// <returns>The drawing, facing this edge.</returns>
    internal TickDrawContext Facing(TickDrawContext draw) => draw with
    {
        Side = Side,
        Alignment = AlignmentFor(draw.LabelRotation),
    };

    /// <summary>A tick drawing hung from an edge on this side: facing it (<see cref="Facing"/>), at <paramref name="edge"/>, half the
    /// stroke of the spine on this side in.</summary>
    /// <param name="draw">How the ticks read (formatter, fonts, rotation).</param>
    /// <param name="edge">The coordinate across the edge the ticks hang from.</param>
    /// <param name="spines">The axes' spines.</param>
    /// <param name="theme">The theme the spine is drawn under, which says how wide a spine is that was given no width.</param>
    /// <returns>The drawing, hung from that edge.</returns>
    internal TickDrawContext Hang(TickDrawContext draw, double edge, SpinesConfig spines, Theme theme) => draw with
    {
        Side = Side,
        Alignment = AlignmentFor(draw.LabelRotation),
        AxisEdge = edge,
        SpineHalf = SpineOf(spines).HalfWidthIn(theme),
    };

    /// <summary>Where a tick label is anchored: <paramref name="reach"/> (the mark's part outside the axes and the pad) out from the edge,
    /// level with its tick; it stands on that point by <see cref="LabelVertical"/>.</summary>
    /// <param name="along">Where the tick stands along the edge.</param>
    /// <param name="edge">The coordinate across the edge the ticks hang from.</param>
    /// <param name="reach">How far out from the edge the label stands.</param>
    /// <returns>The label's anchor.</returns>
    internal Point LabelAnchor(double along, double edge, double reach) =>
        Point.Along(AlongX, along, edge + Outward * reach);

    /// <summary>Where the offset text of an axis on this edge stands (matplotlib's <c>ScalarFormatter</c> offset text,
    /// <c>OFFSETTEXTPAD</c> past what it is written beside): past the tick label band and the pad, at the plot's right end, on a top
    /// or a bottom edge; the pad above the plot's top, at its end on this side, on a left or a right one.</summary>
    /// <param name="plot">The plot area.</param>
    /// <param name="edge">The coordinate across the edge the ticks hang from.</param>
    /// <param name="band">From the edge to the far side of the tick labels.</param>
    /// <param name="pad">The pad, in pixels.</param>
    /// <returns>Where the text stands, how it is anchored there and how it stands on it: as the edge's labels below or above the
    /// plot, on its baseline above the plot's top beside a left or a right edge.</returns>
    internal TextPlacement OffsetTextAt(Rect plot, double edge, double band, double pad) => AlongX
        ? new(new Point(plot.X + OffsetEnd * plot.Width, edge + Outward * band + Outward * pad), OffsetAlignment) { Vertical = LabelVertical }
        : new(new Point(plot.X + OffsetEnd * plot.Width, plot.Y - pad), OffsetAlignment);

    /// <summary>How far a turned label reaches away from this edge, past the mark and the pad: on a top or a bottom edge its height and
    /// what the turn carries its far side past where it stands upright; on a left or a right edge the far side of the turned box.</summary>
    /// <param name="turned">The label's box turned by its rotation about its anchor, at the origin.</param>
    /// <param name="upright">The label's box upright about its anchor, at the origin.</param>
    /// <param name="size">The label's measured extent, upright.</param>
    /// <returns>The reach, in pixels.</returns>
    internal double TurnedAway(Rect turned, Rect upright, TextExtent size) =>
        AlongX ? size.ExtentAcross(AlongX) + Outward * (FarSide(turned) - FarSide(upright)) : Outward * FarSide(turned);

    /// <summary>The side of a box across this edge that faces away from the plot: its bottom below a bottom edge, its top above a
    /// top edge, its left beside a left edge, its right beside a right edge.</summary>
    private double FarSide(Rect box) => Outward > 0 ? box.EndAlong(!AlongX) : box.StartAlong(!AlongX);
}
