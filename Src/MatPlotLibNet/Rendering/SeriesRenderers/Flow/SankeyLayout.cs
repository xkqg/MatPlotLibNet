// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers.Flow;

/// <summary>
/// Immutable result of <see cref="SankeyLayoutEngine.Compute"/>. Captures
/// the per-node column assignment, rectangles (after packing + relaxation),
/// per-node flow values, and orientation flag. The rendering side
/// (<see cref="SankeySeriesRenderer.DrawLinks"/> /
/// <see cref="SankeySeriesRenderer.DrawNodesAndLabels"/>) consumes this
/// record; it knows nothing about how the layout was computed.
/// </summary>
/// <remarks>
/// Phase B.10 of the strict-90 floor plan (2026-04-20). Resolves the
/// class-level SRP violation where <c>SankeySeriesRenderer</c> did both
/// layout computation (BFS column assignment, value aggregation, packing,
/// iterative relaxation, collision resolution) and rendering (link paths,
/// node rectangles, labels). Now layout is its own responsibility owned
/// by <see cref="SankeyLayoutEngine"/>.
/// </remarks>
/// <param name="Columns">Per-node column index (0 = source side). -1 means "unreachable".</param>
/// <param name="MaxCol">Highest column index after alignment. Negative means no layout.</param>
/// <param name="NodeRects">Per-node bounding rectangle in pixel space.</param>
/// <param name="NodeValues">Per-node flow value (used by link drawing to weight stroke widths).</param>
/// <param name="Vertical">True for vertical Sankey (top-to-bottom), false for horizontal (left-to-right).</param>
public sealed record SankeyLayout(
    int[] Columns,
    int MaxCol,
    Rect[] NodeRects,
    double[] NodeValues,
    bool Vertical);
