// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Represents a triangulated mesh with flat triangle index array and point coordinates.</summary>
/// <param name="Triangles">Flat triangle index array — every 3 consecutive integers define one triangle.</param>
/// <param name="X">X coordinates of the mesh vertices.</param>
/// <param name="Y">Y coordinates of the mesh vertices.</param>
public sealed record TriMesh(int[] Triangles, double[] X, double[] Y);

/// <summary>Delaunay triangulation using Bowyer-Watson incremental insertion.</summary>
public static class Delaunay
{
    private const double Epsilon = 1e-10;

    /// <summary>Computes the Delaunay triangulation of a 2D point set.</summary>
    /// <param name="x">X coordinates of the points.</param>
    /// <param name="y">Y coordinates of the points. Must be the same length as <paramref name="x"/>.</param>
    /// <returns>A <see cref="TriMesh"/> with the triangulation result.</returns>
    public static TriMesh Triangulate(double[] x, double[] y)
    {
        int n = x.Length;
        if (n < 3) return new TriMesh([], x, y);

        // Jitter near-duplicate or collinear points to prevent degenerate triangulations
        x = JitterCollinear(x, y);

        // Bowyer-Watson: start with a super-triangle that encloses all points
        double xMin = x.Min(), xMax = x.Max();
        double yMin = y.Min(), yMax = y.Max();
        double dx = xMax - xMin, dy = yMax - yMin;
        double delta = Math.Max(dx, dy) * 10;
        double mx = (xMin + xMax) / 2, my = (yMin + yMax) / 2;

        // Super-triangle vertices appended at indices n, n+1, n+2
        double[] px = [..x, mx - delta, mx, mx + delta];
        double[] py = [..y, my - 3 * delta, my + delta, my - 3 * delta];

        // Initial super-triangle in CCW order (required for determinant circumcircle test)
        var triangles = new List<(int a, int b, int c)>
        {
            (n, n + 2, n + 1)
        };

        for (int i = 0; i < n; i++)
        {
            double xi = px[i], yi = py[i];
            var badTriangles = new List<(int a, int b, int c)>();
            foreach (var tri in triangles)
            {
                if (InCircumcircle(xi, yi, px[tri.a], py[tri.a], px[tri.b], py[tri.b], px[tri.c], py[tri.c]))
                    badTriangles.Add(tri);
            }

            // Find boundary polygon of bad triangles (edges not shared by 2 bad triangles)
            var boundary = new List<(int a, int b)>();
            foreach (var tri in badTriangles)
            {
                (int, int)[] edges = [(tri.a, tri.b), (tri.b, tri.c), (tri.c, tri.a)];
                foreach (var e in edges)
                {
                    bool shared = badTriangles.Any(other => other != tri && TriHasEdge(other, e.Item1, e.Item2));
                    if (!shared) boundary.Add(e);
                }
            }

            foreach (var bad in badTriangles)
                triangles.Remove(bad);

            foreach (var (a, b) in boundary)
                triangles.Add((a, b, i));
        }

        // Remove triangles that use super-triangle vertices
        triangles.RemoveAll(t => t.a >= n || t.b >= n || t.c >= n);

        var flat = new int[triangles.Count * 3];
        for (int i = 0; i < triangles.Count; i++)
        {
            flat[i * 3] = triangles[i].a;
            flat[i * 3 + 1] = triangles[i].b;
            flat[i * 3 + 2] = triangles[i].c;
        }

        return new TriMesh(flat, x, y);
    }

    private static bool InCircumcircle(double px, double py,
        double ax, double ay, double bx, double by, double cx, double cy)
    {
        // Determinant test
        double ax_ = ax - px, ay_ = ay - py;
        double bx_ = bx - px, by_ = by - py;
        double cx_ = cx - px, cy_ = cy - py;
        double det = ax_ * (by_ * (cx_ * cx_ + cy_ * cy_) - cy_ * (bx_ * bx_ + by_ * by_))
                   - ay_ * (bx_ * (cx_ * cx_ + cy_ * cy_) - cx_ * (bx_ * bx_ + by_ * by_))
                   + (ax_ * ax_ + ay_ * ay_) * (bx_ * cy_ - by_ * cx_);
        return det > 0;
    }

    private static bool TriHasEdge((int a, int b, int c) t, int u, int v)
    {
        var e = (u, v);
        return (t.a == e.Item1 && t.b == e.Item2) || (t.b == e.Item1 && t.a == e.Item2)
            || (t.b == e.Item1 && t.c == e.Item2) || (t.c == e.Item1 && t.b == e.Item2)
            || (t.c == e.Item1 && t.a == e.Item2) || (t.a == e.Item1 && t.c == e.Item2);
    }

    private static double[] JitterCollinear(double[] x, double[] y)
    {
        // Detect near-collinear or duplicate points and add small epsilon jitter
        double[] jx = (double[])x.Clone();
        double scale = (x.Max() - x.Min() + y.Max() - y.Min()) * Epsilon;
        if (scale == 0) scale = Epsilon;

        for (int i = 0; i < x.Length; i++)
        {
            // Simple deterministic jitter based on index
            jx[i] += (i % 7 - 3) * scale;
        }
        return jx;
    }
}
