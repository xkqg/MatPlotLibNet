// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Projects 3D data coordinates to 2D pixel coordinates using matplotlib's exact
/// <c>mpl_toolkits.mplot3d</c> pipeline: <c>World → View → Ortho/Persp → NDC → Pixel</c>.
/// </summary>
/// <remarks>
/// The pipeline is faithful to matplotlib 3.11 <c>axes3d.py / proj3d.py</c>:
/// <list type="bullet">
///   <item>World matrix scales data ranges by <c>pb_aspect</c> so the data box becomes the world box.</item>
///   <item>View matrix places the camera on a sphere around the box center at distance <c>_dist</c>,
///         with basis <c>(u, v, w)</c> derived from elevation, azimuth and roll.</item>
///   <item>Ortho projection gives depth-independent <c>(x, y)</c>; perspective projection (if
///         <c>focalLength</c> set) applies foreshortening.</item>
///   <item>The projected point is placed as matplotlib places it: the plot is shrunk to a square centred in it, and the
///         square shows the projected points from -0.95 / dist to 0.9 / dist in both directions, whatever the corners of
///         the box project to.</item>
/// </list>
/// </remarks>
public sealed class Projection3D
{
    /// <summary>The three axes of the world box: the data range along each (widened when flat) and the share of the box it spans.</summary>
    private readonly WorldAxis _x, _y, _z;
    private readonly double? _distance;

    // matplotlib's default box aspect (set_box_aspect): (4, 4, 3) times 1.8294640721620434 * 25/24 / |(4, 4, 3)| at zoom 1,
    // which is (25/21, 25/21, 25/28) ≈ (1.190, 1.190, 0.893).
    private const double BoxAspectX = 25.0 / 21.0;  // ≈ 1.19047619
    private const double BoxAspectY = 25.0 / 21.0;
    private const double BoxAspectZ = 25.0 / 28.0;  // ≈ 0.89285714

    // matplotlib's focal length (Axes3D __init__: proj_type='persp', focal_length=None → 1); the camera's distance at rest is
    // Camera3D.RestingDistance (self._dist = 10).
    private const double DefaultFocalLength = 1.0;

    // The rows of the full 4×4 projection matrix M = ProjM · ViewM · WorldM, each applied by MatrixRow.Of to a data point
    // measured from the box's first corner (WorldAxis.From).
    private readonly MatrixRow _m0, _m1, _m2, _m3;

    // matplotlib's view limits times the camera distance (axes3d.py set_top_view): the view runs from -0.95 to 0.9 in
    // both directions, 1.85 in all.
    private const double ViewBelow = 0.95, ViewAbove = 0.9, ViewSpan = 1.85;

    // The fit of the box into the plot, worked out once: the pixel the projected origin (the centre of the box) lands on and
    // the one pixel scale both directions share (see Project).
    private readonly double _originX, _originY, _fitScale;

    /// <summary>The camera elevation above the XY plane, in degrees.</summary>
    public double Elevation { get; }

    /// <summary>The camera azimuth, its rotation around the Z axis, in degrees.</summary>
    public double Azimuth { get; }

    /// <summary>The camera distance the caller set, at least 2; null for matplotlib's default camera, at <see cref="Models.Camera3D.RestingDistance"/>. The projection is
    /// a perspective one either way.</summary>
    public double? Distance => _distance;

    /// <summary>The data box this projection maps — the <c>(xMin…zMax)</c> the constructor received, with an axis of no
    /// width widened by half a unit on each side.</summary>
    public Box3D DataBox { get; }

    /// <summary>
    /// Which three cube faces this camera puts at the back, and therefore which cube edges carry the
    /// X, Y and Z ticks. Computed once per projection; see <see cref="CubeFaceSelection"/>.
    /// </summary>
    public CubeFaceSelection Faces { get; }

