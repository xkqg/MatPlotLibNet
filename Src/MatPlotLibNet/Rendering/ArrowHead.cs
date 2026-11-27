// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The ends of a line a head stands at: where the line begins (an annotation's text), where it points to (its target), or both.</summary>
[Flags]
internal enum ArrowEnds
{
    /// <summary>Where the line begins.</summary>
    Source = 1,

    /// <summary>Where the line points to.</summary>
    Target = 2,

    /// <summary>Both ends.</summary>
    Both = Source | Target,
}

/// <summary>The head an arrow ends in, for annotation arrows and the edges of a network graph: one sealed leaf for each kind of
/// <see cref="ArrowStyle"/>, chosen in one place (<see cref="Of"/>), that draws itself at one end of a line, as matplotlib's
/// <c>&lt;-</c>, <c>-&gt;</c> and <c>&lt;-&gt;</c> do. A solid head is a filled polygon, an open head a path stroked one pixel wide;
/// <see cref="ArrowStyle.None"/> and <see cref="ArrowStyle.Simple"/> have none. The open head of the library's own arrows, two
/// strokes from the tip back along the line in the width of the arrow's own stroke, is a <see cref="BarbsArrowHead"/>
/// (<see cref="OpenHead"/>, <see cref="OpenHeadMirrored"/>, <see cref="QuiverHead"/>); no <see cref="ArrowStyle"/> names it.</summary>
internal abstract class ArrowHead
{
    private static readonly NoArrowHead NoHead = new();
    private static readonly SolidArrowHead Fancy = new(halfWidthShare: 0.5);
    private static readonly SolidArrowHead WedgeHead = new(halfWidthShare: 1.0);
    private static readonly CurveArrowHead CurveAtSource = new(ArrowEnds.Source);
    private static readonly CurveArrowHead CurveAtTarget = new(ArrowEnds.Target);
    private static readonly CurveArrowHead CurveAtBoth = new(ArrowEnds.Both);
    private static readonly BracketArrowHead BracketAtSource = new(ArrowEnds.Source);
    private static readonly BracketArrowHead BracketAtTarget = new(ArrowEnds.Target);
    private static readonly BracketArrowHead BracketAtBoth = new(ArrowEnds.Both);

    /// <summary>The length of the barbs of the library's open head for an arrow size of 1, in pixels.</summary>
    internal const double OpenHeadLength = 6;

    /// <summary>How far each barb of the library's open head turns from the way back along the line, in radians (about 23 degrees).</summary>
    private const double OpenHeadTurn = 0.4;

    /// <summary>How far from the direction an arrow points in each barb of a scaled quiver arrow's head stands, in radians.</summary>
    private const double QuiverBarbAngle = 2.5;

    /// <summary>The library's open head (a streamplot's arrow): two barbs, the one turned the smaller way from the line's way back
    /// drawn first. Its length is <see cref="OpenHeadLength"/> for an arrow size of 1.</summary>
    internal static readonly ArrowHead OpenHead = new BarbsArrowHead(-OpenHeadTurn);

    /// <summary>The library's open head with its two barbs in the other order (a 3-D quiver's arrow): the picture is the same, and the
    /// markup of a picture drawn with it before it shared the head does not move.</summary>
    internal static readonly ArrowHead OpenHeadMirrored = new BarbsArrowHead(OpenHeadTurn);

    /// <summary>The head of a scaled quiver arrow: barbs <see cref="QuiverBarbAngle"/> radians off the direction the arrow points in,
    /// as long as the series' share of the shaft.</summary>
    internal static readonly ArrowHead QuiverHead = new BarbsArrowHead(QuiverBarbAngle - Math.PI);

