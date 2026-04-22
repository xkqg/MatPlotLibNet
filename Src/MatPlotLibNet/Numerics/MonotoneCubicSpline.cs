// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>
/// Fritsch-Carlson monotone cubic interpolation.
/// Produces a smooth curve through the data points that never overshoots —
/// monotone intervals in the input remain monotone in the output.
/// </summary>
internal static class MonotoneCubicSpline
{
    /// <summary>
    /// Expands <paramref name="x"/>/<paramref name="y"/> to a smooth curve using monotone cubic interpolation.
    /// Returns <c>(n−1)×<paramref name="resolution"/>+1</c> output points.
    /// </summary>
    /// <param name="x">Strictly increasing X values. Must have at least 2 elements.</param>
    /// <param name="y">Y values, same length as <paramref name="x"/>.</param>
    /// <param name="resolution">Number of output sub-segments per input interval. Default 10.</param>
    /// <returns>
    /// An <see cref="XYCurve"/> of interpolated X and Y arrays of length <c>(n−1)×resolution+1</c>.
    /// Returns the input unchanged when <paramref name="x"/> has fewer than 2 elements
    /// or <paramref name="resolution"/> ≤ 1.
    /// </returns>
    internal static XYCurve Interpolate(double[] x, double[] y, int resolution = 10)
    {
        int n = x.Length;
        if (n < 2 || resolution <= 1) return new(x, y);

        // Step 1: compute secant slopes
        var d = new double[n - 1];
        for (int i = 0; i < n - 1; i++)
        {
            double dx = x[i + 1] - x[i];
            d[i] = dx == 0 ? 0.0 : (y[i + 1] - y[i]) / dx;
        }

        // Step 2: initialize tangents (Fritsch-Carlson)
        var m = new double[n];
        m[0] = d[0];
        m[n - 1] = d[n - 2];
        for (int i = 1; i < n - 1; i++)
            m[i] = (d[i - 1] + d[i]) / 2;

        // Step 3: enforce monotonicity
        for (int i = 0; i < n - 1; i++)
        {
            if (d[i] == 0)
            {
                m[i] = 0;
                m[i + 1] = 0;
                continue;
            }
            double alpha = m[i] / d[i];
            double beta  = m[i + 1] / d[i];
            double sq = alpha * alpha + beta * beta;
            if (sq > 9)
            {
                double tau = 3 / Math.Sqrt(sq);
                m[i]     = tau * alpha * d[i];
                m[i + 1] = tau * beta  * d[i];
            }
        }

        // Step 4: evaluate Hermite cubic at resolution sub-points per interval
        int outLen = (n - 1) * resolution + 1;
        var outX = new double[outLen];
        var outY = new double[outLen];
        int idx = 0;

        for (int i = 0; i < n - 1; i++)
        {
            double x0 = x[i], x1 = x[i + 1];
            double y0 = y[i], y1 = y[i + 1];
            double m0 = m[i], m1 = m[i + 1];
            double h = x1 - x0;

            int count = (i < n - 2) ? resolution : resolution + 1; // include last point on final segment
            for (int j = 0; j < count; j++)
            {
                double t = (double)j / resolution;
                double t2 = t * t;
                double t3 = t2 * t;
                // Hermite basis functions
                double h00 = 2 * t3 - 3 * t2 + 1;
                double h10 = t3 - 2 * t2 + t;
                double h01 = -2 * t3 + 3 * t2;
                double h11 = t3 - t2;

                outX[idx] = x0 + t * h;
                outY[idx] = h00 * y0 + h10 * h * m0 + h01 * y1 + h11 * h * m1;
                idx++;
            }
        }

        return new(outX, outY);
    }
}
