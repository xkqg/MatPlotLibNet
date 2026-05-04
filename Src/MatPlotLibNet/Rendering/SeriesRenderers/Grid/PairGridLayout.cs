// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Pure geometry for the N×N panel layout used by
/// <c>PairGridSeriesRenderer</c>. Splits a single rectangular plot area into
/// uniformly sized cell rectangles separated by an optional fractional gutter.</summary>
/// <remarks>Direct unit testable — no rendering dependencies. Sub-pixel cells
/// (width or height below <see cref="MinPanelPx"/>) must be skipped by callers,
/// not by this function: layout produces the geometry; the renderer chooses
/// what to draw.</remarks>
internal static class PairGridLayout
{
    /// <summary>Minimum pixel width/height for a pair-grid cell below which the
    /// cell is suppressed by the renderer to avoid sub-pixel noise. Aliases the
    /// shared <see cref="HierarchicalLayout.MinPanelPx"/> so a single source-of-truth
    /// applies across every composite renderer.</summary>
    internal const double MinPanelPx = HierarchicalLayout.MinPanelPx;

    /// <summary>Splits <paramref name="plotBounds"/> into an
    /// <paramref name="n"/>×<paramref name="n"/> grid of cells separated by a
    /// gutter of <paramref name="cellSpacing"/> fractional width / height.
    /// Index ordering: <c>cells[row, col]</c>; row 0 is the top, column 0 the left.</summary>
    /// <param name="plotBounds">The full plot area to subdivide.</param>
    /// <param name="n">The number of cells per side (one per variable). Must be ≥ 1.</param>
    /// <param name="cellSpacing">Gutter as a fraction of total bounds. Caller
    /// is responsible for clamping to a sensible range; the function itself does
    /// not clamp.</param>
    /// <returns>An <c>n</c>×<c>n</c> array of <see cref="Rect"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="n"/> &lt; 1.</exception>
    internal static Rect[,] ComputeCellRects(Rect plotBounds, int n, double cellSpacing)
    {
        if (n < 1)
            throw new ArgumentOutOfRangeException(nameof(n), n, "n must be at least 1.");

        if (n == 1)
            return new Rect[1, 1] { { plotBounds } };

        double totalGutterX = cellSpacing * plotBounds.Width;
        double totalGutterY = cellSpacing * plotBounds.Height;
        double cellW   = (plotBounds.Width  - totalGutterX) / n;
        double cellH   = (plotBounds.Height - totalGutterY) / n;
        double gutterW = totalGutterX / (n - 1);
        double gutterH = totalGutterY / (n - 1);

        var cells = new Rect[n, n];
        for (int r = 0; r < n; r++)
        for (int c = 0; c < n; c++)
        {
            cells[r, c] = new Rect(
                plotBounds.X + c * (cellW + gutterW),
                plotBounds.Y + r * (cellH + gutterH),
                cellW,
                cellH);
        }
        return cells;
    }
}
