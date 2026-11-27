// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>Where a point of an axes' data stands on the picture: the one question a patch's outline and an inset's zoom indicator
/// ask of the axes they are drawn on, whatever its coordinate system. A Cartesian or ternary axes answers through its transform
/// (<see cref="TransformPlacement"/>), a polar axes by angle and radius (<see cref="PolarPlacement"/>), as matplotlib draws a patch
/// through the axes' <c>transData</c>. Each placement is a value, so a generic caller is compiled for it and places a point
/// without a call through the interface.</summary>
internal interface IDataPlacement
{
    /// <summary>The pixel of the data point as matplotlib's <c>transData</c> places it, before any clip: a finite value at or
    /// below zero on a log axis a thousand decades beyond that axis (<c>nonpositive='clip'</c>); a point that is not finite where
    /// the axes masks it (outside 0 to 1 on a logit axis, inside an axis break, a radius on the far side of a polar scale's
    /// origin).</summary>
    /// <param name="x">The X value (the angle on a polar axes, in radians).</param>
    /// <param name="y">The Y value (the radius on a polar axes).</param>
    /// <returns>The pixel, or a point that is not finite where the axes masks the value.</returns>
    Point PlaceUnclipped(double x, double y);

    /// <summary>The axes <paramref name="pixel"/>, from <see cref="PlaceUnclipped"/>, stands a thousand decades beyond: an outline
    /// through it is cut at their edges by <see cref="CutAtTheStart"/>.</summary>
    /// <param name="pixel">The pixel.</param>
    /// <returns>The axes the pixel stands beyond.</returns>
    AxisPair<bool> BeyondOf(Point pixel);

    /// <summary>A path in pixels cut where it leaves the plot through the edge each axis <paramref name="beyond"/> names begins
    /// at, as the axes clip cuts what the log scale puts a thousand decades beyond it (<see cref="DataTransform.CutAtTheStart"/>).</summary>
    /// <param name="path">The vertices, in pixels.</param>
    /// <param name="closed">Whether the path closes back to its first vertex.</param>
    /// <param name="beyond">The axes the outline reaches beyond.</param>
    /// <returns>The cut path.</returns>
    IReadOnlyList<Point> CutAtTheStart(IReadOnlyList<Point> path, bool closed, AxisPair<bool> beyond);

    /// <summary>Whether an axis of the placement is drawn in parts that stand room apart: an outline is then cut where it crosses a
    /// room as well (<see cref="Rendering.DataTransform.RoomCut.Cut"/>, through <see cref="CutAtTheStart"/>).</summary>
    bool HasRooms { get; }
}

/// <summary>The placement of a Cartesian or ternary axes: its transform, <see cref="DataTransform.DataToPixelUnclipped"/> for a
/// point and <see cref="DataTransform.CutAtTheStart"/> for an outline through a value a log axis cannot place.</summary>
/// <param name="Transform">The transform of the axes' render.</param>
internal readonly record struct TransformPlacement(DataTransform Transform) : IDataPlacement
{
    /// <inheritdoc />
    public Point PlaceUnclipped(double x, double y) => Transform.DataToPixelUnclipped(x, y);

    /// <inheritdoc />
    public AxisPair<bool> BeyondOf(Point pixel) => Transform.BeyondOf(pixel);

    /// <inheritdoc />
    public IReadOnlyList<Point> CutAtTheStart(IReadOnlyList<Point> path, bool closed, AxisPair<bool> beyond) =>
        Transform.CutAtTheStart(path, closed, beyond);

    /// <inheritdoc />
    public bool HasRooms => Transform.HasRooms;
}

/// <summary>The placement of a polar axes: x is the angle in radians and y the radius, through the transform its series are drawn
/// with (<see cref="PolarTransform.PolarToPixel"/>), so a radius past the outer ring stands past it on the same scale and one past the
/// origin has no place, as for a polar series. A polar axes has no log axis, so nothing stands beyond one.</summary>
/// <param name="Polar">The transform of the axes' render.</param>
internal readonly record struct PolarPlacement(PolarTransform Polar) : IDataPlacement
{
    /// <inheritdoc />
    public Point PlaceUnclipped(double x, double y) => Polar.PolarToPixel(y, x);

    /// <inheritdoc />
    public AxisPair<bool> BeyondOf(Point pixel) => default;

    /// <inheritdoc />
    public IReadOnlyList<Point> CutAtTheStart(IReadOnlyList<Point> path, bool closed, AxisPair<bool> beyond) => path;

    /// <inheritdoc />
    /// <remarks>A polar axes has no break.</remarks>
    public bool HasRooms => false;
}