    /// <summary>The head of <paramref name="style"/>: the one place an arrow style is mapped to its head, the same instance on
    /// every call. A style with no head, or one the enum does not declare, draws nothing.</summary>
    /// <param name="style">The style an annotation asks for.</param>
    /// <returns>The head of the style; one that draws nothing for a style with no head.</returns>
    internal static ArrowHead Of(ArrowStyle style) => style switch
    {
        ArrowStyle.FancyArrow => Fancy,
        ArrowStyle.Wedge => WedgeHead,
        ArrowStyle.CurveA => CurveAtSource,
        ArrowStyle.CurveB => CurveAtTarget,
        ArrowStyle.CurveAB => CurveAtBoth,
        ArrowStyle.BracketA => BracketAtSource,
        ArrowStyle.BracketB => BracketAtTarget,
        ArrowStyle.BracketAB => BracketAtBoth,
        _ => NoHead,
    };

    /// <summary>The ends of the line the head stands at.</summary>
    internal virtual ArrowEnds Ends => ArrowEnds.Target;

    /// <summary>Draws the head at each end of the line from <paramref name="from"/> to <paramref name="tip"/> it stands at, the
    /// direction worked out here, once: at <paramref name="tip"/> pointing along the line, at <paramref name="from"/> pointing
    /// back along it, or at both. A line of no length has no direction and no head.</summary>
    /// <param name="ctx">The context to draw on.</param>
    /// <param name="from">Where the line begins.</param>
    /// <param name="tip">Where the line points to.</param>
    /// <param name="size">The length of the head.</param>
    /// <param name="color">The colour of the head; the head is as wide as one pixel.</param>
    internal void Draw(IRenderContext ctx, Point from, Point tip, double size, Color color) =>
        Draw(ctx, from, tip, size, StrokeStyle.Solid(color, LineWidthRule.OwnPixel.Resting));

    /// <summary>Draws the head in the stroke of the line it finishes, as <see cref="Draw(IRenderContext, Point, Point, double, Color)"/>
    /// draws it in a colour: a filled head in the stroke's colour, a path head and the barbs in its colour and width.</summary>
    /// <param name="ctx">The context to draw on.</param>
    /// <param name="from">Where the line begins.</param>
    /// <param name="tip">Where the line points to.</param>
    /// <param name="size">The length of the head.</param>
    /// <param name="ink">The stroke the head is drawn in.</param>
    internal void Draw(IRenderContext ctx, Point from, Point tip, double size, StrokeStyle ink)
    {
        var heading = from.HeadingTo(tip);
        if (heading.Length < 1e-9) return;
        double ux = heading.Ux, uy = heading.Uy;
        if ((Ends & ArrowEnds.Target) != 0) DrawAt(ctx, tip, ux, uy, size, ink);
        if ((Ends & ArrowEnds.Source) != 0) DrawAt(ctx, from, -ux, -uy, size, ink);
    }

    /// <summary>Draws the head at <paramref name="tip"/> for the unit direction (<paramref name="ux"/>, <paramref name="uy"/>)
    /// the line runs in, towards the tip.</summary>
    private protected abstract void DrawAt(IRenderContext ctx, Point tip, double ux, double uy, double size, StrokeStyle ink);
}

/// <summary>No head: the line ends at its tip.</summary>
internal sealed class NoArrowHead : ArrowHead
{
    private protected override void DrawAt(IRenderContext ctx, Point tip, double ux, double uy, double size, StrokeStyle ink) { }
}

/// <summary>A filled triangle with its point at the tip, as long as the size and as wide on each side as
/// <c>halfWidthShare</c> of it (FancyArrow a half, Wedge the whole).</summary>
/// <param name="halfWidthShare">How wide the head is on each side of the line, as a share of its length.</param>
internal sealed class SolidArrowHead(double halfWidthShare) : ArrowHead
{
    /// <summary>The corners of the head, the tip first.</summary>
    /// <param name="tip">Where the head points to.</param>
    /// <param name="ux">X of the unit direction the line runs in, towards the tip.</param>
    /// <param name="uy">Y of the unit direction the line runs in, towards the tip.</param>
    /// <param name="size">The length of the head.</param>
    internal IReadOnlyList<Point> Polygon(Point tip, double ux, double uy, double size)
    {
        double nx = -uy, ny = ux; // perpendicular unit vector
        double halfWidth = size * halfWidthShare;
        var left  = new Point(tip.X - ux * size + nx * halfWidth, tip.Y - uy * size + ny * halfWidth);
        var right = new Point(tip.X - ux * size - nx * halfWidth, tip.Y - uy * size - ny * halfWidth);
        return [tip, left, right];
    }

