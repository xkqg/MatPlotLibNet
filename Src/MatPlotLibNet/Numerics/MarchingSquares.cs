// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Drawing;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Numerics;

/// <summary>Extracts iso-lines and filled bands from a 2D scalar field using the marching-squares algorithm.</summary>
internal static class MarchingSquares
{
    /// <summary>One continuous contour polyline at a specific level.</summary>
    internal readonly record struct ContourLine(double Level, PointF[] Points);

    /// <summary>One filled band between two consecutive iso-levels: the region where the grid is above
    /// <paramref name="LevelLow"/>, as the pieces the cells hold of it (<see cref="RegionAbove"/>), which a painter's fill
    /// paints over the bands below it, so what shows of it lies between its two levels.</summary>
    /// <param name="LevelLow">The band's lower level.</param>
    /// <param name="LevelHigh">The band's upper level.</param>
    /// <param name="Polygons">The pieces of the region above <paramref name="LevelLow"/>, each anticlockwise in data
    /// space, none overlapping another; empty for the lowest band, whose colour the fill paints under the whole grid.</param>
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
        if (yGrid.Length < 2 || xGrid.Length < 2) return [];

        var result = new List<ContourLine>();
        var segments = new List<Segment>();
        foreach (double level in levels)
        {
            segments.Clear();
            CollectSegments(xGrid, yGrid, zGrid, level, segments);
            foreach (var polyline in JoinSegments(segments))
                result.Add(new ContourLine(level, polyline));
        }

