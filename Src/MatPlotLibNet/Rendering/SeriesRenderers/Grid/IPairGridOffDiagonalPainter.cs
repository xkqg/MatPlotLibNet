// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Strategy interface for off-diagonal cell painting in
/// <see cref="PairGridSeriesRenderer"/>. Each implementation handles a single
/// <see cref="PairGridOffDiagonalKind"/> value, isolating the rendering logic per
/// kind, so a kind such as <c>Hexbin</c> is added without touching the others — adding a
/// kind requires a new <see cref="IPairGridOffDiagonalPainter"/> implementation
/// plus one entry in <see cref="Rendering.SeriesRenderers.PairGridSeriesRenderer"/>, with no
/// modifications to the renderer's dispatch loop.</summary>
internal interface IPairGridOffDiagonalPainter
{
    /// <summary>Paints one off-diagonal cell. The cell-local axis spans are
    /// pre-computed by the caller (NaN-safe, in <see cref="PairGridCell.Frame"/>) so all painters share the same
    /// coordinate frame.</summary>
    /// <param name="ctx">The context to draw on.</param>
    /// <param name="cell">What is drawn in the cell, and the frame it is drawn in.</param>
    void Paint(IRenderContext ctx, PairGridCell cell);
}

/// <summary>Per-point scatter rendering. Honours <see cref="PairGridSeries.HueGroups"/>
/// when active, falling back to a single base colour otherwise. Non-finite samples
/// are skipped before the cell-pixel projection.</summary>
internal sealed class ScatterOffDiagonalPainter : IPairGridOffDiagonalPainter
{
    /// <inheritdoc />
    public void Paint(IRenderContext ctx, PairGridCell cell)
    {
        var (series, xData, yData, n, frame, baseColor, hueColors, _, radius) = cell;

        if (!cell.HueActive)
        {
            for (int k = 0; k < n; k++)
            {
                if (!double.IsFinite(xData[k]) || !double.IsFinite(yData[k])) continue;
                ctx.DrawCircle(frame.Map(xData[k], yData[k]), radius, ShapeStyle.Filled(baseColor));
            }
            return;
        }

        var hue = series.HueGroups!;
        // HueColors seeded every group ID present in HueGroups before dispatch, so
        // the lookup is total — no fallback required.
        for (int k = 0; k < n; k++)
        {
            if (!double.IsFinite(xData[k]) || !double.IsFinite(yData[k])) continue;
            ctx.DrawCircle(frame.Map(xData[k], yData[k]), radius, ShapeStyle.Filled(hueColors![hue[k]]));
        }
    }
}

/// <summary>Hexagonal density painter. Reuses <see cref="HexLattice"/> for the
/// binning and the hexagons; maps data-space hex centres to cell-pixel space via
/// the same linear scaling as <see cref="ScatterOffDiagonalPainter"/>. Hue is
/// intentionally ignored — density encoding cannot cleanly carry a per-group
/// dimension.</summary>
internal sealed class HexbinOffDiagonalPainter : IPairGridOffDiagonalPainter
{
    /// <summary>The fewest points a hexagon of the panel holds to be drawn (matplotlib's <c>mincnt=1</c>): an empty one is left blank.</summary>
    private const int LeastCountDrawn = 1;

    /// <inheritdoc />
    public void Paint(IRenderContext ctx, PairGridCell cell)
    {
        var (series, xData, yData, n, frame, _, _, _, _) = cell;
        var (xMin, xSpan) = frame.X;
        var (yMin, ySpan) = frame.Y;
        // The first n samples that can be placed (HexLattice also leaves out a point that cannot be placed).
        var xFiltered = new List<double>(n);
        var yFiltered = new List<double>(n);
        for (int k = 0; k < n; k++)
        {
            if (!double.IsFinite(xData[k]) || !double.IsFinite(yData[k])) continue;
            xFiltered.Add(xData[k]);
            yFiltered.Add(yData[k]);
        }
        if (xFiltered.Count == 0) return;

        double xMax = xMin + xSpan;
        double yMax = yMin + ySpan;
        int gridSize = Math.Max(1, series.HexbinGridSize);

        // The grid covers the panel's range of both variables, as matplotlib's grid covers the extent it is given.
        var lattice = HexLattice.Over(new HexExtent(xMin, xMax, yMin, yMax), gridSize);
        int[] counts = lattice.Count([.. xFiltered], [.. yFiltered]);

        int maxCount = counts.Max();
        if (maxCount == 0) return;
        // The panel is a hexbin that draws the hexagons holding a point, coloured by the hexbin's one rule over its counts.
        var scale = cell.Density.Over(HexbinSeries.ColourRangeOver(counts, LeastCountDrawn, cell.Density));

        Span<Point> outline = stackalloc Point[6];
        for (int cellIndex = 0; cellIndex < counts.Length; cellIndex++)
        {
            if (counts[cellIndex] < LeastCountDrawn) continue;

            lattice.OutlineAt(lattice.CentreOf(cellIndex), outline);
            var pixelVerts = new Point[6];
            for (int v = 0; v < 6; v++)
                pixelVerts[v] = frame.Map(outline[v].X, outline[v].Y);

            // Outlined in its own colour, as matplotlib draws a hexagon, so neighbours leave no seam.
            var fill = scale.ColorOf(counts[cellIndex]);
            ctx.DrawPolygon(pixelVerts, ShapeStyle.Edged(fill, fill, PointLengthExtensions.PixelsPerPoint));
        }
    }
}
