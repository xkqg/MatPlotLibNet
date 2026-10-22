// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Drawing;

namespace MatPlotLibNet.Rendering.Algorithms;

/// <summary>Extracts iso-lines and filled bands from a 2D scalar field using the marching-squares algorithm.</summary>
internal static class MarchingSquares
{
    /// <summary>One continuous contour polyline at a specific level.</summary>
    internal readonly record struct ContourLine(double Level, PointF[] Points);

    /// <summary>One filled band between two consecutive iso-levels, as a set of closed polygons.</summary>
    internal readonly record struct ContourBand(double LevelLow, double LevelHigh, PointF[][] Polygons);

    /// <summary>One oriented edge segment between two points on the iso surface.</summary>
    private readonly record struct Segment(PointF A, PointF B);

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
            var segments = new List<Segment>();

            for (int row = 0; row < rows - 1; row++)
            {
                for (int col = 0; col < cols - 1; col++)
                {
                    if (!zGrid.IsDrawableCell(xGrid, yGrid, row, col)) continue; // masked, as matplotlib masks it

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
                        case 1:  case 14: segments.Add(new(Bottom(), Left()));   break;
                        case 2:  case 13: segments.Add(new(Bottom(), Right()));  break;
                        case 3:  case 12: segments.Add(new(Left(),   Right()));  break;
                        case 4:  case 11: segments.Add(new(Top(),    Right()));  break;
                        case 6:  case 9:  segments.Add(new(Bottom(), Top()));    break;
                        case 7:  case 8:  segments.Add(new(Top(),    Left()));   break;
                        // Saddle cases: split into two segments using average
                        case 5:
                            segments.Add(new(Bottom(), Left()));
                            segments.Add(new(Top(),    Right()));
                            break;
                        case 10:
                            segments.Add(new(Bottom(), Right()));
                            segments.Add(new(Top(),    Left()));
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

    /// <summary>
    /// Extracts filled contour bands for a 2D scalar field.
    /// Each band represents the region between two consecutive evenly-spaced iso-levels.
    /// </summary>
    /// <param name="xGrid">X coordinates (length = columns of <paramref name="zGrid"/>).</param>
    /// <param name="yGrid">Y coordinates (length = rows of <paramref name="zGrid"/>).</param>
    /// <param name="zGrid">Z values indexed as [row, col].</param>
    /// <param name="levels">Number of iso-levels (produces <paramref name="levels"/>-1 bands).</param>
    /// <returns>Bands in ascending level order, each with closed polygon outlines.</returns>
    internal static ContourBand[] ExtractBands(double[] xGrid, double[] yGrid, double[,] zGrid, int levels)
    {
        if (levels < 2 || !HasVariation(xGrid, yGrid, zGrid, out var z)) return [];

        // The levels span the values that are there: a missing value (NaN) and an infinity are left out.
        return BandsBetween(xGrid, yGrid, zGrid, ContourLevels.Spanning(z.Lo, z.Hi, levels));
    }

    /// <summary>
    /// Extracts filled contour bands between iso-levels that are given. Each band is the region between two consecutive
    /// <paramref name="thresholds"/>, whatever range the grid spans: a threshold the grid never reaches bounds a band that
    /// draws nothing.
    /// </summary>
    /// <param name="xGrid">X coordinates (length = columns of <paramref name="zGrid"/>).</param>
    /// <param name="yGrid">Y coordinates (length = rows of <paramref name="zGrid"/>).</param>
    /// <param name="zGrid">Z values indexed as [row, col].</param>
    /// <param name="thresholds">The iso-levels, ascending and each once (see <see cref="ContourLevels.ExplicitLevels"/>);
    /// <c>thresholds.Length - 1</c> bands.</param>
    /// <returns>Bands in ascending level order, each with closed polygon outlines; none when fewer than two thresholds are
    /// given or the grid has no variation to fill.</returns>
    internal static ContourBand[] ExtractBands(double[] xGrid, double[] yGrid, double[,] zGrid, double[] thresholds) =>
        thresholds.Length < 2 || !HasVariation(xGrid, yGrid, zGrid, out _) ? [] : BandsBetween(xGrid, yGrid, zGrid, thresholds);

    /// <summary>Whether the grid can be filled at all: it has two rows and two columns, a value that can be drawn, and not
    /// every such value the same.</summary>
    private static bool HasVariation(double[] xGrid, double[] yGrid, double[,] zGrid, out Range1D range)
    {
        range = default;
        if (yGrid.Length < 2 || xGrid.Length < 2 || zGrid.DrawableRange() is not { } z)
        {
            return false;
        }

        range = z;
        // Flat grid — no variation to fill
        return z.Hi - z.Lo >= 1e-12;
    }

    /// <summary>The band between each pair of consecutive thresholds.</summary>
    private static ContourBand[] BandsBetween(double[] xGrid, double[] yGrid, double[,] zGrid, double[] thresholds)
    {
        int rows = yGrid.Length;
        int cols = xGrid.Length;

        var bands = new ContourBand[thresholds.Length - 1];
        for (int b = 0; b < bands.Length; b++)
        {
            double lo = thresholds[b];
            double hi = thresholds[b + 1];

            // Collect segments that border cells where lo <= z < hi
            var segments = new List<Segment>();
            CollectBandBoundary(xGrid, yGrid, zGrid, lo, hi, rows, cols, segments);

            PointF[][] polygons = [.. JoinSegments(segments).Select(p => ClosePolygon(p))];
            bands[b] = new ContourBand(lo, hi, polygons);
        }

        return bands;
    }

    /// <summary>
    /// Collects boundary segments for cells whose z-values overlap [lo, hi).
    /// Uses the marching-squares edge between the "above lo" and "above hi" masks.
    /// </summary>
    private static void CollectBandBoundary(
        double[] xGrid, double[] yGrid, double[,] zGrid,
        double lo, double hi, int rows, int cols,
        List<Segment> segments)
    {
        // We run Extract once at lo and once at hi, then merge — simpler and avoids
        // bespoke polygon-fill logic.  The band polygon boundary = iso(lo) ∪ iso(hi)
        // plus grid-boundary edges where needed.  For SVG painter's algorithm we only
        // need the *outer* boundary of the band; collecting both iso-lines gives us
        // all interior edges; the renderer fills the whole area with the band color on
        // top of previous bands, so closing is optional — but we do it for completeness.

        // Segments at the low boundary (lo iso-line)
        foreach (var seg in GetRawSegments(xGrid, yGrid, zGrid, lo, rows, cols))
            segments.Add(seg);

        // Segments at the high boundary (hi iso-line)
        foreach (var seg in GetRawSegments(xGrid, yGrid, zGrid, hi, rows, cols))
            segments.Add(seg);
    }

    /// <summary>Returns all raw marching-squares edge segments for a single iso-level.</summary>
    private static IEnumerable<Segment> GetRawSegments(
        double[] xGrid, double[] yGrid, double[,] zGrid, double level, int rows, int cols)
    {
        for (int row = 0; row < rows - 1; row++)
        {
            for (int col = 0; col < cols - 1; col++)
            {
                if (!zGrid.IsDrawableCell(xGrid, yGrid, row, col)) continue; // masked, as matplotlib masks it

                double v00 = zGrid[row,     col];
                double v10 = zGrid[row,     col + 1];
                double v01 = zGrid[row + 1, col];
                double v11 = zGrid[row + 1, col + 1];

                float x0 = (float)xGrid[col];
                float x1 = (float)xGrid[col + 1];
                float y0 = (float)yGrid[row];
                float y1 = (float)yGrid[row + 1];

                int idx = (v01 >= level ? 8 : 0)
                        | (v11 >= level ? 4 : 0)
                        | (v10 >= level ? 2 : 0)
                        | (v00 >= level ? 1 : 0);

                if (idx == 0 || idx == 15) continue;

                PointF Bottom() => new(Lerp(x0, x1, v00, v10, level), y0);
                PointF Top()    => new(Lerp(x0, x1, v01, v11, level), y1);
                PointF Left()   => new(x0, Lerp(y0, y1, v00, v01, level));
                PointF Right()  => new(x1, Lerp(y0, y1, v10, v11, level));

                switch (idx)
                {
                    case 1:  case 14: yield return new(Bottom(), Left());  break;
                    case 2:  case 13: yield return new(Bottom(), Right()); break;
                    case 3:  case 12: yield return new(Left(),   Right()); break;
                    case 4:  case 11: yield return new(Top(),    Right()); break;
                    case 6:  case 9:  yield return new(Bottom(), Top());   break;
                    case 7:  case 8:  yield return new(Top(),    Left());  break;
                    case 5:
                        yield return new(Bottom(), Left());
                        yield return new(Top(),    Right());
                        break;
                    case 10:
                        yield return new(Bottom(), Right());
                        yield return new(Top(),    Left());
                        break;
                }
            }
        }
    }

    /// <summary>Closes a polyline by appending the first point at the end if not already closed.</summary>
    private static PointF[] ClosePolygon(PointF[] points)
    {
        const float Eps = 1e-6f;
        if (points.Length < 2) return points;
        if (Near(points[0], points[^1], Eps)) return points;
        return [.. points, points[0]];
    }

    // --- Private helpers ---

    private static float Lerp(float a, float b, double va, double vb, double level)
    {
        double t = (vb - va) == 0 ? 0.5 : (level - va) / (vb - va);
        return (float)(a + t * (b - a));
    }

    /// <summary>Greedily chains segments that share an endpoint into polylines.</summary>
    private static IEnumerable<PointF[]> JoinSegments(List<Segment> segments)
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