    private protected override void DrawAt(IRenderContext ctx, Point tip, double ux, double uy, double size, StrokeStyle ink) =>
        ctx.DrawPolygon(Polygon(tip, ux, uy, size), ShapeStyle.Filled(ink.Color));
}

/// <summary>An open head: a path stroked one pixel wide in the colour.</summary>
internal abstract class PathArrowHead : ArrowHead
{
    /// <summary>The path of the head.</summary>
    /// <param name="tip">Where the head points to.</param>
    /// <param name="ux">X of the unit direction the line runs in, towards the tip.</param>
    /// <param name="uy">Y of the unit direction the line runs in, towards the tip.</param>
    /// <param name="size">The length of the head.</param>
    internal abstract IReadOnlyList<PathSegment> Path(Point tip, double ux, double uy, double size);

    private protected sealed override void DrawAt(IRenderContext ctx, Point tip, double ux, double uy, double size, StrokeStyle ink) =>
        ctx.DrawPath(Path(tip, ux, uy, size), ShapeStyle.Outlined(ink.Color, ink.Thickness));
}

/// <summary>An open head of two strokes from the tip back along the line, in the stroke the line is drawn in: the first barb turned
/// by <c>firstTurn</c> from the way back, the second by the same turn the other way. The barbs reach <c>size</c> from the tip. The
/// library's own arrows (a streamplot's, a quiver's, a 3-D quiver's) wear it.</summary>
/// <param name="firstTurn">How far the first barb turns from the way back along the line, in radians; the second turns by its negative.</param>
internal sealed class BarbsArrowHead(double firstTurn) : ArrowHead
{
    private readonly double _cos = Math.Cos(firstTurn), _sin = Math.Sin(firstTurn);

    private protected override void DrawAt(IRenderContext ctx, Point tip, double ux, double uy, double size, StrokeStyle ink)
    {
        ctx.DrawLine(tip, new Point(tip.X - size * (ux * _cos - uy * _sin), tip.Y - size * (uy * _cos + ux * _sin)), ink);
        ctx.DrawLine(tip, new Point(tip.X - size * (ux * _cos + uy * _sin), tip.Y - size * (uy * _cos - ux * _sin)), ink);
    }
}

/// <summary>An open curved head: two short curves fanning out from the tip.</summary>
/// <param name="ends">The ends of the line the head stands at.</param>
internal sealed class CurveArrowHead(ArrowEnds ends) : PathArrowHead
{
    /// <inheritdoc />
    internal override ArrowEnds Ends => ends;

    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Path(Point tip, double ux, double uy, double size)
    {
        double nx = -uy, ny = ux;
        var left  = new Point(tip.X - ux * size + nx * size * 0.5, tip.Y - uy * size + ny * size * 0.5);
        var right = new Point(tip.X - ux * size - nx * size * 0.5, tip.Y - uy * size - ny * size * 0.5);
        var ctrl  = new Point(tip.X - ux * size * 0.3, tip.Y - uy * size * 0.3);
        return
        [
            new MoveToSegment(left),
            new BezierSegment(ctrl, ctrl, tip),
            new MoveToSegment(tip),
            new BezierSegment(ctrl, ctrl, right),
        ];
    }
}

/// <summary>A bracket: one line across the tip, perpendicular to the line, as long as the size.</summary>
/// <param name="ends">The ends of the line the bracket stands at.</param>
internal sealed class BracketArrowHead(ArrowEnds ends) : PathArrowHead
{
    /// <inheritdoc />
    internal override ArrowEnds Ends => ends;

    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Path(Point tip, double ux, double uy, double size)
    {
        double nx = -uy, ny = ux;
        var p1 = new Point(tip.X - nx * size * 0.5, tip.Y - ny * size * 0.5);
        var p2 = new Point(tip.X + nx * size * 0.5, tip.Y + ny * size * 0.5);
        return [new MoveToSegment(p1), new LineToSegment(p2)];
    }
}