    /// <summary>Initializes a new <see cref="Projection3D"/> matching matplotlib's 3-D pipeline.</summary>
    /// <param name="elevation">Camera elevation above the XY plane in degrees.</param>
    /// <param name="azimuth">Camera azimuth rotation around the Z axis in degrees.</param>
    /// <param name="plotBounds">Pixel rectangle into which the projection maps.</param>
    /// <param name="xMin">Minimum X data value.</param>
    /// <param name="xMax">Maximum X data value.</param>
    /// <param name="yMin">Minimum Y data value.</param>
    /// <param name="yMax">Maximum Y data value.</param>
    /// <param name="zMin">Minimum Z data value.</param>
    /// <param name="zMax">Maximum Z data value.</param>
    /// <param name="distance">The camera's distance from the centre of the box, at least 2; null for matplotlib's default
    /// camera, at <see cref="Models.Camera3D.RestingDistance"/>. A distance d draws as matplotlib's perspective with focal length d / 10.</param>
    /// <remarks>An axis whose range has no width (one point, a constant column) places its values in the middle of the
    /// box: the range is widened by half a unit on each side, the rule every 2-D axis follows
    /// (<see cref="Range1D.Normalized"/>), and <see cref="DataBox"/> holds the widened range. Scaling a range of no width
    /// to the box would divide by zero. A range given high to low (a limit set that way) draws its axis the
    /// other way round, as matplotlib's inverted 3-D axis does.</remarks>
    public Projection3D(double elevation, double azimuth, Rect plotBounds,
        double xMin, double xMax, double yMin, double yMax, double zMin, double zMax,
        double? distance = null)
    {
        _x = WorldAxis.Of(new Range1D(xMin, xMax), BoxAspectX);
        _y = WorldAxis.Of(new Range1D(yMin, yMax), BoxAspectY);
        _z = WorldAxis.Of(new Range1D(zMin, zMax), BoxAspectZ);

        Elevation = elevation;
        Azimuth = azimuth;
        _distance = distance.HasValue ? Math.Max(2.0, distance.Value) : null;

        // Camera distance drives BOTH the view transform (eye placement along the
        // camera-forward axis) AND the perspective projection matrix's `zfront`/`zback`
        // clip range. Previously this field was stored but never consumed, so every
        // projection ran with the hard-coded resting distance regardless of the caller's
        // `distance:` argument — masking the perspective-parallax behaviour entirely.
        // Null (the common case) still resolves to 10 for backward compatibility with
        // every production 3-D renderer that doesn't set `Axes.CameraDistance`.
        double dist = _distance ?? Models.Camera3D.RestingDistance;
        double focalLength = DefaultFocalLength;
        double elRad = elevation.ToRadians();
        double azRad = azimuth.ToRadians();
        double cosEl = Math.Cos(elRad), sinEl = Math.Sin(elRad);
        double cosAz = Math.Cos(azRad), sinAz = Math.Sin(azRad);

        // --- World matrix: scales data range to world box [0, ax] × [0, ay] × [0, az] ---
        // matplotlib world_transformation(xmin..zmax, pb_aspect=(ax, ay, az)) with the matrix
        //   [[ ax/dx,   0,     0,   -xmin*ax/dx ],
        //    [   0,   ay/dy,   0,   -ymin*ay/dy ],
        //    [   0,     0,   az/dz, -zmin*az/dz ],
        //    [   0,     0,     0,        1      ]]
        // A range set high to low has a negative width, and the negative scale draws its axis the other way round, as
        // matplotlib's world transformation does; Range1D.Normalized has already widened a range of no width, so no width
        // is 0. The translation column -xmin*ax/dx is taken out of the matrix: a point is measured from the box's first corner
        // before it meets the matrix ((x - xmin) * ax/dx, the same number in exact arithmetic), so a narrow box far from 0
        // (a millisecond of dates) keeps its digits instead of losing them to two large terms that cancel.
        double sx = _x.Scale, sy = _y.Scale, sz = _z.Scale;

        // --- View matrix (port of proj3d._view_axes + _view_transformation_uvw) ---
        //   R   = 0.5 * box_aspect                               (box centre in world coords)
        //   ps  = (cos(el)cos(az), cos(el)sin(az), sin(el))      (camera direction unit)
        //   eye = R + dist * ps
        //   For persp, viewM uses eye_focal = R + dist*ps*focal_length (= eye when focal_length=1)
        //
        //   w = normalize(eye - R) = ps                          (forward, out of screen)
        //   u = normalize(cross(V, w))  where V = (0, 0, 1)      (right, in screen)
        //       cross(V, w) = (-w_y, w_x, 0) = (-cos(el)sin(az), cos(el)cos(az), 0)
        //       |...|       = cos(el)  (el ∈ [-90°, 90°])
        //       u           = (-sin(az), cos(az), 0)
        //   v = cross(w, u)                                      (up, in screen)
        //       = (-sin(el)cos(az), -sin(el)sin(az), cos(el))
        //
        //   viewM = Mr · Mt   with Mr = [[u], [v], [w]] and Mt translates by -eye_focal
        double rcx = _x.Middle, rcy = _y.Middle, rcz = _z.Middle;
        double ex = rcx + dist * focalLength * cosEl * cosAz;
        double ey = rcy + dist * focalLength * cosEl * sinAz;
        double ez = rcz + dist * focalLength * sinEl;

        double ux = -sinAz,             uy = cosAz,              uz = 0;
        double vx = -sinEl * cosAz,     vy = -sinEl * sinAz,     vz = cosEl;
        double wx =  cosEl * cosAz,     wy =  cosEl * sinAz,     wz = sinEl;

        double uT = -(ux * ex + uy * ey + uz * ez);
        double vT = -(vx * ex + vy * ey + vz * ez);
        double wT = -(wx * ex + wy * ey + wz * ez);

        // --- view · world (3-row × 4-col result; bottom row is [0,0,0,1]) ---
        // worldM[i][j] is diagonal sx/sy/sz (its translation is the anchoring above),
        // so (view · world)[r][c] = view[r][0..2] · worldDiag[0..2][c] when c<3,
        // and = view[r][3] when c==3.
        double vw00 = ux * sx, vw01 = uy * sy, vw02 = uz * sz, vw03 = uT;
        double vw10 = vx * sx, vw11 = vy * sy, vw12 = vz * sz, vw13 = vT;
        double vw20 = wx * sx, vw21 = wy * sy, vw22 = wz * sz, vw23 = wT;

        // --- Persp projection matrix (matplotlib _persp_transformation(-dist, dist, focal_length)) ---
        //   e = focal_length, a = 1
        //   b = (zfront + zback) / (zfront - zback)     → 0 for ±dist
        //   c = -2·(zfront·zback) / (zfront - zback)    → dist for ±dist
        //   projM = [[e,  0, 0, 0],
        //            [0, e/a, 0, 0],
        //            [0,  0, b, c],
        //            [0,  0,-1, 0]]
        //
        // Compose M = projM · (view · world):
        //   row 0 of M = e · (view · world)[0]
        //   row 1 of M = (e/a) · (view · world)[1]
        //   row 2 of M = b · (view · world)[2] + c · [0,0,0,1]
        //             = [b·vw20, b·vw21, b·vw22, b·vw23 + c]
        //   row 3 of M = -1 · (view · world)[2]  (perspective-divide denominator)
        //             = [-vw20, -vw21, -vw22, -vw23]
        //
        // With focal_length = 1 (default) and zfront=-dist, zback=dist: e=1, b=0, c=dist.
        // So row 2 becomes [0,0,0, dist] and w-row becomes the negated w-projection.
        const double a = 1.0;
        double e = focalLength;
        double zfront = -dist, zback = dist;
        double b = (zfront + zback) / (zfront - zback);
        double c = -2 * (zfront * zback) / (zfront - zback);

        _m0 = new MatrixRow(e * vw00, e * vw01, e * vw02, e * vw03);
        _m1 = new MatrixRow((e / a) * vw10, (e / a) * vw11, (e / a) * vw12, (e / a) * vw13);
        _m2 = new MatrixRow(b * vw20, b * vw21, b * vw22, b * vw23 + c);
        _m3 = new MatrixRow(-vw20, -vw21, -vw22, -vw23);

        // The fit, matplotlib's rule (axes3d.py): the axes is the plot shrunk to a square and centred in it (apply_aspect,
        // anchor 'C'), and its view limits are fixed at (-0.95 / dist, 0.9 / dist) in both directions (set_top_view), so the
        // box is never fitted to its projected corners. A camera distance d draws as matplotlib's perspective with focal
        // length d / 10 (the same eye, the projected point d / 10 the size of ours), so the limits scale with the distance
        // and the box keeps its size. Svg3DRotationScript fits the box by the same rule.
        double side = plotBounds.ShorterSide;
        _fitScale = side * dist / ViewSpan;
        _originX = Range1D.CentredIn(plotBounds.X, plotBounds.Width, side).Lo + side * ViewBelow / ViewSpan;
        _originY = Range1D.CentredIn(plotBounds.Y, plotBounds.Height, side).Lo + side * ViewAbove / ViewSpan;

        DataBox = new Box3D(_x.Range, _y.Range, _z.Range);
        Faces = SelectFaces();
    }

