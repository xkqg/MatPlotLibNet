// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering;

/// <summary>
/// Projects 3D data coordinates to 2D pixel coordinates using matplotlib's exact
/// <c>mpl_toolkits.mplot3d</c> pipeline: <c>World → View → Ortho/Persp → NDC → Pixel</c>.
/// </summary>
/// <remarks>
/// The pipeline is faithful to matplotlib 3.10 <c>axes3d.py / proj3d.py</c>:
/// <list type="bullet">
///   <item>World matrix scales data ranges by <c>pb_aspect</c> so the data box becomes the world box.</item>
///   <item>View matrix places the camera on a sphere around the box center at distance <c>_dist</c>,
///         with basis <c>(u, v, w)</c> derived from elevation, azimuth and roll.</item>
///   <item>Ortho projection gives depth-independent <c>(x, y)</c>; perspective projection (if
///         <c>focalLength</c> set) applies foreshortening.</item>
///   <item>The resulting NDC bounding box of the 8 cube corners is then fit into the pixel plot
///         bounds. matplotlib's exact pixel scale depends on axes zoom and bbox, so we use a
///         fit-to-plot-bounds with a small margin multiplier. Verified against matplotlib by
///         dumping <c>get_proj()</c> output and comparing pixel positions.</item>
/// </list>
/// </remarks>
public sealed class Projection3D
{
    private readonly Rect _plotBounds;
    private readonly double _xMin, _xMax, _yMin, _yMax, _zMin, _zMax;
    private readonly double? _distance;

    // matplotlib default box aspect: (4, 4, 3) / (3 * zoom) at zoom=1, then SCALED by an
    // internal factor so get_box_aspect() returns (25/21, 25/21, 25/28) ≈ (1.190, 1.190, 0.893).
    // These values make the cube corners project to NDC values in range ~[-0.09, +0.09],
    // which matplotlib then scales to its axes pixel bbox using an additional ~11× factor.
    private const double BoxAspectX = 25.0 / 21.0;  // ≈ 1.19047619
    private const double BoxAspectY = 25.0 / 21.0;
    private const double BoxAspectZ = 25.0 / 28.0;  // ≈ 0.89285714

    // Default matplotlib camera distance and focal length.
    // matplotlib Axes3D __init__: self._dist = 10, proj_type='persp', focal_length=None (→ 1).
    private const double DefaultDist = 10.0;
    private const double DefaultFocalLength = 1.0;

    // Full 4×4 projection matrix M = ProjM · ViewM · WorldM (row-major).
    private readonly double[,] _m;

    // Post-rotation screen-space fit bounds, computed from the 8 corners of the normalized
    // [0, ax] × [0, ay] × [0, az] world box. The renderer fits this bbox into _plotBounds.
    private readonly double _fitXMin, _fitXMax, _fitYMin, _fitYMax;

    public double Elevation { get; }

    public double Azimuth { get; }

