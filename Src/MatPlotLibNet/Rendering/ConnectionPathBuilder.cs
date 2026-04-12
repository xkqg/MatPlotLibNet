// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>Builds the SVG path segments for annotation connection lines.</summary>
internal static class ConnectionPathBuilder
{
    /// <summary>Builds the path from <paramref name="from"/> to <paramref name="to"/> using the specified style.</summary>
    public static IReadOnlyList<PathSegment> BuildPath(Point from, Point to,
        ConnectionStyle style, double rad = 0.3) => style switch
    {
        ConnectionStyle.Arc3   => BuildArc3(from, to, rad),
        ConnectionStyle.Angle  => BuildAngle(from, to),
        ConnectionStyle.Angle3 => BuildAngle3(from, to, rad),
        _                      => BuildStraight(from, to),
    };

    private static IReadOnlyList<PathSegment> BuildStraight(Point from, Point to) =>
        [new MoveToSegment(from), new LineToSegment(to)];

    private static IReadOnlyList<PathSegment> BuildArc3(Point from, Point to, double rad)
    {
        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double len = Math.Sqrt(dx * dx + dy * dy);

        // Perpendicular unit vector (rotate 90° counter-clockwise)
        double px = -dy, py = dx;
        if (len > 0) { px /= len; py /= len; }

        double offset = rad * len;
        double midX = (from.X + to.X) / 2 + px * offset;
        double midY = (from.Y + to.Y) / 2 + py * offset;

        // Use symmetric control points at the offset midpoint for a smooth arc
        var ctrl = new Point(midX, midY);
        return [new MoveToSegment(from), new BezierSegment(ctrl, ctrl, to)];
    }

    private static IReadOnlyList<PathSegment> BuildAngle(Point from, Point to)
    {
        // Horizontal-then-vertical: corner at (to.X, from.Y)
        var corner = new Point(to.X, from.Y);
        return [new MoveToSegment(from), new LineToSegment(corner), new LineToSegment(to)];
    }

    private static IReadOnlyList<PathSegment> BuildAngle3(Point from, Point to, double rad)
    {
        // Smoothed right-angle: two Bezier curves meeting at the corner
        var corner = new Point(to.X, from.Y);
        double bx = Math.Min(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y)) * Math.Clamp(rad, 0.05, 0.5);

        // First bezier: from → approach corner along horizontal
        var ctrl1a = new Point(corner.X - Math.Sign(to.X - from.X) * bx, from.Y);
        var mid = new Point(corner.X - Math.Sign(to.X - from.X) * bx * 0.5,
                            corner.Y + Math.Sign(to.Y - from.Y) * bx * 0.5);

        // Second bezier: leave corner along vertical
        var ctrl2b = new Point(corner.X, corner.Y + Math.Sign(to.Y - from.Y) * bx);

        return
        [
            new MoveToSegment(from),
            new BezierSegment(from, ctrl1a, mid),
            new BezierSegment(ctrl2b, to, to),
        ];
    }
}