        return [.. result];
    }

    /// <summary>
    /// Extracts filled contour bands between iso-levels that are given, one between each two consecutive
    /// <paramref name="thresholds"/>, whatever range the grid spans: each with the region above its lower threshold, which a
    /// band whose threshold the grid never exceeds leaves empty. A value that equals a threshold is not above it, so it belongs
    /// to the band below (<c>lower &lt; z &lt;= upper</c>, as contourpy fills).
    /// </summary>
    /// <param name="xGrid">X coordinates (length = columns of <paramref name="zGrid"/>).</param>
    /// <param name="yGrid">Y coordinates (length = rows of <paramref name="zGrid"/>).</param>
    /// <param name="zGrid">Z values indexed as [row, col].</param>
    /// <param name="thresholds">The iso-levels, ascending and each once (see <see cref="ContourLevels.ExplicitLevels"/>);
    /// <c>thresholds.Length - 1</c> bands.</param>
    /// <returns>Bands in ascending level order, each with the pieces of its region; none when fewer than two thresholds are
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

    /// <summary>The band between each pair of consecutive thresholds, each with the region above its lower threshold
    /// (none for the lowest, which the fill paints under the whole grid).</summary>
    private static ContourBand[] BandsBetween(double[] xGrid, double[] yGrid, double[,] zGrid, double[] thresholds)
    {
        var bands = new ContourBand[thresholds.Length - 1];
        var pieces = new List<PointF[]>();
        for (int b = 0; b < bands.Length; b++)
        {
            pieces.Clear();
            if (b > 0) RegionAbove(xGrid, yGrid, zGrid, thresholds[b], pieces);
            bands[b] = new ContourBand(thresholds[b], thresholds[b + 1], [.. pieces]);
        }

        return bands;
    }

    /// <summary>Adds the pieces of the region where the grid is above <paramref name="level"/> to
    /// <paramref name="pieces"/>, cell by cell, each anticlockwise in data space. A cell wholly above the level is a
    /// rectangle, and a run of them along a row is one; a cell the iso-line crosses gives its corners above the level and
    /// the points where the iso-line crosses its edges, in order round the cell, at the points <see cref="CollectSegments"/>
    /// puts the iso-line through, and a saddle (two opposite corners above the level) gives the two corner triangles,
    /// as the line's saddle rule separates them. A masked cell gives nothing. Pieces that share an edge are filled as one
    /// region when they are painted as one path, so the region has no seam, reaches every edge and corner of the grid it
    /// holds, and leaves out a valley inside it.</summary>
    private static void RegionAbove(double[] xGrid, double[] yGrid, double[,] zGrid, double level, List<PointF[]> pieces)
    {
        int rows = yGrid.Length;
        int cols = xGrid.Length;
        Span<PointF> ring = stackalloc PointF[6];
        for (int row = 0; row < rows - 1; row++)
        {
            float y0 = (float)yGrid[row];
            float y1 = (float)yGrid[row + 1];
            int runStart = -1;
            for (int col = 0; col < cols - 1; col++)
            {
                int idx = zGrid.IsDrawableCell(xGrid, yGrid, row, col) ? CaseOf(zGrid, row, col, level) : 0;
                if (idx == 15)
                {
                    if (runStart < 0) runStart = col;
                    continue;
                }

                if (runStart >= 0)
                {
                    pieces.Add(Rectangle((float)xGrid[runStart], (float)xGrid[col], y0, y1));
                    runStart = -1;
                }

                if (idx == 0) continue;

                double v00 = zGrid[row, col];         // bottom-left
                double v10 = zGrid[row, col + 1];     // bottom-right
                double v11 = zGrid[row + 1, col + 1]; // top-right
                double v01 = zGrid[row + 1, col];     // top-left
                float x0 = (float)xGrid[col];
                float x1 = (float)xGrid[col + 1];
                var bl = new PointF(x0, y0);
                var br = new PointF(x1, y0);
                var tr = new PointF(x1, y1);
                var tl = new PointF(x0, y1);
                var bottom = new PointF(Lerp(x0, x1, v00, v10, level), y0);
                var right = new PointF(x1, Lerp(y0, y1, v10, v11, level));
                var top = new PointF(Lerp(x0, x1, v01, v11, level), y1);
                var left = new PointF(x0, Lerp(y0, y1, v00, v01, level));

                switch (idx)
                {
                    case 5:
                        AddPiece(pieces, [bl, bottom, left]);
                        AddPiece(pieces, [tr, top, right]);
                        continue;
                    case 10:
                        AddPiece(pieces, [bottom, br, right]);
                        AddPiece(pieces, [top, tl, left]);
                        continue;
                }

                // Round the cell anticlockwise: each corner above the level, and the crossing on each edge whose two
                // corners fall on either side of it.
                int n = 0;
                bool inBl = (idx & 1) != 0, inBr = (idx & 2) != 0, inTr = (idx & 4) != 0, inTl = (idx & 8) != 0;
                if (inBl) ring[n++] = bl;
                if (inBl != inBr) ring[n++] = bottom;
                if (inBr) ring[n++] = br;
                if (inBr != inTr) ring[n++] = right;
                if (inTr) ring[n++] = tr;
                if (inTr != inTl) ring[n++] = top;
                if (inTl) ring[n++] = tl;
                if (inTl != inBl) ring[n++] = left;
                AddPiece(pieces, ring[..n]);
            }

            if (runStart >= 0) pieces.Add(Rectangle((float)xGrid[runStart], (float)xGrid[cols - 1], y0, y1));
        }
    }

    /// <summary>Adds <paramref name="piece"/> unless it encloses nothing: a level that runs through a corner of the grid puts a
    /// crossing on the corner itself, and the cell's piece there is a point or a line.</summary>
    private static void AddPiece(List<PointF[]> pieces, ReadOnlySpan<PointF> piece)
    {
        // Twice the area, measured from the first point so that a cell far from the origin loses nothing to cancellation.
        double twice = 0, x0 = piece[0].X, y0 = piece[0].Y;
        for (int i = 1; i < piece.Length - 1; i++)
            twice += (piece[i].X - x0) * (piece[i + 1].Y - y0) - (piece[i + 1].X - x0) * (piece[i].Y - y0);

        if (twice > 0) pieces.Add(piece.ToArray());
    }

    /// <summary>The marching-squares case of the cell whose bottom-left corner is at (<paramref name="row"/>,
    /// <paramref name="col"/>): bit 3 the top-left corner above <paramref name="level"/>, bit 2 the top-right, bit 1 the
    /// bottom-right, bit 0 the bottom-left. The one case index the lines and the regions read. A corner is above a level when
    /// <c>z &gt; level</c>, a corner that equals it is below, as contourpy (matplotlib's contour generator) has it: a line at the
    /// grid's lowest value runs along the edge where the value equals it, and a line at its highest value is not drawn.</summary>
    private static int CaseOf(double[,] zGrid, int row, int col, double level) =>
        (zGrid[row + 1, col] > level ? 8 : 0)
        | (zGrid[row + 1, col + 1] > level ? 4 : 0)
        | (zGrid[row, col + 1] > level ? 2 : 0)
        | (zGrid[row, col] > level ? 1 : 0);

    /// <summary>The rectangle from <paramref name="x0"/> to <paramref name="x1"/> and <paramref name="y0"/> to
    /// <paramref name="y1"/>, anticlockwise.</summary>
    private static PointF[] Rectangle(float x0, float x1, float y0, float y1) => [new(x0, y0), new(x1, y0), new(x1, y1), new(x0, y1)];

    /// <summary>Adds every marching-squares edge segment of the iso-line at <paramref name="level"/> to
    /// <paramref name="segments"/>, cell by cell: the one case table of this algorithm, for the lines and the bands alike.</summary>
    private static void CollectSegments(double[] xGrid, double[] yGrid, double[,] zGrid, double level, List<Segment> segments)
    {
        int rows = yGrid.Length;
        int cols = xGrid.Length;
        for (int row = 0; row < rows - 1; row++)
        {
            for (int col = 0; col < cols - 1; col++)
            {
                if (!zGrid.IsDrawableCell(xGrid, yGrid, row, col)) continue; // masked, as matplotlib masks it

                double v00 = zGrid[row,     col];     // bottom-left
                double v10 = zGrid[row,     col + 1]; // bottom-right
                double v01 = zGrid[row + 1, col];     // top-left
                double v11 = zGrid[row + 1, col + 1]; // top-right

                float x0 = (float)xGrid[col];
                float x1 = (float)xGrid[col + 1];
                float y0 = (float)yGrid[row];
                float y1 = (float)yGrid[row + 1];

                // 4-bit case index: bit3=TL, bit2=TR, bit1=BR, bit0=BL
                int idx = CaseOf(zGrid, row, col, level);

                if (idx == 0 || idx == 15) continue; // all below or all above

                // Edge crossings by linear interpolation
                PointF Bottom() => new(Lerp(x0, x1, v00, v10, level), y0);
                PointF Top()    => new(Lerp(x0, x1, v01, v11, level), y1);
                PointF Left()   => new(x0, Lerp(y0, y1, v00, v01, level));
                PointF Right()  => new(x1, Lerp(y0, y1, v10, v11, level));

                // One segment per cell, two for the saddles, resolved with a consistent orientation
                switch (idx)
                {
                    case 1:  case 14: segments.Add(new(Bottom(), Left()));  break;
                    case 2:  case 13: segments.Add(new(Bottom(), Right())); break;
                    case 3:  case 12: segments.Add(new(Left(),   Right())); break;
                    case 4:  case 11: segments.Add(new(Top(),    Right())); break;
                    case 6:  case 9:  segments.Add(new(Bottom(), Top()));   break;
                    case 7:  case 8:  segments.Add(new(Top(),    Left()));  break;
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
    }

    private static float Lerp(float a, float b, double va, double vb, double level)
    {
        double t = CrossingAt(va, vb, level);
        return (float)(a + t * (b - a));
    }

    /// <summary>Where <paramref name="level"/> crosses the edge between a corner of value <paramref name="va"/> and one of
    /// <paramref name="vb"/>: its share of the way from the first corner, linearly; the middle of an edge whose corners are equal. The
    /// one place a contour (of a grid, of a triangulation, in 3-D) works out a crossing.</summary>
    /// <param name="va">The value at the first corner.</param>
    /// <param name="vb">The value at the second corner.</param>
    /// <param name="level">The level.</param>
    /// <returns>The share of the edge, from the first corner.</returns>
    internal static double CrossingAt(double va, double vb, double level) => (vb - va) == 0 ? 0.5 : (level - va) / (vb - va);

    /// <summary>Greedily chains segments that share an endpoint into polylines.</summary>
    private static IEnumerable<PointF[]> JoinSegments(List<Segment> segments)
    {
        if (segments.Count == 0) yield break;

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
                    if (Near(segments[i].A, tail))
                    {
                        chain.Add(segments[i].B);
                        used[i] = true; extended = true; break;
                    }
                    if (Near(segments[i].B, tail))
                    {
                        chain.Add(segments[i].A);
                        used[i] = true; extended = true; break;
                    }
                }
            }

            yield return [.. chain];
        }
    }

    /// <summary>Whether two points are the same end of a segment: within <see cref="Eps"/> on both axes.</summary>
    private static bool Near(PointF a, PointF b)
        => Math.Abs(a.X - b.X) < Eps && Math.Abs(a.Y - b.Y) < Eps;

    /// <summary>How close two segment ends must be to be one point.</summary>
    private const float Eps = 1e-6f;
}