    // matplotlib's machine epsilon (numpy float64 `np.finfo(float).eps`) — the tolerance
    // axis3d.py:289 uses to decide that two parallel planes are exactly edge-on.
    private const double MachineEpsilon = 2.220446049250313e-16;

    /// <summary>
    /// Port of matplotlib <c>axis3d.Axis._get_coord_info</c> (lines 280-297): for each axis, compare
    /// the mean PROJECTED depth of the two parallel cube faces; the farther one is the back pane.
    /// The edge-on special case is carried over verbatim — when exactly two axis-pairs are within
    /// <see cref="MachineEpsilon"/> of each other (the camera looks straight down one axis), the
    /// tie is broken by the one remaining axis instead of by float noise.
    /// </summary>
    /// <remarks>
    /// A SINGLE near-tie (one axis seen exactly edge-on — azimuth ±90° for the X faces, 0°/±180° for
    /// the Y faces) is left to the same comparison matplotlib uses, deliberately: resolving it from
    /// the camera basis instead gives the OTHER face at azimuth ±180° (measured), so an "improvement"
    /// there would be a divergence from the reference. Both faces of an edge-on pair project onto the
    /// same screen line, so the choice is not visible; every operation after
    /// <see cref="Math.Cos"/>/<see cref="Math.Sin"/> is IEEE-deterministic, so the two platforms agree
    /// as long as their trig agrees on these arguments.
    /// </remarks>
    private CubeFaceSelection SelectFaces()
    {
        Span<double> lo = [_x.Range.Lo, _y.Range.Lo, _z.Range.Lo];
        Span<double> hi = [_x.Range.Hi, _y.Range.Hi, _z.Range.Hi];
        Span<bool> backAtMax = stackalloc bool[3];
        Span<bool> edgeOn = stackalloc bool[3];
        int edgeOnCount = 0;

        for (int axis = 0; axis < 3; axis++)
        {
            double meanLo = MeanPlaneDepth(axis, lo[axis], lo, hi);
            double meanHi = MeanPlaneDepth(axis, hi[axis], lo, hi);
            backAtMax[axis] = meanLo < meanHi;
            edgeOn[axis] = Math.Abs(meanLo - meanHi) <= MachineEpsilon;
            if (edgeOn[axis]) edgeOnCount++;
        }

        if (edgeOnCount == 2)
        {
            int vertical = !edgeOn[0] ? 0 : !edgeOn[1] ? 1 : 2;
            switch (vertical)
            {
                case 2:  // looking at the XY plane
                    backAtMax[0] = true; backAtMax[1] = true;
                    break;
                case 1:  // looking at the XZ plane
                    backAtMax[0] = true; backAtMax[2] = false;
                    break;
                default: // looking at the YZ plane
                    backAtMax[1] = false; backAtMax[2] = false;
                    break;
            }
        }

        return new CubeFaceSelection(
            new CubePlane(CubeAxis.X, backAtMax[0] ? CubeSide.Max : CubeSide.Min),
            new CubePlane(CubeAxis.Y, backAtMax[1] ? CubeSide.Max : CubeSide.Min),
            new CubePlane(CubeAxis.Z, backAtMax[2] ? CubeSide.Max : CubeSide.Min));
    }

