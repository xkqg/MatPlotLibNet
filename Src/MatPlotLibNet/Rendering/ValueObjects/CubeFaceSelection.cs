// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.Lighting;

namespace MatPlotLibNet.Rendering;

/// <summary>One of the three axes of the 3-D data cube.</summary>
public enum CubeAxis
{
    /// <summary>The X axis.</summary>
    X = 0,

    /// <summary>The Y axis.</summary>
    Y = 1,

    /// <summary>The Z axis.</summary>
    Z = 2,
}

/// <summary>Which of the two parallel planes perpendicular to a <see cref="CubeAxis"/> is meant —
/// the one at the axis minimum or the one at the axis maximum.</summary>
public enum CubeSide
{
    /// <summary>The plane at the axis minimum.</summary>
    Min = 0,

    /// <summary>The plane at the axis maximum.</summary>
    Max = 1,
}

/// <summary>
/// The camera's verdict for ONE axis: which of that axis' two parallel cube faces points away from
/// the viewer. Carries a <see cref="CubeSide"/>, never a coordinate — the coordinate only exists
/// relative to a data box, and a projection may be built over a different box than the renderer
/// draws with, so the side is resolved by the caller through <see cref="Coordinate"/>.
/// </summary>
/// <param name="Axis">The axis this plane is perpendicular to.</param>
/// <param name="Back">The side facing away from the camera — the pane matplotlib shades.</param>
public readonly record struct CubePlane(CubeAxis Axis, CubeSide Back)
{
    /// <summary>The side facing the camera — the opposite of <see cref="Back"/>.</summary>
    public CubeSide Front => Back == CubeSide.Min ? CubeSide.Max : CubeSide.Min;

    /// <summary>Resolves one side of this plane into a coordinate on the supplied data box.</summary>
    /// <param name="box">The data box the coordinate is read from.</param>
    /// <param name="side">Which side to resolve — typically <see cref="Back"/> or <see cref="Front"/>.</param>
    public double Coordinate(Box3D box, CubeSide side)
    {
        var range = Axis switch
        {
            CubeAxis.X => box.X,
            CubeAxis.Y => box.Y,
            _ => box.Z,
        };
        return side == CubeSide.Min ? range.Lo : range.Hi;
    }
}

/// <summary>A cube edge in data space, carrying the two endpoints a tick row or axis title runs along.</summary>
/// <param name="From">The endpoint on the back side of the edge's own axis.</param>
/// <param name="To">The endpoint on the front side of the edge's own axis.</param>
public readonly record struct AxisEdge3D(Vec3 From, Vec3 To);

/// <summary>
/// Which three cube faces the camera puts at the back, and therefore which three cube edges carry
/// the X, Y and Z ticks. This is the port of matplotlib's
/// <c>mpl_toolkits.mplot3d.axis3d.Axis._get_coord_info</c> (which of two parallel planes is farther)
/// plus <c>_get_axis_line_edge_points</c> (which edge each axis line then runs along).
/// </summary>
/// <remarks>
/// <para>
/// Computed by <see cref="Projection3D"/>, which owns both the camera and the data box; read it as
/// <see cref="Projection3D.Faces"/>. Before v1.14.1 the renderer hard-coded floor <c>z=zMin</c>,
/// wall <c>x=xMin</c> and wall <c>y=yMax</c>, which is matplotlib's answer only for elevation ≥ 0
/// with azimuth in [−90°, 0°]; outside that quadrant the shaded pane landed on a face that had
/// rotated to the FRONT and the Y/Z tick rows landed behind the data (GitHub issue #18).
/// </para>
/// <para>
/// The axis-line rule, verified against matplotlib 3.11.1 over 144 cameras: the X line runs along
/// the FRONT Y plane and the BACK Z plane; the Y line along the FRONT X plane and the BACK Z plane;
/// the Z line along the FRONT X plane and the BACK Y plane.
/// </para>
/// </remarks>
/// <param name="X">The camera's verdict for the X axis.</param>
/// <param name="Y">The camera's verdict for the Y axis.</param>
/// <param name="Z">The camera's verdict for the Z axis.</param>
public readonly record struct CubeFaceSelection(CubePlane X, CubePlane Y, CubePlane Z)
{
    /// <summary>The three selected planes in axis order — the set the renderer shades and grids.</summary>
    public IReadOnlyList<CubePlane> Planes => [X, Y, Z];

    /// <summary>The plane belonging to <paramref name="axis"/>.</summary>
    public CubePlane this[CubeAxis axis] => axis switch
    {
        CubeAxis.X => X,
        CubeAxis.Y => Y,
        _ => Z,
    };

    /// <summary>
    /// The cube edge along which <paramref name="axis"/>' ticks and title are drawn: it spans that
    /// axis' full range and is pinned to the two planes matplotlib pins it to (see the type remarks).
    /// <see cref="AxisEdge3D.From"/> sits on the axis' own BACK side, <see cref="AxisEdge3D.To"/> on
    /// its front side.
    /// </summary>
    /// <param name="axis">The axis whose tick edge is wanted.</param>
    /// <param name="box">The data box the edge coordinates are resolved against.</param>
    public AxisEdge3D AxisEdge(CubeAxis axis, Box3D box)
    {
        var own = this[axis];
        double back = own.Coordinate(box, own.Back);
        double front = own.Coordinate(box, own.Front);
        double xFront = X.Coordinate(box, X.Front);
        double yFront = Y.Coordinate(box, Y.Front);
        double yBack = Y.Coordinate(box, Y.Back);
        double zBack = Z.Coordinate(box, Z.Back);

        return axis switch
        {
            CubeAxis.X => new(new(back, yFront, zBack), new(front, yFront, zBack)),
            CubeAxis.Y => new(new(xFront, back, zBack), new(xFront, front, zBack)),
            _ => new(new(xFront, yBack, back), new(xFront, yBack, front)),
        };
    }

    /// <summary>The single corner where the three back planes meet — the corner farthest from the camera.</summary>
    /// <param name="box">The data box the corner is resolved against.</param>
    public Vec3 FarCorner(Box3D box) => new(
        X.Coordinate(box, X.Back),
        Y.Coordinate(box, Y.Back),
        Z.Coordinate(box, Z.Back));
}
