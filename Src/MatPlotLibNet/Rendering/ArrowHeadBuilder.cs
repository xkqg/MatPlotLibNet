// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>Builds arrowhead geometry (polygon vertices or path segments) for annotation arrows.</summary>
internal static class ArrowHeadBuilder
{
    /// <summary>Returns a polygon (filled shape) for solid arrowhead styles.
    /// Returns an empty list for styles that use open/line arrowheads or no arrowhead.</summary>
    public static IReadOnlyList<Point> BuildPolygon(Point tip, double ux, double uy,
        ArrowStyle style, double size = 8)
    {
        double nx = -uy, ny = ux; // perpendicular unit vector

        return style switch
        {
            ArrowStyle.FancyArrow => BuildFancyArrow(tip, ux, uy, nx, ny, size, halfWidth: size * 0.5),
            ArrowStyle.Wedge      => BuildFancyArrow(tip, ux, uy, nx, ny, size, halfWidth: size),
            _                     => Array.Empty<Point>(),
        };
    }

    /// <summary>Returns path segments for open/line arrowhead styles.
    /// Returns <c>null</c> for styles handled by <see cref="BuildPolygon(Point, double, double, ArrowStyle, double)"/> or with no head.</summary>
    public static IReadOnlyList<PathSegment>? BuildPath(Point tip, double ux, double uy,
        ArrowStyle style, double size = 8)
    {
        double nx = -uy, ny = ux;

        return style switch
        {
            ArrowStyle.CurveA   => BuildCurveHead(tip, ux, uy, nx, ny, size),
            ArrowStyle.CurveB   => BuildCurveHead(tip, ux, uy, nx, ny, size),
            ArrowStyle.CurveAB  => BuildDoubleCurveHead(tip, ux, uy, nx, ny, size),
            ArrowStyle.BracketA => BuildBracket(tip, nx, ny, size),
            ArrowStyle.BracketB => BuildBracket(tip, nx, ny, size),
            ArrowStyle.BracketAB => BuildDoubleBracket(tip, nx, ny, size),
            _                   => null,
        };
    }

    /// <summary>Returns the polygon of a solid head at <paramref name="tip"/> for a line that comes from
    /// <paramref name="from"/>, the direction worked out here. A line of no length has no direction and no head.</summary>
    public static IReadOnlyList<Point> BuildPolygon(Point from, Point tip, ArrowStyle style, double size = 8) =>
        Direction(from, tip) is { } unit ? BuildPolygon(tip, unit.X, unit.Y, style, size) : Array.Empty<Point>();

    /// <summary>Returns the path of an open head at <paramref name="tip"/> for a line that comes from
    /// <paramref name="from"/>; <c>null</c> for a style with another kind of head and for a line of no length.</summary>
    public static IReadOnlyList<PathSegment>? BuildPath(Point from, Point tip, ArrowStyle style, double size = 8) =>
        Direction(from, tip) is { } unit ? BuildPath(tip, unit.X, unit.Y, style, size) : null;

    // --- Private helpers ---

    /// <summary>A direction of length one.</summary>
    private readonly record struct Unit(double X, double Y);

    /// <summary>The unit direction from <paramref name="from"/> to <paramref name="tip"/>, the one place it is worked out;
    /// <see langword="null"/> when the two are the same point (or so close that there is no direction to speak of).</summary>
    private static Unit? Direction(Point from, Point tip)
    {
        double dx = tip.X - from.X, dy = tip.Y - from.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        return length < 1e-9 ? null : new Unit(dx / length, dy / length);
    }

    private static IReadOnlyList<Point> BuildFancyArrow(Point tip,
        double ux, double uy, double nx, double ny, double len, double halfWidth)
    {
        var left  = new Point(tip.X - ux * len + nx * halfWidth, tip.Y - uy * len + ny * halfWidth);
        var right = new Point(tip.X - ux * len - nx * halfWidth, tip.Y - uy * len - ny * halfWidth);
        return [tip, left, right];
    }

    private static IReadOnlyList<PathSegment> BuildCurveHead(Point tip,
        double ux, double uy, double nx, double ny, double size)
    {
        // Open curved arrowhead: two short curved lines fanning out from the tip
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

    private static IReadOnlyList<PathSegment> BuildDoubleCurveHead(Point tip,
        double ux, double uy, double nx, double ny, double size)
    {
        // Same as CurveB plus a mirrored head at the source end (tip - ux*size*2 is approx the other end)
        var head1 = BuildCurveHead(tip, ux, uy, nx, ny, size);
        var src   = new Point(tip.X - ux * size * 2, tip.Y - uy * size * 2);
        var head2 = BuildCurveHead(src, -ux, -uy, nx, ny, size);
        return [.. head1, .. head2];
    }

    private static IReadOnlyList<PathSegment> BuildBracket(Point tip,
        double nx, double ny, double size)
    {
        // Perpendicular line at the tip: tip - perpendicular*size/2 → tip + perpendicular*size/2
        var p1 = new Point(tip.X - nx * size * 0.5, tip.Y - ny * size * 0.5);
        var p2 = new Point(tip.X + nx * size * 0.5, tip.Y + ny * size * 0.5);
        return [new MoveToSegment(p1), new LineToSegment(p2)];
    }

    private static IReadOnlyList<PathSegment> BuildDoubleBracket(Point tip,
        double nx, double ny, double size)
    {
        var b1 = BuildBracket(tip, nx, ny, size);
        // Second bracket is offset along the arrow direction — caller provides the far end tip
        var farTip = new Point(tip.X + nx * size, tip.Y + ny * size);
        var b2 = BuildBracket(farTip, nx, ny, size);
        return [.. b1, .. b2];
    }
}