    /// <summary>Mean projected depth of the four corners of one axis-aligned cube face.</summary>
    private double MeanPlaneDepth(int axis, double coordinate, Span<double> lo, Span<double> hi)
    {
        int a = (axis + 1) % 3;
        int b = (axis + 2) % 3;
        Span<double> point = stackalloc double[3];
        point[axis] = coordinate;
        double sum = 0;
        for (int i = 0; i < 2; i++)
        {
            point[a] = i == 0 ? lo[a] : hi[a];
            for (int j = 0; j < 2; j++)
            {
                point[b] = j == 0 ? lo[b] : hi[b];
                sum += ProjectedDepth(point[0], point[1], point[2]);
            }
        }
        return sum / 4;
    }

    /// <summary>
    /// The POST-divide projected z of a data point — matplotlib's <c>proj_transform</c> third
    /// component, the statistic its face selection compares and its depth shading measures by. Distinct from
    /// <see cref="Depth"/>, which is the pre-divide painter's-algorithm sort key.
    /// </summary>
    private double ProjectedDepth(double x, double y, double z)
    {
        double ax = _x.From(x), ay = _y.From(y), az = _z.From(z);
        return _m2.Of(ax, ay, az) / _m3.Of(ax, ay, az);
    }

    /// <summary>A data point as matplotlib's <c>proj_transform</c> gives it, before the plot's fit: the view coordinates of
    /// <see cref="Project"/> and the POST-divide depth (<see cref="ProjectedDepth"/>), which grows with the distance. What the depth
    /// shading of a 3-D scatter measures its markers by.</summary>
    /// <param name="x">The data x.</param>
    /// <param name="y">The data y.</param>
    /// <param name="z">The data z.</param>
    /// <returns>The projected point.</returns>
    internal Projected3DPoint ProjectedAt(double x, double y, double z)
    {
        double ax = _x.From(x), ay = _y.From(y), az = _z.From(z);
        double mw = _m3.Of(ax, ay, az);
        return new Projected3DPoint(_m0.Of(ax, ay, az) / mw, _m1.Of(ax, ay, az) / mw, ProjectedDepth(x, y, z));
    }

