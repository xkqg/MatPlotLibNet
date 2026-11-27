// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>A slice of a ring (or of a disc, when the inner radius is zero) between two angles: the shape a donut and a sunburst
/// and a chord diagram's ring draw as arcs (<see cref="Path"/>, <see cref="OuterArc"/>), and a pie's slice, a dial's band and a polar heat map's cell draw as a polyline
/// (<see cref="Polygon"/>, <see cref="PolygonPath"/>) at one resolution (<see cref="StepsFor"/>).</summary>
/// <remarks>Angles follow the standard math convention: the point at <c>angle</c> is (cx + r·cos(angle), cy + r·sin(angle)), so
/// they run clockwise on a screen whose Y points down. A chart that lays its angles counter-clockwise, in radians, from the right
/// (a pie, a donut, a dial, a polar axes) makes its wedge with <see cref="CounterClockwise"/>.</remarks>
/// <param name="Cx">Circle centre X.</param>
/// <param name="Cy">Circle centre Y.</param>
/// <param name="InnerRadius">Inner radius; zero (or less) for a solid slice.</param>
/// <param name="OuterRadius">Outer radius.</param>
/// <param name="StartAngleDegrees">Start angle in degrees.</param>
/// <param name="EndAngleDegrees">End angle in degrees.</param>
internal readonly record struct RingWedge(
    double Cx, double Cy,
    double InnerRadius, double OuterRadius,
    double StartAngleDegrees, double EndAngleDegrees)
{
    /// <summary>The steps a full turn of a polyline arc is cut into: 120 gives a smooth circle at typical sizes. The one resolution every
    /// sector drawn as a polyline has, whichever chart draws it.</summary>
    internal const int StepsPerTurn = 120;

    /// <summary>The fewest steps an arc is cut into, however small its sweep.</summary>
    internal const int FewestSteps = 3;

    /// <summary>How far over a whole number of steps a sweep may come out before it takes another: the share of a turn a sweep is
    /// is a division and a multiplication of angles that were themselves worked out, so a slice of exactly a thirtieth of a turn
    /// arrives a rounding error over four steps and must not draw five.</summary>
    private const double StepTolerance = 1e-9;

    /// <summary>The centre of the circle.</summary>
    private Point Centre => new(Cx, Cy);

    /// <summary>The wedge of a chart that lays its angles in radians counter-clockwise from the right with Y up, as a pie, a donut, a
    /// dial and a polar axes do: the angles negated into the wedge's clockwise degrees.</summary>
    /// <param name="centre">The centre of the circle, in pixels.</param>
    /// <param name="innerRadius">Inner radius; zero (or less) for a solid slice.</param>
    /// <param name="outerRadius">Outer radius.</param>
    /// <param name="startRadians">The start angle, in radians.</param>
    /// <param name="endRadians">The end angle, in radians.</param>
    /// <returns>The wedge.</returns>
    internal static RingWedge CounterClockwise(Point centre, double innerRadius, double outerRadius, double startRadians, double endRadians) =>
        new(centre.X, centre.Y, innerRadius, outerRadius, (-startRadians).ToDegrees(), (-endRadians).ToDegrees());

    /// <summary>The steps an arc of <paramref name="sweepDegrees"/> is cut into: its share of a turn at <see cref="StepsPerTurn"/>,
    /// rounded up (within <see cref="StepTolerance"/>), never fewer than <see cref="FewestSteps"/>.</summary>
    /// <param name="sweepDegrees">How far the arc turns, in degrees; either way round.</param>
    /// <returns>The number of steps.</returns>
    internal static int StepsFor(double sweepDegrees) =>
        Math.Max(FewestSteps, (int)Math.Ceiling(Math.Abs(sweepDegrees) / 360 * StepsPerTurn - StepTolerance));

    /// <summary>The outline as a polyline: a solid slice is the centre and then the outer arc from the start angle to the end angle; a
    /// ring wedge is the outer arc and then the inner arc back from the end angle to the start. The arc is cut into
    /// <see cref="StepsFor"/> of its sweep.</summary>
    /// <returns>The corners, in drawing order; not closed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IReadOnlyList<Point> Polygon()
    {
        var centre = Centre;
        double start = StartAngleDegrees.ToRadians(), sweep = (EndAngleDegrees - StartAngleDegrees).ToRadians();
        int steps = StepsFor(EndAngleDegrees - StartAngleDegrees);
        bool solid = InnerRadius <= 0;
        var points = new Point[solid ? steps + 2 : 2 * (steps + 1)];
        int at = 0;
        if (solid) points[at++] = centre;
        for (int step = 0; step <= steps; step++) points[at++] = Rim(centre, OuterRadius, start, sweep, step, steps);
        if (!solid)
        {
            for (int step = steps; step >= 0; step--) points[at++] = Rim(centre, InnerRadius, start, sweep, step, steps);
        }
        return points;
    }

    /// <summary>The polyline of <see cref="Polygon"/> as the segments of a closed path: a move to its first corner, a line to each
    /// of the others, and the close. Built corner by corner, with no array of corners made on the way.</summary>
    /// <returns>The segments.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public IReadOnlyList<PathSegment> PolygonPath()
    {
        var centre = Centre;
        double start = StartAngleDegrees.ToRadians(), sweep = (EndAngleDegrees - StartAngleDegrees).ToRadians();
        int steps = StepsFor(EndAngleDegrees - StartAngleDegrees);
        bool solid = InnerRadius <= 0;
        var segments = new PathSegment[(solid ? steps + 2 : 2 * (steps + 1)) + 1];
        int at = 0;
        // The path begins at its first corner: the centre of a solid slice, else the start of the outer arc.
        if (solid)
        {
            segments[at++] = new MoveToSegment(centre);
            segments[at++] = new LineToSegment(Rim(centre, OuterRadius, start, sweep, 0, steps));
        }
        else
        {
            segments[at++] = new MoveToSegment(Rim(centre, OuterRadius, start, sweep, 0, steps));
        }
        for (int step = 1; step <= steps; step++) segments[at++] = new LineToSegment(Rim(centre, OuterRadius, start, sweep, step, steps));
        if (!solid)
        {
            for (int step = steps; step >= 0; step--) segments[at++] = new LineToSegment(Rim(centre, InnerRadius, start, sweep, step, steps));
        }
        segments[at] = new CloseSegment();
        return segments;
    }

    /// <summary>The point of an arc at <paramref name="radius"/> after <paramref name="step"/> of <paramref name="steps"/> steps from
    /// <paramref name="start"/>, an arc that turns <paramref name="sweep"/> radians.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Point Rim(Point centre, double radius, double start, double sweep, int step, int steps) =>
        centre.OnRim(radius, radius, start + sweep * step / steps);

    /// <summary>The wedge of a chart that lays its angles clockwise from twelve o'clock, in radians (a chord diagram's ring): each angle
    /// is that many radians round from the top, which is -90 degrees in this wedge's convention.</summary>
    /// <param name="centre">The centre of the circle, in pixels.</param>
    /// <param name="innerRadius">Inner radius; zero (or less) for a solid slice.</param>
    /// <param name="outerRadius">Outer radius.</param>
    /// <param name="startBearing">The start angle, in radians clockwise from twelve o'clock.</param>
    /// <param name="endBearing">The end angle, in radians clockwise from twelve o'clock.</param>
    /// <returns>The wedge.</returns>
    internal static RingWedge FromBearings(Point centre, double innerRadius, double outerRadius, double startBearing, double endBearing) =>
        new(centre.X, centre.Y, innerRadius, outerRadius, startBearing.ToDegrees() - 90, endBearing.ToDegrees() - 90);

    /// <summary>The one arc along the outer rim, from the start angle to the end angle: the part of the outline a chord diagram's
    /// ribbon takes for its end, and the part <see cref="Path"/> runs along first.</summary>
    /// <returns>The arc.</returns>
    internal ArcSegment OuterArc() => new(Centre, OuterRadius, OuterRadius, StartAngleDegrees, EndAngleDegrees);

    /// <summary>Builds the outline with <see cref="ArcSegment"/> commands. When the inner radius is 0 the result is a solid
    /// slice (the first segment moves to the centre); when positive the result is a ring wedge (the first segment moves to the
    /// inner arc start).</summary>
    public IReadOnlyList<PathSegment> Path()
    {
        double startRad = StartAngleDegrees.ToRadians();
        double endRad   = EndAngleDegrees.ToRadians();

        if (InnerRadius <= 0)
        {
            return new PathSegment[]
            {
                new MoveToSegment(Centre),
                new LineToSegment(Centre.AtAngle(OuterRadius, startRad)),
                OuterArc(),
                new CloseSegment()
            };
        }

        return new PathSegment[]
        {
            new MoveToSegment(Centre.AtAngle(InnerRadius, startRad)),
            new LineToSegment(Centre.AtAngle(OuterRadius, startRad)),
            OuterArc(),
            new LineToSegment(Centre.AtAngle(InnerRadius, endRad)),
            new ArcSegment(Centre, InnerRadius, InnerRadius, EndAngleDegrees, StartAngleDegrees),
            new CloseSegment()
        };
    }
}
