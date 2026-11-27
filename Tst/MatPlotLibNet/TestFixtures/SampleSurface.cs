// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>A surface the tests draw: the axes the values are given over and the value at each crossing, <c>Z[y, x]</c>. The same few
/// surfaces stood in a dozen test classes, each written out in place.</summary>
/// <param name="X">The values along x.</param>
/// <param name="Y">The values along y.</param>
/// <param name="Z">The height at each (y, x).</param>
public readonly record struct SampleSurface(double[] X, double[] Y, double[,] Z)
{
    /// <summary>matplotlib's <c>peaks</c> function, <c>(1 - x/2 + x^5 + y^3) exp(-x^2 - y^2)</c>, over -3 to 3 on both axes.</summary>
    /// <param name="n">How many values lie along each axis.</param>
    public static SampleSurface Peaks(int n = 100)
    {
        double[] x = Spaced(n);
        return PeaksOver(x, x);
    }

    /// <summary>The <c>peaks</c> function over the axes given.</summary>
    /// <param name="x">The values along x.</param>
    /// <param name="y">The values along y.</param>
    public static SampleSurface PeaksOver(double[] x, double[] y) =>
        Over(x, y, (px, py) => (1 - px / 2 + Math.Pow(px, 5) + Math.Pow(py, 3)) * Math.Exp(-px * px - py * py));

    /// <summary>The cardinal sine of the distance from the origin, <c>sin(r) / r</c> (1 at the origin), over -3 to 3 on both axes.</summary>
    /// <param name="n">How many values lie along each axis.</param>
    public static SampleSurface Sinc(int n = 20)
    {
        double[] x = Spaced(n);
        return Over(x, x, (px, py) =>
        {
            double r = Math.Sqrt((px * px) + (py * py));
            return r < 1e-10 ? 1.0 : Math.Sin(r) / r;
        });
    }

    /// <summary>The sine of the distance from the origin, <c>sin(r)</c>, over the axes given.</summary>
    /// <param name="x">The values along x.</param>
    /// <param name="y">The values along y.</param>
    public static SampleSurface SinR(double[] x, double[] y) => Over(x, y, (px, py) => Math.Sin(Math.Sqrt((px * px) + (py * py))));

    /// <summary>A ramp, <c>x + y</c>, over the axes given.</summary>
    /// <param name="x">The values along x.</param>
    /// <param name="y">The values along y.</param>
    public static SampleSurface Ramp(double[] x, double[] y) => Over(x, y, (px, py) => px + py);

    /// <summary>The distance from the middle of the grid, <c>n</c> by <c>n</c>, over the axes 0 to n - 1: concentric rings.</summary>
    /// <param name="n">How many values lie along each axis.</param>
    public static SampleSurface Rings(int n = 11)
    {
        double[] axis = [.. Enumerable.Range(0, n).Select(i => (double)i)];
        return Over(axis, axis, (px, py) => Math.Sqrt(((px - (n / 2.0)) * (px - (n / 2.0))) + ((py - (n / 2.0)) * (py - (n / 2.0)))));
    }

    /// <summary>Nine heights, 0 to 8 row by row, over the axes 0, 5 and 10.</summary>
    public static SampleSurface ThreeByThree() =>
        new([0.0, 5.0, 10.0], [0.0, 5.0, 10.0], new double[,] { { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 } });

    private static double[] Spaced(int n) => [.. Enumerable.Range(0, n).Select(i => -3.0 + (6.0 * i / (n - 1)))];

    private static SampleSurface Over(double[] x, double[] y, Func<double, double, double> height)
    {
        var z = new double[y.Length, x.Length];
        for (int iy = 0; iy < y.Length; iy++)
        {
            for (int ix = 0; ix < x.Length; ix++)
            {
                z[iy, ix] = height(x[ix], y[iy]);
            }
        }

        return new SampleSurface(x, y, z);
    }
}