    /// <summary>Normalizes data coordinates to matplotlib's world box.</summary>
    /// <remarks>Primarily called by 3-D series renderers; consumer code should prefer the renderer
    /// pipeline rather than calling this method directly. Returns values in the matplotlib default
    /// <c>(25/21, 25/21, 25/28)</c> world box, not a uniform [-1, 1]³ cube.</remarks>
    public Normalized3DPoint Normalize(double x, double y, double z)
    {
        // A range set high to low normalises the other way round, where the projection draws it; no range has no width
        // (the constructor widened it).
        double nx = _x.Share(x), ny = _y.Share(y), nz = _z.Share(z);
        // Recenter to [-BoxAspect/2, +BoxAspect/2] for backward-compat with old callers that
        // expect zero-centered normalized points (used by a handful of grid/label helpers).
        return new(nx - _x.Middle, ny - _y.Middle, nz - _z.Middle);
    }

    /// <summary>Projects a 3D point to 2D pixel coordinates via matplotlib's full M matrix.</summary>
    public Point Project(double x, double y, double z)
    {
        // M @ [x - xmin, y - ymin, z - zmin, 1], divided by its w, then fitted into the plot (the fit is worked out once, in
        // the constructor).
        double ax = _x.From(x), ay = _y.From(y), az = _z.From(z);
        double mw = _m3.Of(ax, ay, az);
        double nx = _m0.Of(ax, ay, az) / mw;
        double ny = _m1.Of(ax, ay, az) / mw;
        return new Point(_originX + nx * _fitScale, _originY - ny * _fitScale);
    }

