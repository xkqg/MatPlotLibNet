// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Pins <see cref="Projection3D"/> to its arithmetic, bit for bit: the pixel, the depth and the normalised point of
/// a sweep of cameras, perspective and orthographic, over ranges low to high, high to low and of no width, equal the
/// reference the projection was written as (matplotlib's world, view and perspective matrices composed into one, the
/// projected point placed by matplotlib's fixed view limits, -0.95 / dist to 0.9 / dist, in the square centred in the plot). A 3-D picture is written from these numbers, so a projection that works
/// them out with less work must work out the same ones.</summary>
public sealed class Projection3DArithmeticPinsTests
{
    public static TheoryData<double, double, double?> Cameras => new()
    {
        { 30, -60, null }, { 35, -50, null }, { 0, 0, null }, { 90, 0, null }, { -90, 45, null }, { 12.5, 137, null },
        { 30, -60, 8 }, { 60, 210, 3 }, { -20, 90, 1 },
    };

    [Theory]
    [MemberData(nameof(Cameras))]
    public void EveryPointOfASweep_IsProjectedAsTheReferenceProjectsIt(double elevation, double azimuth, double? distance)
    {
        var plot = new Rect(37.5, 21.25, 410.3, 377.9);
        foreach (var (x0, x1, y0, y1, z0, z1) in Ranges)
        {
            var projection = new Projection3D(elevation, azimuth, plot, x0, x1, y0, y1, z0, z1, distance);
            var reference = new Reference(elevation, azimuth, plot, x0, x1, y0, y1, z0, z1, distance);
            foreach (var (x, y, z) in PointsIn(projection.DataBox))
            {
                Assert.Equal(reference.Project(x, y, z), projection.Project(x, y, z));
                Assert.Equal(reference.Depth(x, y, z), projection.Depth(x, y, z));
            }
        }
    }

    /// <summary>A box far from 0 whose width is a millisecond of dates projects as the same box moved to 0: every point is
    /// measured from the corner the box starts at, so the date's large value never meets the small width.</summary>
    [Fact]
    public void ABoxFarFromZero_IsProjectedAsTheSameBoxAtZero()
    {
        double start = new DateTime(2026, 1, 1).ToOADate();
        double bit = Math.BitIncrement(start) - start;
        double step = Math.Round(1e-4 / 86_400 / bit) * bit;
        var plot = new Rect(37.5, 21.25, 410.3, 377.9);
        var far = new Projection3D(30, -60, plot, start, start + 10 * step, 0, 1, 0, 1);
        var near = new Projection3D(30, -60, plot, 0, 10 * step, 0, 1, 0, 1);

        for (int k = 0; k <= 10; k++)
        {
            var a = far.Project(start + k * step, 0.5, 0.25);
            var b = near.Project(k * step, 0.5, 0.25);
            Assert.Equal(b.X, a.X, 1e-9);
            Assert.Equal(b.Y, a.Y, 1e-9);
            Assert.Equal(near.Depth(k * step, 0.5, 0.25), far.Depth(start + k * step, 0.5, 0.25), 1e-12);
        }

        Assert.Equal(near.Faces, far.Faces);
    }

    private static readonly (double, double, double, double, double, double)[] Ranges =
    [
        (0, 10, 0, 5, -1, 1), (-3.7, 12.1, 100, 200, 0.001, 0.002), (10, 0, 5, 0, 1, -1), (2, 2, 0, 1, 3, 3),
    ];

    private static IEnumerable<(double, double, double)> PointsIn(Box3D box)
    {
        for (int i = 0; i <= 4; i++)
        for (int j = 0; j <= 4; j++)
        for (int k = 0; k <= 4; k++)
            yield return (box.X.Lo + (box.X.Hi - box.X.Lo) * i / 4.0, box.Y.Lo + (box.Y.Hi - box.Y.Lo) * j / 4.0, box.Z.Lo + (box.Z.Hi - box.Z.Lo) * k / 4.0);
    }

    /// <summary>The projection written plainly: the composed matrix in a 4 x 4 array, applied to the point measured from the
    /// box's first corner, and the fit worked out on every call. Kept here only as the reference the class is pinned to.</summary>
    private sealed class Reference
    {
        private const double Ax = 25.0 / 21.0, Ay = 25.0 / 21.0, Az = 25.0 / 28.0;
        private readonly double[,] _m = new double[4, 4];
        private readonly Rect _plot;
        private readonly double _dist, _x0, _y0, _z0;

