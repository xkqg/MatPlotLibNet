// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The one owner of what a flat list of triangles must be and of the edges it draws: three corners to a triangle, each
/// a point from 0 to one less than there are points. The list a caller gives is held to it where it is accepted (the triangle
/// plot, a triangle-coloured mesh, a mesh read from JSON), beside <see cref="Delaunay"/>, which makes the list when none is
/// given.</summary>
internal static class TriangleListExtensions
{
    /// <summary>The corners of a triangle: the indices three to a triangle in a flat list.</summary>
    internal const int CornersPerTriangle = 3;

    extension(int[] triangles)
    {
        /// <summary>Refuses a list of triangles that is not whole triangles, or that names a point there is not: the corners of a
        /// triangle are indices into the points, counted from 0.</summary>
        /// <param name="points">How many points there are.</param>
        /// <exception cref="ArgumentException">The list does not hold a multiple of three indices (the message names how many it
        /// holds).</exception>
        /// <exception cref="ArgumentOutOfRangeException">An index is below 0 or not below <paramref name="points"/> (the message names
        /// its place in the list).</exception>
        internal void ThrowIfNotCornersOf(int points)
        {
            if (triangles.Length % CornersPerTriangle != 0)
            {
                throw new ArgumentException(
                    string.Invariant($"triangles holds {triangles.Length} indices; a triangle has three corners, so give a multiple of three."), nameof(triangles));
            }

            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i].ThrowIfNotIndexOf(points, nameof(triangles), i, static place => string.Invariant($"A corner, triangles[{place}],"));
            }
        }

        /// <summary>The edges of these triangles as the segments of one path, drawn at the given coordinates: each edge once
        /// however many triangles share it and whichever way round each runs it, in the order the triangles first meet them
        /// (a triangle's three sides in the order of its corners), and none for a corner a triangle names twice. An edge to a
        /// point that cannot be placed is left out by the path.</summary>
        /// <param name="x">The x coordinate of each point.</param>
        /// <param name="y">The y coordinate of each point, one per x position.</param>
        /// <returns>The segments, three points for each edge; empty for no triangles.</returns>
        internal SegmentPath EdgesAmong(double[] x, double[] y)
        {
            // A triangle has three sides, so there are at most as many edges as there are whole corners.
            int corners = triangles.Length / CornersPerTriangle * CornersPerTriangle;
            var met = new HashSet<long>(corners);
            int[] starts = new int[corners], ends = new int[corners];
            int edges = 0;
            for (int first = 0; first < corners; first += CornersPerTriangle)
            {
                for (int side = 0; side < CornersPerTriangle; side++)
                {
                    int a = triangles[first + side], b = triangles[first + (side + 1) % CornersPerTriangle];
                    if (a != b && met.Add(Math.Min(a, b) * (long)x.Length + Math.Max(a, b)))
                    {
                        starts[edges] = a;
                        ends[edges++] = b;
                    }
                }
            }

            double[] x0 = new double[edges], y0 = new double[edges], x1 = new double[edges], y1 = new double[edges];
            for (int i = 0; i < edges; i++)
            {
                x0[i] = x[starts[i]];
                y0[i] = y[starts[i]];
                x1[i] = x[ends[i]];
                y1[i] = y[ends[i]];
            }

            return SegmentPath.Of(x0, y0, x1, y1);
        }
    }
}