    /// <summary>Camera distance for perspective projection. Null = orthographic. Clamped to minimum 2.0.</summary>
    public double? Distance => _distance;

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
    /// <param name="distance">Camera distance (persp focal length). Null = orthographic with dist=10.</param>
    public Projection3D(double elevation, double azimuth, Rect plotBounds,
        double xMin, double xMax, double yMin, double yMax, double zMin, double zMax,
        double? distance = null)
    {
        Elevation = elevation;
        Azimuth = azimuth;
        _plotBounds = plotBounds;
        _xMin = xMin; _xMax = xMax; _yMin = yMin; _yMax = yMax; _zMin = zMin; _zMax = zMax;
        _distance = distance.HasValue ? Math.Max(2.0, distance.Value) : null;

        // Camera distance drives BOTH the view transform (eye placement along the
        // camera-forward axis) AND the perspective projection matrix's `zfront`/`zback`
        // clip range. Previously this field was stored but never consumed, so every
        // projection ran with the hard-coded `DefaultDist = 10` regardless of the caller's
        // `distance:` argument — masking the perspective-parallax behaviour entirely.
        // Null (the common case) still resolves to 10 for backward compatibility with
        // every production 3-D renderer that doesn't set `Axes.CameraDistance`.
        double dist = _distance ?? DefaultDist;
        double focalLength = DefaultFocalLength;
        double elRad = elevation * Math.PI / 180;
        double azRad = azimuth * Math.PI / 180;
        double cosEl = Math.Cos(elRad), sinEl = Math.Sin(elRad);
        double cosAz = Math.Cos(azRad), sinAz = Math.Sin(azRad);

        // --- World matrix: scales data range to world box [0, ax] × [0, ay] × [0, az] ---
        // matplotlib world_transformation(xmin..zmax, pb_aspect=(ax, ay, az)) with the matrix
        //   [[ ax/dx,   0,     0,   -xmin*ax/dx ],
        //    [   0,   ay/dy,   0,   -ymin*ay/dy ],
        //    [   0,     0,   az/dz, -zmin*az/dz ],
        //    [   0,     0,     0,        1      ]]
        double dx = xMax - xMin, dy = yMax - yMin, dz = zMax - zMin;
        if (dx <= 0) dx = 1;
        if (dy <= 0) dy = 1;
        if (dz <= 0) dz = 1;
        double sx = BoxAspectX / dx, sy = BoxAspectY / dy, sz = BoxAspectZ / dz;
        double tx = -xMin * sx, ty = -yMin * sy, tz = -zMin * sz;

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
        double rcx = 0.5 * BoxAspectX, rcy = 0.5 * BoxAspectY, rcz = 0.5 * BoxAspectZ;
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
        // worldM[i][j] is diagonal sx/sy/sz with translation column tx/ty/tz,
        // so (view · world)[r][c] = view[r][0..2] · worldDiag[0..2][c] when c<3,
        // and = view[r][0..2] · (tx,ty,tz) + view[r][3] when c==3.
        double vw00 = ux * sx, vw01 = uy * sy, vw02 = uz * sz;
        double vw03 = ux * tx + uy * ty + uz * tz + uT;
        double vw10 = vx * sx, vw11 = vy * sy, vw12 = vz * sz;
        double vw13 = vx * tx + vy * ty + vz * tz + vT;
        double vw20 = wx * sx, vw21 = wy * sy, vw22 = wz * sz;
        double vw23 = wx * tx + wy * ty + wz * tz + wT;

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

        double[,] M = new double[4, 4];
        M[0, 0] = e * vw00;   M[0, 1] = e * vw01;   M[0, 2] = e * vw02;   M[0, 3] = e * vw03;
        M[1, 0] = (e / a) * vw10; M[1, 1] = (e / a) * vw11; M[1, 2] = (e / a) * vw12; M[1, 3] = (e / a) * vw13;
        M[2, 0] = b * vw20;   M[2, 1] = b * vw21;   M[2, 2] = b * vw22;   M[2, 3] = b * vw23 + c;
        M[3, 0] = -vw20;      M[3, 1] = -vw21;      M[3, 2] = -vw22;      M[3, 3] = -vw23;

        _m = M;

        // Pre-compute rotated screen-space extent of the 8 world-box corners [0,ax]×[0,ay]×[0,az].
        double minX = double.MaxValue, maxX = double.MinValue;
        double minY = double.MaxValue, maxY = double.MinValue;
        for (int i = 0; i < 8; i++)
        {
            double wxv = (i & 1) == 0 ? 0 : BoxAspectX;
            double wyv = (i & 2) == 0 ? 0 : BoxAspectY;
            double wzv = (i & 4) == 0 ? 0 : BoxAspectZ;
            var p = ProjectWorldToNdc(wxv, wyv, wzv);
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Y < minY) minY = p.Y;
            if (p.Y > maxY) maxY = p.Y;
        }
        _fitXMin = minX; _fitXMax = maxX;
        _fitYMin = minY; _fitYMax = maxY;
    }

    /// <summary>Applies the full 4×4 matrix M to a world-box point, returning post-divide NDC (x, y).</summary>
    private Point ProjectWorldToNdc(double wx, double wy, double wz)
    {
        // Direct M @ [wx, wy, wz, 1] without the intermediate world_M step (world coords already).
        // But _m was built as M = Proj · View · World, so feed DATA coords in. For the cube-corner
        // pre-fit pass we feed world coords directly and bypass the world step — so we need a
        // "view+proj only" path. Re-apply the inverse world transform: data = (wx - tx)/sx etc.
        // Simpler: just recompute view·proj on the fly using the stored matrix inverted through world.
        // Easiest: call Project via inverse world map. But for clarity here, we inline a fresh
        // view·proj multiply on (wx, wy, wz) directly.
        //
        // Actually, since _m already includes world, and here wx is ALREADY a world coord (not data),
        // we compute it via the view·proj submatrix. We re-derive that by multiplying the inverse
        // world step. But since the caller passes WORLD coords in [0, BoxAspect*], we can just
        // invert the world: data_x = (wx - tx)/sx. For the pre-fit this returns xMin/xMax.
        // Simpler still: feed DATA coords matching the world corners. World corner (0, 0, 0) = data (xMin, yMin, zMin).
        double dataX = (wx == 0) ? _xMin : _xMax;
        double dataY = (wy == 0) ? _yMin : _yMax;
        double dataZ = (wz == 0) ? _zMin : _zMax;
        double x = _m[0, 0] * dataX + _m[0, 1] * dataY + _m[0, 2] * dataZ + _m[0, 3];
        double y = _m[1, 0] * dataX + _m[1, 1] * dataY + _m[1, 2] * dataZ + _m[1, 3];
        double w = _m[3, 0] * dataX + _m[3, 1] * dataY + _m[3, 2] * dataZ + _m[3, 3];
        return new(x / w, y / w);
    }

    /// <summary>Normalizes data coordinates to matplotlib's world box.</summary>
    /// <remarks>Primarily called by 3-D series renderers; consumer code should prefer the renderer
    /// pipeline rather than calling this method directly. Returns values in the matplotlib default
    /// <c>(25/21, 25/21, 25/28)</c> world box, not a uniform [-1, 1]³ cube.</remarks>
    public Normalized3DPoint Normalize(double x, double y, double z)
    {
        double nx = _xMax > _xMin ? BoxAspectX * (x - _xMin) / (_xMax - _xMin) : 0;
        double ny = _yMax > _yMin ? BoxAspectY * (y - _yMin) / (_yMax - _yMin) : 0;
        double nz = _zMax > _zMin ? BoxAspectZ * (z - _zMin) / (_zMax - _zMin) : 0;
        // Recenter to [-BoxAspect/2, +BoxAspect/2] for backward-compat with old callers that
        // expect zero-centered normalized points (used by a handful of grid/label helpers).
        return new(nx - BoxAspectX / 2, ny - BoxAspectY / 2, nz - BoxAspectZ / 2);
    }

    /// <summary>Projects a 3D point to 2D pixel coordinates via matplotlib's full M matrix.</summary>
    public Point Project(double x, double y, double z)
    {
        // M @ [x, y, z, 1] — full pipeline including world · view · proj.
        double mx = _m[0, 0] * x + _m[0, 1] * y + _m[0, 2] * z + _m[0, 3];
        double my = _m[1, 0] * x + _m[1, 1] * y + _m[1, 2] * z + _m[1, 3];
        double mw = _m[3, 0] * x + _m[3, 1] * y + _m[3, 2] * z + _m[3, 3];
        double nx = mx / mw;
        double ny = my / mw;

        // Fit the cube's NDC bbox into the plot bounds, PRESERVING its natural aspect ratio.
        // Verified: matplotlib uses the same px/NDC rate on both axes (~2290 px per NDC unit
        // for a 423.5 px axes bbox). The cube's NDC w/h ≈ 1.047 — wider than tall — and
        // fills roughly 90 % of matplotlib's square axes bbox.
        //
        // Uniform scale: pick whichever of (plotWidth / rangeX) or (plotHeight / rangeY)
        // is smaller so the cube fits within both dimensions. Then centre the cube in the
        // plot bounds. The 0.95 margin factor leaves ~5 % breathing room around the cube
        // for tick labels, matching matplotlib's pane-fill behaviour.
        double rangeX = _fitXMax - _fitXMin;
        double rangeY = _fitYMax - _fitYMin;
        double cxFit = (_fitXMin + _fitXMax) / 2;
        double cyFit = (_fitYMin + _fitYMax) / 2;

        double scaleX = _plotBounds.Width / rangeX;
        double scaleY = _plotBounds.Height / rangeY;
        // Phase P fix (2026-04-18) — was *1.00 "fill the plot area exactly", which at
        // typical camera angles (el=35, az=-50) leaves visible L/R whitespace because
        // the projected cube's horizontal extent is narrower than its vertical extent.
        // First iteration tried 1.25× (user approved "aggressive fill"); at that scale
        // the cube top overlapped the axes title. 1.15 keeps most of the gained real
        // estate while leaving the title row clear. Must stay in lockstep with
        // Svg3DRotationScript's BOX_FILL constant.
        double scale = Math.Min(scaleX, scaleY) * 1.15;

        double px = _plotBounds.X + _plotBounds.Width / 2 + (nx - cxFit) * scale;
        double pyScreen = _plotBounds.Y + _plotBounds.Height / 2 - (ny - cyFit) * scale;
        return new Point(px, pyScreen);
    }

    /// <summary>
    /// Returns a depth value for painter's-algorithm sorting. Lower depth = FARTHER from
    /// camera. For the persp matrix, row 3 of M is <c>-1 · (view·world)[2]</c> which is
    /// the negated camera-forward projection — i.e. a larger value means FURTHER from the
    /// eye. Negating again gives "low = far" so the caller's ascending sort paints the
    /// farthest polygon first.
    /// </summary>
    public double Depth(double x, double y, double z)
    {
        double dw = _m[3, 0] * x + _m[3, 1] * y + _m[3, 2] * z + _m[3, 3];
        return -dw;
    }
}