    /// <summary>
    /// Returns a depth value for painter's-algorithm sorting. Lower depth = FARTHER from
    /// camera. For the persp matrix, row 3 of M is <c>-1 · (view·world)[2]</c> which is
    /// the negated camera-forward projection — i.e. a larger value means FURTHER from the
    /// eye. Negating again gives "low = far" so the caller's ascending sort paints the
    /// farthest polygon first.
    /// </summary>
    public double Depth(double x, double y, double z) => -_m3.Of(_x.From(x), _y.From(y), _z.From(z));

    /// <summary>One row of the projection matrix, applied to a point measured from the box's first corner.</summary>
    private readonly record struct MatrixRow(double X, double Y, double Z, double W)
    {
        /// <summary>The row applied to the point (<paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>, 1), each
        /// coordinate already measured from the box's first corner, summed left to right.</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal double Of(double x, double y, double z) => X * x + Y * y + Z * z + W;
    }

    /// <summary>One axis of the world box a 3-D projection maps its data into (matplotlib's <c>world_transformation</c>, with
    /// <c>pb_aspect</c>): the data range along it and the share of the box it spans. The one place a value along X, Y or Z is measured from
    /// the box's first corner and scaled into the box, so <see cref="Projection3D"/> says it once for the three axes.</summary>
    /// <param name="Range">The data range, widened when flat (<see cref="Range1D.Normalized"/>); a range set high to low scales the other way
    /// round, as matplotlib draws an inverted 3-D axis.</param>
    /// <param name="Aspect">The share of the box the axis spans (matplotlib's box aspect, 25/21 along X and Y and 25/28 along Z).</param>
    internal readonly record struct WorldAxis(Range1D Range, double Aspect)
    {
        /// <summary>The axis over <paramref name="range"/>, widened first when it is flat, spanning <paramref name="aspect"/> of the box.</summary>
        /// <param name="range">The data range.</param>
        /// <param name="aspect">The share of the box.</param>
        /// <returns>The axis.</returns>
        internal static WorldAxis Of(Range1D range, double aspect) => new(range.Normalized(), aspect);

        /// <summary>How far <paramref name="value"/> lies from the low end of the range: a point is measured from the box's first corner
        /// before it meets the projection's matrix, so a narrow box far from 0 keeps its digits.</summary>
        /// <param name="value">The data value.</param>
        /// <returns>The offset, in data units.</returns>
        internal double From(double value) => value - Range.Lo;

        /// <summary>The factor a data offset along the axis is scaled into the box by: its share over the width of the range.</summary>
        internal double Scale => Aspect / (Range.Hi - Range.Lo);

        /// <summary>Where <paramref name="value"/> stands in the box along the axis, from 0 at the low end to <see cref="Rendering.Projection3D.WorldAxis.Aspect"/> at the high.</summary>
        /// <param name="value">The data value.</param>
        /// <returns>The place in the box.</returns>
        internal double Share(double value) => Aspect * (value - Range.Lo) / (Range.Hi - Range.Lo);

        /// <summary>The middle of the box along the axis: half its share.</summary>
        internal double Middle => Aspect / 2;
    }
}
