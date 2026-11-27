// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering;

/// <summary>The line an annotation draws from its text to its target: one sealed leaf for each <see cref="ConnectionStyle"/>,
/// chosen in one place (<see cref="Of"/>).</summary>
internal abstract class ConnectionPath
{
    private static readonly StraightConnection Straight = new();
    private static readonly Arc3Connection Arc3 = new();
    private static readonly AngleConnection Angle = new();
    private static readonly Angle3Connection Angle3 = new();

    /// <summary>The path of <paramref name="style"/>: the one place a connection style is mapped to its path, the same instance
    /// on every call. A style the enum does not declare is drawn straight.</summary>
    /// <param name="style">The style an annotation asks for.</param>
    internal static ConnectionPath Of(ConnectionStyle style) => style switch
    {
        ConnectionStyle.Arc3 => Arc3,
        ConnectionStyle.Angle => Angle,
        ConnectionStyle.Angle3 => Angle3,
        _ => Straight,
    };

    /// <summary>The path from <paramref name="from"/> to <paramref name="to"/>.</summary>
    /// <param name="from">Where the connection begins.</param>
    /// <param name="to">Where the connection ends.</param>
    /// <param name="rad">How far an arc bulges, or how round a smoothed corner is.</param>
    internal abstract IReadOnlyList<PathSegment> Between(Point from, Point to, double rad = Annotation.RestingConnectionRad);
}

/// <summary>A straight line from the text to the target.</summary>
internal sealed class StraightConnection : ConnectionPath
{
    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Between(Point from, Point to, double rad = Annotation.RestingConnectionRad) =>
        [new MoveToSegment(from), new LineToSegment(to)];
}

/// <summary>A curve whose one control point stands off the midpoint, perpendicular to the line, by <c>rad</c> of its length.</summary>
internal sealed class Arc3Connection : ConnectionPath
{
    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Between(Point from, Point to, double rad = Annotation.RestingConnectionRad)
    {
        var heading = from.HeadingTo(to);

        // Perpendicular unit vector (rotate 90° counter-clockwise)
        var perp = heading.LeftNormal;

        double offset = rad * heading.Length;
        double midX = (from.X + to.X) / 2 + perp.X * offset;
        double midY = (from.Y + to.Y) / 2 + perp.Y * offset;

        // Use symmetric control points at the offset midpoint for a smooth arc
        var ctrl = new Point(midX, midY);
        return [new MoveToSegment(from), new BezierSegment(ctrl, ctrl, to)];
    }
}

/// <summary>A right angle: along X first, then along Y to the target.</summary>
internal sealed class AngleConnection : ConnectionPath
{
    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Between(Point from, Point to, double rad = Annotation.RestingConnectionRad)
    {
        // Horizontal-then-vertical: corner at (to.X, from.Y)
        var corner = new Point(to.X, from.Y);
        return [new MoveToSegment(from), new LineToSegment(corner), new LineToSegment(to)];
    }
}

/// <summary>A right angle with a rounded corner: two curves meeting at the corner, as round as <c>rad</c> (held to 0.05–0.5) of
/// the shorter leg.</summary>
internal sealed class Angle3Connection : ConnectionPath
{
    /// <inheritdoc />
    internal override IReadOnlyList<PathSegment> Between(Point from, Point to, double rad = Annotation.RestingConnectionRad)
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
