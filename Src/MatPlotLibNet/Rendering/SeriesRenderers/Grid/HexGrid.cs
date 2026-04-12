// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Flat-top hexagonal grid utilities for hexbin series rendering.</summary>
internal static class HexGrid
{
    private const double Sqrt3 = 1.7320508075688772; // Math.Sqrt(3)

    /// <summary>Computes the hexagon radius (center-to-vertex) in data units for the given grid size.</summary>
    internal static double ComputeHexSize(double xMin, double xMax, int gridSize)
    {
        double span = xMax - xMin;
        return span / Math.Max(gridSize * 1.5, 1.0);
    }

    /// <summary>
    /// Bins scatter data into a flat-top hexagonal grid using axial (q, r) coordinates.
    /// Returns a dictionary mapping each occupied hex cell to its point count.
    /// </summary>
    internal static Dictionary<(int q, int r), int> ComputeHexBins(
        double[] x, double[] y, double xMin, double xMax, double yMin, double yMax, int gridSize)
    {
        var bins = new Dictionary<(int, int), int>();
        if (x.Length == 0) return bins;

        double hexSize = ComputeHexSize(xMin, xMax, gridSize);
        if (hexSize <= 0) hexSize = 1.0;

        int n = Math.Min(x.Length, y.Length);
        for (int i = 0; i < n; i++)
        {
            double lx = x[i] - xMin;
            double ly = y[i] - yMin;

            // Flat-top axial coordinate conversion
            double qFrac = (2.0 / 3.0 * lx) / hexSize;
            double rFrac = (-1.0 / 3.0 * lx + Sqrt3 / 3.0 * ly) / hexSize;

            var (q, r) = CubeRound(qFrac, rFrac);

            if (bins.TryGetValue((q, r), out int count))
                bins[(q, r)] = count + 1;
            else
                bins[(q, r)] = 1;
        }
        return bins;
    }

    /// <summary>
    /// Returns the 6 vertices of a flat-top regular hexagon centered at (<paramref name="cx"/>, <paramref name="cy"/>)
    /// with center-to-vertex radius <paramref name="hexSize"/>.
    /// </summary>
    /// <returns>Array of 6 (X, Y) tuples in order.</returns>
    internal static (double X, double Y)[] HexagonVertices(double cx, double cy, double hexSize)
    {
        var verts = new (double X, double Y)[6];
        for (int i = 0; i < 6; i++)
        {
            double angle = Math.PI / 3.0 * i; // flat-top: vertex 0 at angle 0°
            verts[i] = (cx + hexSize * Math.Cos(angle), cy + hexSize * Math.Sin(angle));
        }
        return verts;
    }

    /// <summary>
    /// Returns the data-space center of the hex at axial coordinates (<paramref name="q"/>, <paramref name="r"/>),
    /// offset by (<paramref name="xOffset"/>, <paramref name="yOffset"/>).
    /// </summary>
    internal static (double X, double Y) HexCenter(int q, int r, double hexSize, double xOffset = 0, double yOffset = 0)
    {
        double x = hexSize * 1.5 * q + xOffset;
        double y = hexSize * Sqrt3 * (r + q * 0.5) + yOffset;
        return (x, y);
    }

    /// <summary>Rounds fractional axial coordinates to the nearest integer hex cell using cube-coordinate rounding.</summary>
    private static (int q, int r) CubeRound(double qFrac, double rFrac)
    {
        double sFrac = -qFrac - rFrac;
        int rq = (int)Math.Round(qFrac);
        int rr = (int)Math.Round(rFrac);
        int rs = (int)Math.Round(sFrac);
        double dq = Math.Abs(rq - qFrac);
        double dr = Math.Abs(rr - rFrac);
        double ds = Math.Abs(rs - sFrac);
        if (dq > dr && dq > ds) rq = -rr - rs;
        else if (dr > ds) rr = -rq - rs;
        return (rq, rr);
    }
}
