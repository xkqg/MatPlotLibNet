// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.CartesianParts;

/// <summary>Draws the mark of every axis break on a Cartesian axes, on top of the series, the X axis' breaks first, then the Y axis'.
/// An axis cut to nothing (the library's own themes) gets, at the start of each break, the gap erased in the axes background across the
/// whole plot, then the zigzag or straight strokes in the foreground colour, each break in the order it was added; a break styled
/// <see cref="BreakStyle.None"/> and a break with no place on its axis draw nothing. An axis drawn in parts (<see cref="AxisBreakMap.IsDrawnInParts"/>:
/// a theme whose subplots stand a share apart, the matplotlib themes) is drawn as matplotlib's broken-axis recipe draws it: the room
/// between two parts is the figure's ground, nothing of the axes in it, and each part carries the break's shape at the two corners of its
/// cut end (the recipe's diagonal for <see cref="BreakStyle.Straight"/>).</summary>
/// <remarks>The renderer makes this part only when the axes has a break, so a frame without breaks makes no object.</remarks>
internal sealed class CartesianBreakMarkersPart : CartesianAxesPart
{
    private const double MarkerHalfSize = 6.0;
    private const double MarkerThickness = 1.5;
    private const double GapHalf = 4.0;

    /// <summary>How far a mark at a corner of a part reaches each way, as a share of the part: the recipe's <c>d = .015</c> of the axes
    /// (matplotlib's gallery <c>broken_axis</c>: <c>ax.plot((-d, +d), (-d, +d), transform=ax.transAxes)</c>).</summary>
    private const double CornerReach = 0.015;

    /// <summary>A spine as the theme draws it: the colour the recipe's marks continue the spines in (<c>axes.edgecolor</c>).</summary>
    private static readonly SpineConfig ThemeSpine = new();

    /// <summary>The shape a straight break draws at a corner, in the corner's box (-1 to 1 each way, Y up): the recipe's diagonal.</summary>
    private static readonly Point[] Diagonal = [new(-1, -1), new(1, 1)];

    /// <summary>The shape a zigzag break draws at a corner, in the corner's box: three strokes across it.</summary>
    private static readonly Point[] Zigzag = [new(-1, -1), new(-1.0 / 3, 1), new(1.0 / 3, -1), new(1, 1)];

    /// <summary>The colour of the ground the rooms of an axis drawn in parts show.</summary>
    private readonly Color _ground;

    /// <summary>Constructs the part for one render.</summary>
    /// <param name="context">What the render draws with: the axes, the plot area, the surface, the theme and the primary transform.</param>
    /// <param name="ground">The figure's ground (<see cref="Figure.Ground"/>), which the room between two parts shows.</param>
    internal CartesianBreakMarkersPart(in CartesianPartContext context, Color ground)
        : base(context) => _ground = ground;

    /// <inheritdoc />
    public override void Render()
    {
        Render(Axes.XBreaks, EdgeTerms.Bottom);
        Render(Axes.YBreaks, EdgeTerms.Left);
    }

    /// <summary>The marks of one axis' <paramref name="breaks"/>: in parts where the transform draws the axis in parts, the library's
    /// marks otherwise.</summary>
    private void Render(IReadOnlyList<AxisBreak> breaks, EdgeTerms edge)
    {
        if (Transform.BreaksAlong(edge.AlongX) is { IsDrawnInParts: true } map) RenderParts(breaks, map, edge.AlongX);
        else RenderBreakMarkers(breaks, edge);
    }

    /// <summary>An axis drawn in parts: each room painted in the figure's ground across the plot, then, for a break that is styled, its
    /// shape at both corners of each part's cut end, <see cref="CornerReach"/> of the part along the axis and of the plot across it, in the
    /// spine colour and as wide as a line (<c>lines.linewidth</c>).</summary>
    private void RenderParts(IReadOnlyList<AxisBreak> breaks, AxisBreakMap map, bool isX)
    {
        double acrossStart = PlotArea.StartAlong(!isX), acrossLength = PlotArea.LengthAlong(!isX);
        var rooms = map.Rooms;
        var parts = map.Parts;
        for (int i = 0; i < rooms.Length; i++)
        {
            var room = Transform.RoomAlong(isX, i);
            Ctx.DrawRectangle(Rect.Along(isX, room.Lo, room.Hi - room.Lo, acrossStart, acrossLength), ShapeStyle.Filled(_ground));
        }

        var stroke = StrokeStyle.Solid(ThemeSpine.StrokeIn(Theme).Color, Theme.Looks.Lines.WidthOf(null, LineWidthRule.Line));
        for (int i = 0; i < rooms.Length; i++)
        {
            if (ShapeOf(StyleAt(breaks, rooms[i].Lo)) is not { } shape) continue;
            DrawCorners(shape, isX, Transform.PixelAlong(rooms[i].Lo, isX), LengthOf(parts[i], isX), acrossStart, acrossLength, stroke);
            DrawCorners(shape, isX, Transform.PixelAlong(rooms[i].Hi, isX), LengthOf(parts[i + 1], isX), acrossStart, acrossLength, stroke);
        }
    }

    /// <summary>The shape a break of <paramref name="style"/> draws at a corner; <see langword="null"/> for none.</summary>
    private static Point[]? ShapeOf(BreakStyle style) => style switch
    {
        BreakStyle.Straight => Diagonal,
        BreakStyle.Zigzag => Zigzag,
        _ => null,
    };

    /// <summary>The style of the break the cut beginning at <paramref name="from"/> starts with: of overlapping breaks merged into one
    /// cut, the one that begins it.</summary>
    private static BreakStyle StyleAt(IReadOnlyList<AxisBreak> breaks, double from)
    {
        foreach (var b in breaks)
            if (b.From == from) return b.Style;
        return AxisBreak.RestingStyle;
    }

