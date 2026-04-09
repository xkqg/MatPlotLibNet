// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Drawing;

namespace MatPlotLibNet.Rendering.Algorithms;

/// <summary>Extracts iso-lines from a 2D scalar field using the marching-squares algorithm.</summary>
internal static class MarchingSquares
{
    /// <summary>One continuous contour polyline at a specific level.</summary>
    internal readonly record struct ContourLine(double Level, PointF[] Points);

    /// <summary>Extracts iso-lines for the given levels from a 2D scalar field.</summary>
    /// <param name="xGrid">X coordinates (length = number of columns in <paramref name="zGrid"/>).</param>
    /// <param name="yGrid">Y coordinates (length = number of rows in <paramref name="zGrid"/>).</param>
    /// <param name="zGrid">Z values, indexed as [row, col] where row → y and col → x.</param>
    /// <param name="levels">Iso-levels to extract.</param>
    /// <returns>One <see cref="ContourLine"/> per connected segment group per level.</returns>
    internal static ContourLine[] Extract(double[] xGrid, double[] yGrid, double[,] zGrid, double[] levels)
    {
        int rows = yGrid.Length;
        int cols = xGrid.Length;
        if (rows < 2 || cols < 2) return [];

        var result = new List<ContourLine>();

        foreach (double level in levels)
        {
            // Collect all raw segments for this level
            var segments = new List<(PointF A, PointF B)>();

            for (int row = 0; row < rows - 1; row++)
            {
                for (int col = 0; col < cols - 1; col++)
                {
                    double v00 = zGrid[row, col];         // bottom-left
                    double v10 = zGrid[row, col + 1];     // bottom-right
                    double v01 = zGrid[row + 1, col];     // top-left
                    double v11 = zGrid[row + 1, col + 1]; // top-right

                    // Corner coordinates
                    float x0 = (float)xGrid[col];
                    float x1 = (float)xGrid[col + 1];
                    float y0 = (float)yGrid[row];
                    float y1 = (float)yGrid[row + 1];

                    // 4-bit case index: bit3=TL, bit2=TR, bit1=BR, bit0=BL
                    int idx = (v01 >= level ? 8 : 0)
                            | (v11 >= level ? 4 : 0)
                            | (v10 >= level ? 2 : 0)
                            | (v00 >= level ? 1 : 0);

                    if (idx == 0 || idx == 15) continue; // all below or all above

                    // Edge midpoints via linear interpolation
                    PointF Bottom() => new(Lerp(x0, x1, v00, v10, level), y0);
                    PointF Top()    => new(Lerp(x0, x1, v01, v11, level), y1);
                    PointF Left()   => new(x0, Lerp(y0, y1, v00, v01, level));
                    PointF Right()  => new(x1, Lerp(y0, y1, v10, v11, level));

                    // Cases: two segments per cell (saddle cases resolved with consistent orientation)
                    switch (idx)
                    {
                        case 1:  case 14: segments.Add((Bottom(), Left()));   break;
                        case 2:  case 13: segments.Add((Bottom(), Right()));  break;
                        case 3:  case 12: segments.Add((Left(),   Right()));  break;
                        case 4:  case 11: segments.Add((Top(),    Right()));  break;
                        case 6:  case 9:  segments.Add((Bottom(), Top()));    break;
                        case 7:  case 8:  segments.Add((Top(),    Left()));   break;
                        // Saddle cases: split into two segments using average
                        case 5:
                            segments.Add((Bottom(), Left()));
                            segments.Add((Top(),    Right()));
                            break;
                        case 10:
                            segments.Add((Bottom(), Right()));
                            segments.Add((Top(),    Left()));
                            break;
                    }
                }
            }

            // Join segments into polylines
            foreach (var polyline in JoinSegments(segments))
                result.Add(new ContourLine(level, polyline));
        }

        return [.. result];
    }

    // --- Private helpers ---

    private static float Lerp(float a, float b, double va, double vb, double level)
    {
        double t = (vb - va) == 0 ? 0.5 : (level - va) / (vb - va);
        return (float)(a + t * (b - a));
    }

    /// <summary>Greedily chains segments that share an endpoint into polylines.</summary>
    private static IEnumerable<PointF[]> JoinSegments(List<(PointF A, PointF B)> segments)
    {
        if (segments.Count == 0) yield break;

        const float Eps = 1e-6f;
        var used = new bool[segments.Count];

        for (int start = 0; start < segments.Count; start++)
        {
            if (used[start]) continue;
            used[start] = true;
            var chain = new List<PointF> { segments[start].A, segments[start].B };

            bool extended = true;
            while (extended)
            {
                extended = false;
                PointF tail = chain[^1];
                for (int i = 0; i < segments.Count; i++)
                {
                    if (used[i]) continue;
                    if (Near(segments[i].A, tail, Eps))
                    {
                        chain.Add(segments[i].B);
                        used[i] = true; extended = true; break;
                    }
                    if (Near(segments[i].B, tail, Eps))
                    {
                        chain.Add(segments[i].A);
                        used[i] = true; extended = true; break;
                    }
                }
            }

            yield return [.. chain];
        }
    }

    private static bool Near(PointF a, PointF b, float eps)
        => Math.Abs(a.X - b.X) < eps && Math.Abs(a.Y - b.Y) < eps;
}