        internal Reference(double elevation, double azimuth, Rect plot, double xMin, double xMax, double yMin, double yMax,
            double zMin, double zMax, double? distance)
        {
            var xr = new Range1D(xMin, xMax).Normalized();
            var yr = new Range1D(yMin, yMax).Normalized();
            var zr = new Range1D(zMin, zMax).Normalized();
            xMin = xr.Lo; xMax = xr.Hi; yMin = yr.Lo; yMax = yr.Hi; zMin = zr.Lo; zMax = zr.Hi;
            _plot = plot;
            double dist = distance.HasValue ? Math.Max(2.0, distance.Value) : 10.0;
            double el = elevation * Math.PI / 180, az = azimuth * Math.PI / 180;
            double cosEl = Math.Cos(el), sinEl = Math.Sin(el), cosAz = Math.Cos(az), sinAz = Math.Sin(az);
            double dx = xMax - xMin, dy = yMax - yMin, dz = zMax - zMin;
            double sx = Ax / dx, sy = Ay / dy, sz = Az / dz;
            _x0 = xMin; _y0 = yMin; _z0 = zMin;
            double ex = 0.5 * Ax + dist * 1.0 * cosEl * cosAz, ey = 0.5 * Ay + dist * 1.0 * cosEl * sinAz, ez = 0.5 * Az + dist * 1.0 * sinEl;
            double ux = -sinAz, uy = cosAz, uz = 0;
            double vx = -sinEl * cosAz, vy = -sinEl * sinAz, vz = cosEl;
            double wx = cosEl * cosAz, wy = cosEl * sinAz, wz = sinEl;
            double uT = -(ux * ex + uy * ey + uz * ez), vT = -(vx * ex + vy * ey + vz * ez), wT = -(wx * ex + wy * ey + wz * ez);
            double vw00 = ux * sx, vw01 = uy * sy, vw02 = uz * sz, vw03 = uT;
            double vw10 = vx * sx, vw11 = vy * sy, vw12 = vz * sz, vw13 = vT;
            double vw20 = wx * sx, vw21 = wy * sy, vw22 = wz * sz, vw23 = wT;
            const double a = 1.0;
            double e = 1.0, zf = -dist, zb = dist;
            double b = (zf + zb) / (zf - zb), c = -2 * (zf * zb) / (zf - zb);
            _m[0, 0] = e * vw00; _m[0, 1] = e * vw01; _m[0, 2] = e * vw02; _m[0, 3] = e * vw03;
            _m[1, 0] = (e / a) * vw10; _m[1, 1] = (e / a) * vw11; _m[1, 2] = (e / a) * vw12; _m[1, 3] = (e / a) * vw13;
            _m[2, 0] = b * vw20; _m[2, 1] = b * vw21; _m[2, 2] = b * vw22; _m[2, 3] = b * vw23 + c;
            _m[3, 0] = -vw20; _m[3, 1] = -vw21; _m[3, 2] = -vw22; _m[3, 3] = -vw23;
            _dist = dist;
        }

        internal Point Project(double x, double y, double z)
        {
            x -= _x0; y -= _y0; z -= _z0;
            double mx = _m[0, 0] * x + _m[0, 1] * y + _m[0, 2] * z + _m[0, 3];
            double my = _m[1, 0] * x + _m[1, 1] * y + _m[1, 2] * z + _m[1, 3];
            double mw = _m[3, 0] * x + _m[3, 1] * y + _m[3, 2] * z + _m[3, 3];
            double nx = mx / mw, ny = my / mw;
            double side = Math.Min(_plot.Width, _plot.Height), scale = side * _dist / 1.85;
            double originX = _plot.X + (_plot.Width - side) / 2 + side * 0.95 / 1.85;
            double originY = _plot.Y + (_plot.Height - side) / 2 + side * 0.9 / 1.85;
            return new Point(originX + nx * scale, originY - ny * scale);
        }

        internal double Depth(double x, double y, double z) =>
            -(_m[3, 0] * (x - _x0) + _m[3, 1] * (y - _y0) + _m[3, 2] * (z - _z0) + _m[3, 3]);
    }
}