    /// <summary>The length a part takes along the axis, in pixels.</summary>
    private double LengthOf(Range1D part, bool isX) => Math.Abs(Transform.PixelAlong(part.Hi, isX) - Transform.PixelAlong(part.Lo, isX));

    /// <summary><paramref name="shape"/> at both corners of a part's cut end <paramref name="at"/> along the axis: on either side of the
    /// plot across it, in a box reaching <see cref="CornerReach"/> of the part along the axis and of the plot across it, Y up.</summary>
    private void DrawCorners(Point[] shape, bool isX, double at, double partLength, double acrossStart, double acrossLength, StrokeStyle stroke)
    {
        var reach = Point.Along(isX, CornerReach * partLength, CornerReach * acrossLength);
        for (int side = 0; side < 2; side++)
        {
            var corner = Point.Along(isX, at, acrossStart + side * acrossLength);
            for (int k = 1; k < shape.Length; k++)
                Ctx.DrawLine(InBox(corner, reach, shape[k - 1]), InBox(corner, reach, shape[k]), stroke);
        }

        static Point InBox(Point corner, Point reach, Point unit) => new(corner.X + unit.X * reach.X, corner.Y - unit.Y * reach.Y);
    }

    /// <summary>Draws the mark of each of <paramref name="breaks"/> at the pixel the transform puts the break's start at: the
    /// place the data jumps, worked out by the one rule that places every value on an axis with breaks
    /// (<see cref="DataTransform.DataToPixel"/>), on a log or symlog axis too. A start the plot does not hold
    /// (<see cref="TickRun.PlaceOnPlot"/>: inside another break, 0 on a log axis, or beyond the range) draws no mark.</summary>
    /// <param name="breaks">The breaks of one axis.</param>
    /// <param name="edge">The edge that axis' ticks stand on: the bottom one for the X axis, the left one for the Y axis.</param>
    private void RenderBreakMarkers(IReadOnlyList<AxisBreak> breaks, EdgeTerms edge)
    {
        var run = new TickRun([], Transform, edge);
        bool isXAxis = edge.AlongX;
        for (int i = 0; i < breaks.Count; i++)
        {
            var b = breaks[i];
            if (b.Style == BreakStyle.None) continue;
            if (run.PlaceOnPlot(b.From) is not { } at) continue;
            // Across the whole plot: from its top to its bottom for a break along X, its left to its right for one along Y.
            DrawAxisBreakMark(b.Style, at, PlotArea.StartAlong(!isXAxis), PlotArea.EndAlong(!isXAxis), alongX: isXAxis);
        }
    }

    /// <summary>One mark at <paramref name="pos"/>: the gap erased in the axes background from <paramref name="edgeA"/> to
    /// <paramref name="edgeB"/>, then the strokes of <paramref name="style"/> in the foreground colour; <paramref name="alongX"/> for a
    /// break of the X axis, at a place along X.</summary>
    private void DrawAxisBreakMark(BreakStyle style, double pos, double edgeA, double edgeB, bool alongX)
    {
        var gapFill = ShapeStyle.Filled(Theme.AxesBackground);
        Ctx.DrawRectangle(Rect.Along(alongX, pos - GapHalf, GapHalf * 2, edgeA, edgeB - edgeA), gapFill);

        DrawBreakSegments(
            Orientation.Along(alongX),
            crossStart: edgeA, crossEnd: edgeB,
            perpPos: pos, halfSize: MarkerHalfSize,
            thickness: MarkerThickness, lineColor: Theme.ForegroundText,
            style: style);
    }

    /// <summary>Draws the strokes of an axis break marker: a zigzag is two strokes that meet at the middle of the extent, a
    /// straight mark two parallel slanted strokes, the same on either axis turned a quarter; <see cref="BreakStyle.None"/>
    /// draws none. The gap erasure in the axes background is done by the caller (<see cref="DrawAxisBreakMark"/>).
    /// <paramref name="crossStart"/>/<paramref name="crossEnd"/> span the axis extent (e.g. PlotArea.Y to PlotArea.Y+Height for horizontal);
    /// <paramref name="perpPos"/> is the break position on the perpendicular axis.</summary>
    internal void DrawBreakSegments(
        Orientation orientation,
        double crossStart, double crossEnd,
        double perpPos, double halfSize,
        double thickness, Color lineColor,
        BreakStyle style)
    {
        double span = crossEnd - crossStart;
        // The break's place runs along X for a horizontal mark (a break of the X axis), along Y otherwise; its strokes span across.
        bool alongX = orientation.RunsAlongX;
        Point At(double along, double across) => Point.Along(alongX, along, across);

        if (style == BreakStyle.Zigzag)
        {
            Ctx.DrawLine(At(perpPos - halfSize, crossStart + span * 0.4),
                At(perpPos, crossStart + span * 0.5), StrokeStyle.Solid(lineColor, thickness));
            Ctx.DrawLine(At(perpPos, crossStart + span * 0.5),
                At(perpPos + halfSize, crossStart + span * 0.6), StrokeStyle.Solid(lineColor, thickness));
        }
        else if (style == BreakStyle.Straight)
        {
            // Each stroke runs across a span halfSize long centred on the break, the second one moved along by 0.6 of it.
            var across = Range1D.Around(perpPos, halfSize);
            Ctx.DrawLine(At(across.Lo, crossStart + span * 0.3),
                At(across.Hi, crossStart + span * 0.7),
                StrokeStyle.Solid(lineColor, thickness));
            Ctx.DrawLine(At(across.Lo + halfSize * 0.6, crossStart + span * 0.3),
                At(across.Hi + halfSize * 0.6, crossStart + span * 0.7),
                StrokeStyle.Solid(lineColor, thickness));
        }
    }
}
