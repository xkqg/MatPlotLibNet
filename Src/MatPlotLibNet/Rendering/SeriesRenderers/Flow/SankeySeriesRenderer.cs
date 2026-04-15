// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>
/// Renders a <see cref="SankeySeries"/> as node rectangles connected by bezier links.
/// Supports explicit per-node column overrides, D3-style column alignment modes, iterative
/// vertical relaxation to minimise link crossings, source / target / gradient link colour
/// modes, inside or outside labels, and optional sub-labels (e.g. metrics, Y/Y deltas).
/// </summary>
/// <remarks>
/// <para>Layout pipeline:</para>
/// <list type="number">
///   <item><description><see cref="ComputeColumns"/>: explicit <see cref="SankeyNode.Column"/>
///   overrides take priority; unspecified nodes get their column from BFS distance-from-source.</description></item>
///   <item><description><see cref="ApplyAlignment"/>: post-processes the column indices so the
///   overall grid honours the series' <see cref="SankeySeries.NodeAlignment"/> (Justify / Left /
///   Right / Center).</description></item>
///   <item><description>Greedy vertical packing: nodes sorted by total value within their column,
///   stacked top-to-bottom with <see cref="SankeySeries.NodePadding"/> between them.</description></item>
///   <item><description><see cref="Relax"/>: <see cref="SankeySeries.Iterations"/> passes that
///   shift each node toward the value-weighted average of its upstream / downstream neighbours,
///   then re-resolve collisions column-by-column.</description></item>
///   <item><description>Links drawn as Bezier polygons; fill resolved by
///   <see cref="SankeySeries.LinkColorMode"/> (source / target / gradient).</description></item>
///   <item><description>Nodes drawn as rectangles with labels collected for the
///   <see cref="LabelLayoutEngine"/> (outer placement) or drawn inside when
///   <see cref="SankeySeries.InsideLabels"/> is set and the rect is wide enough.</description></item>
/// </list>
/// </remarks>
internal sealed class SankeySeriesRenderer : SeriesRenderer<SankeySeries>
{
    /// <inheritdoc />
    public SankeySeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SankeySeries series)
    {
        var bounds = Context.Area.PlotBounds;
        int n = series.Nodes.Count;
        if (n == 0) return;

        // 1. Column assignment (explicit Column override wins; BFS otherwise).
        var columns = ComputeColumns(series);
        int maxCol = columns.Max();

        // 2. Alignment post-processing (Justify / Left / Right / Center).
        ApplyAlignment(series, columns, maxCol);
        maxCol = columns.Max();
        if (maxCol < 0) return;

        // 3. Per-node total value (sum of outgoing + incoming link values, capped at max of the
        //    two so two-way nodes don't double-count their own height).
        var nodeValues = ComputeNodeValues(series);

        // 4. Group node indices by column and sort within each column by total value so the
        //    largest nodes stack at the top (matplotlib / D3 default visual order).
        var colGroups = new Dictionary<int, List<int>>();
        for (int i = 0; i < n; i++)
        {
            if (columns[i] < 0) continue;
            if (!colGroups.TryGetValue(columns[i], out var list))
                colGroups[columns[i]] = list = new List<int>();
            list.Add(i);
        }
        foreach (var kv in colGroups)
            kv.Value.Sort((a, b) => nodeValues[b].CompareTo(nodeValues[a]));

        // 5. Initial greedy packing along the flow axis (X for horizontal Sankeys, Y for
        // vertical ones). In vertical mode, "columns" become rows running top-to-bottom
        // and node rectangles are WIDE (stacking axis = X) but SHORT (flow axis = NodeWidth,
        // used as row height). All downstream pipeline steps (relaxation, link drawing,
        // label placement) branch on `vert` so the same helpers serve both orientations.
        bool vert = series.Orient == SankeyOrientation.Vertical;

        var nodeRects = new Rect[n];
        double primaryLen = vert ? bounds.Height : bounds.Width;
        double crossLen   = vert ? bounds.Width  : bounds.Height;
        double colStep = maxCol > 0 ? (primaryLen - series.NodeWidth) / maxCol : 0;
        double totalValuePerCol = ComputeMaxColumnValueSum(colGroups, nodeValues);
        double availableCross = Math.Max(1,
            crossLen - MaxPaddingSum(colGroups, series.NodePadding));
        double valueScale = totalValuePerCol > 0 ? availableCross / totalValuePerCol : 0;

        double primaryOrigin = vert ? bounds.Y : bounds.X;
        double crossOrigin   = vert ? bounds.X : bounds.Y;

        foreach (var (col, nodeIndices) in colGroups)
        {
            double primaryPos = primaryOrigin + col * colStep;
            double crossPos = crossOrigin;
            foreach (int idx in nodeIndices)
            {
                double size = Math.Max(1, nodeValues[idx] * valueScale);
                nodeRects[idx] = vert
                    ? new Rect(crossPos, primaryPos, size, series.NodeWidth)
                    : new Rect(primaryPos, crossPos, series.NodeWidth, size);
                crossPos += size + series.NodePadding;
            }
        }

        // 6. Iterative relaxation — minimises link crossings by shifting each node
        //    toward the weighted average of its neighbour positions on the cross axis.
        Relax(series, columns, colGroups, nodeRects, nodeValues, bounds, vert);

        // 7. Draw links.
        DrawLinks(series, columns, nodeRects, nodeValues, bounds, vert);

        // 8. Draw nodes + labels.
        DrawNodesAndLabels(series, columns, nodeRects, maxCol, vert);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Column assignment
    // ──────────────────────────────────────────────────────────────────────────

    private static int[] ComputeColumns(SankeySeries series)
    {
        int n = series.Nodes.Count;
        var cols = new int[n];
        var fromOverride = new bool[n];

        // Explicit overrides first — pin the node to its declared column.
        for (int i = 0; i < n; i++)
        {
            if (series.Nodes[i].Column is int c)
            {
                cols[i] = c;
                fromOverride[i] = true;
            }
            else
            {
                cols[i] = -1;
            }
        }

        // BFS from source nodes for everything else.
        var isTarget = new bool[n];
        foreach (var link in series.Links)
            isTarget[link.TargetIndex] = true;

        var queue = new Queue<int>();
        for (int i = 0; i < n; i++)
        {
            if (fromOverride[i])
            {
                queue.Enqueue(i);
            }
            else if (!isTarget[i])
            {
                cols[i] = 0;
                queue.Enqueue(i);
            }
        }

        while (queue.Count > 0)
        {
            int src = queue.Dequeue();
            foreach (var link in series.Links)
            {
                if (link.SourceIndex != src) continue;
                int tgt = link.TargetIndex;
                if (fromOverride[tgt]) continue;
                int nextCol = cols[src] + 1;
                if (cols[tgt] < nextCol)
                {
                    cols[tgt] = nextCol;
                    queue.Enqueue(tgt);
                }
            }
        }

        // Nodes that are reachable from nowhere (isolated) get column 0.
        for (int i = 0; i < n; i++) if (cols[i] < 0) cols[i] = 0;
        return cols;
    }

    private static void ApplyAlignment(SankeySeries series, int[] columns, int maxCol)
    {
        // Justify is the default — columns already span 0..maxCol, no change needed.
        if (series.NodeAlignment == SankeyNodeAlignment.Justify) return;

        int n = columns.Length;
        // Compute the "latest possible column" for each node: max(col[neighbour] + 1) over
        // all downstream neighbours, or maxCol for sink nodes. This is the Right-alignment
        // position (D3 sankeyRight).
        var latest = new int[n];
        for (int i = 0; i < n; i++) latest[i] = maxCol;

        for (int pass = 0; pass < n; pass++)
        {
            bool changed = false;
            foreach (var link in series.Links)
            {
                int candidate = latest[link.TargetIndex] - 1;
                if (candidate < latest[link.SourceIndex])
                {
                    latest[link.SourceIndex] = candidate;
                    changed = true;
                }
            }
            if (!changed) break;
        }
        for (int i = 0; i < n; i++) if (latest[i] < columns[i]) latest[i] = columns[i];

        for (int i = 0; i < n; i++)
        {
            columns[i] = series.NodeAlignment switch
            {
                SankeyNodeAlignment.Right  => latest[i],
                SankeyNodeAlignment.Center => (columns[i] + latest[i]) / 2,
                _                          => columns[i],  // Left keeps the BFS value
            };
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Node value aggregation
    // ──────────────────────────────────────────────────────────────────────────

    private static double[] ComputeNodeValues(SankeySeries series)
    {
        int n = series.Nodes.Count;
        var outVal = new double[n];
        var inVal = new double[n];
        foreach (var link in series.Links)
        {
            outVal[link.SourceIndex] += link.Value;
            inVal[link.TargetIndex] += link.Value;
        }
        // Node height = max(outgoing, incoming) so a passthrough node doesn't double-count
        // the same flow value (matches D3 sankey behaviour).
        var values = new double[n];
        for (int i = 0; i < n; i++)
            values[i] = Math.Max(outVal[i], inVal[i]);
        return values;
    }

    private static double ComputeMaxColumnValueSum(Dictionary<int, List<int>> colGroups, double[] values)
    {
        double max = 0;
        foreach (var group in colGroups.Values)
        {
            double sum = 0;
            foreach (int idx in group) sum += values[idx];
            if (sum > max) max = sum;
        }
        return max;
    }

    private static double MaxPaddingSum(Dictionary<int, List<int>> colGroups, double nodePadding)
    {
        int maxCount = 0;
        foreach (var group in colGroups.Values)
            if (group.Count > maxCount) maxCount = group.Count;
        return Math.Max(0, (maxCount - 1) * nodePadding);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Relaxation
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Iterative relaxation pass that minimises link crossings. For each column, shifts every
    /// node toward the value-weighted average of its upstream neighbours (left pass) and
    /// downstream neighbours (right pass), then re-resolves intra-column collisions so nodes
    /// don't overlap and stay inside the plot bounds. Operates on the "cross axis" (Y for
    /// horizontal Sankeys, X for vertical) — the orientation flag is threaded through every
    /// helper so the same control-flow works for both orientations.
    /// </summary>
    private static void Relax(SankeySeries series, int[] columns,
        Dictionary<int, List<int>> colGroups, Rect[] nodeRects,
        double[] nodeValues, Rect bounds, bool vert)
    {
        if (series.Iterations <= 0) return;

        double alpha = 0.99;

        for (int iter = 0; iter < series.Iterations; iter++)
        {
            // Upstream pass: shift each non-source column toward its upstream centroid.
            RelaxColumn(series, colGroups, nodeRects, upstream: true, alpha, vert);
            ResolveCollisions(colGroups, nodeRects, series.NodePadding, bounds, vert);

            // Downstream pass: shift each non-sink column toward its downstream centroid.
            RelaxColumn(series, colGroups, nodeRects, upstream: false, alpha, vert);
            ResolveCollisions(colGroups, nodeRects, series.NodePadding, bounds, vert);

            alpha *= 0.99;  // cool the shift each iteration
        }
    }

    /// <summary>Returns the position of a rect's midpoint on the cross axis — Y-centre for
    /// horizontal layouts, X-centre for vertical ones. Single point of truth so relaxation
    /// and collision resolution operate on the same coordinate without orientation bugs.</summary>
    private static double CrossCentre(Rect r, bool vert) =>
        vert ? r.X + r.Width / 2 : r.Y + r.Height / 2;

    /// <summary>Returns the lower-bound position of a rect on the cross axis — Y for horizontal,
    /// X for vertical — i.e. the top edge (H) or left edge (V).</summary>
    private static double CrossStart(Rect r, bool vert) => vert ? r.X : r.Y;

    /// <summary>Returns the rect's extent along the cross axis — Height for horizontal, Width
    /// for vertical.</summary>
    private static double CrossSize(Rect r, bool vert) => vert ? r.Width : r.Height;

    /// <summary>Moves a rect to a new cross-axis start coordinate without touching the primary
    /// axis. Used by relaxation + collision resolution to slide nodes along their column.</summary>
    private static Rect WithCrossStart(Rect r, double newStart, bool vert) =>
        vert ? new Rect(newStart, r.Y, r.Width, r.Height)
             : new Rect(r.X, newStart, r.Width, r.Height);

    private static void RelaxColumn(SankeySeries series,
        Dictionary<int, List<int>> colGroups, Rect[] nodeRects,
        bool upstream, double alpha, bool vert)
    {
        foreach (var (_, nodeIndices) in colGroups)
        {
            foreach (int idx in nodeIndices)
            {
                double weightedSum = 0;
                double weightTotal = 0;
                foreach (var link in series.Links)
                {
                    if (upstream && link.TargetIndex == idx)
                    {
                        weightedSum += CrossCentre(nodeRects[link.SourceIndex], vert) * link.Value;
                        weightTotal += link.Value;
                    }
                    else if (!upstream && link.SourceIndex == idx)
                    {
                        weightedSum += CrossCentre(nodeRects[link.TargetIndex], vert) * link.Value;
                        weightTotal += link.Value;
                    }
                }
                if (weightTotal == 0) continue;
                double desiredCentre = weightedSum / weightTotal;
                double currentCentre = CrossCentre(nodeRects[idx], vert);
                double shift = (desiredCentre - currentCentre) * alpha;
                double newStart = CrossStart(nodeRects[idx], vert) + shift;
                nodeRects[idx] = WithCrossStart(nodeRects[idx], newStart, vert);
            }
        }
    }

    /// <summary>Resolves overlap between nodes in each column after a relaxation shift.
    /// Nodes are kept inside the plot bounds and at least <paramref name="nodePadding"/>
    /// apart, sorted by their current cross-axis centre (preserving the relaxation-induced
    /// order).</summary>
    private static void ResolveCollisions(Dictionary<int, List<int>> colGroups,
        Rect[] nodeRects, double nodePadding, Rect bounds, bool vert)
    {
        double crossMin = vert ? bounds.X : bounds.Y;
        double crossMax = vert ? bounds.X + bounds.Width : bounds.Y + bounds.Height;

        foreach (var (_, nodeIndices) in colGroups)
        {
            // Sort by current cross-axis centre — relaxation can reorder nodes within a column.
            nodeIndices.Sort((a, b) =>
                CrossCentre(nodeRects[a], vert).CompareTo(CrossCentre(nodeRects[b], vert)));

            // Forward pass: push each node past the previous node's trailing edge + padding.
            double cursor = crossMin;
            foreach (int idx in nodeIndices)
            {
                var r = nodeRects[idx];
                double newStart = Math.Max(CrossStart(r, vert), cursor);
                nodeRects[idx] = WithCrossStart(r, newStart, vert);
                cursor = newStart + CrossSize(r, vert) + nodePadding;
            }

            // Backward pass: if the last node spills beyond the far edge, push everyone back.
            double overshoot = cursor - nodePadding - crossMax;
            if (overshoot > 0)
            {
                for (int k = nodeIndices.Count - 1; k >= 0; k--)
                {
                    int idx = nodeIndices[k];
                    var r = nodeRects[idx];
                    double newStart = CrossStart(r, vert) - overshoot;
                    nodeRects[idx] = WithCrossStart(r, newStart, vert);
                    if (k > 0)
                    {
                        int prev = nodeIndices[k - 1];
                        var pr = nodeRects[prev];
                        double maxAllowedPrevEnd = newStart - nodePadding;
                        double prevEnd = CrossStart(pr, vert) + CrossSize(pr, vert);
                        overshoot = prevEnd > maxAllowedPrevEnd ? prevEnd - maxAllowedPrevEnd : 0;
                    }
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Link drawing
    // ──────────────────────────────────────────────────────────────────────────

    private void DrawLinks(SankeySeries series, int[] columns, Rect[] nodeRects,
        double[] nodeValues, Rect bounds, bool vert)
    {
        int n = series.Nodes.Count;
        // Per-node running offsets tracking how much of the source / target bar has already
        // been consumed by outgoing / incoming links. Sorted by the cross-axis position of
        // each endpoint so the visual stacking order mirrors the relaxed node order.
        var sourceOffsets = new double[n];
        var targetOffsets = new double[n];

        var sortedLinks = series.Links
            .OrderBy(l => CrossStart(nodeRects[l.SourceIndex], vert))
            .ThenBy(l => CrossStart(nodeRects[l.TargetIndex], vert))
            .ToList();

        var culture = System.Globalization.CultureInfo.InvariantCulture;
        foreach (var link in sortedLinks)
        {
            var srcRect = nodeRects[link.SourceIndex];
            var tgtRect = nodeRects[link.TargetIndex];
            if (nodeValues[link.SourceIndex] <= 0 || nodeValues[link.TargetIndex] <= 0) continue;

            double srcBand = CrossSize(srcRect, vert) * (link.Value / nodeValues[link.SourceIndex]);
            double tgtBand = CrossSize(tgtRect, vert) * (link.Value / nodeValues[link.TargetIndex]);

            double srcCrossPos = CrossStart(srcRect, vert) + sourceOffsets[link.SourceIndex];
            double tgtCrossPos = CrossStart(tgtRect, vert) + targetOffsets[link.TargetIndex];

            // Primary-axis entry / exit positions on each endpoint. For horizontal Sankeys
            // the link exits the source at its right edge (x + NodeWidth) and enters the
            // target at its left edge (x); the bezier control points share the same primary
            // midpoint. For vertical Sankeys, swap: source exit is the bottom edge, target
            // entry is the top edge, and the midpoint is the primary-axis average.
            double srcPrimary = vert
                ? srcRect.Y + srcRect.Height            // exit at the bottom of the source row
                : srcRect.X + srcRect.Width;            // exit at the right of the source column
            double tgtPrimary = vert
                ? tgtRect.Y                             // entry at the top of the target row
                : tgtRect.X;                            // entry at the left of the target column
            double midPrimary = (srcPrimary + tgtPrimary) / 2;

            var srcColor = series.Nodes[link.SourceIndex].Color ?? ResolveColor(null);
            var tgtColor = series.Nodes[link.TargetIndex].Color ?? ResolveColor(null);

            // Build the 4-point band polygon in (primary, cross) coordinates, then map back
            // to (X, Y) based on orientation. Using a local helper keeps the bezier control
            // logic orientation-agnostic.
            Point P(double primary, double cross) => vert ? new Point(cross, primary) : new Point(primary, cross);

            var segments = new List<PathSegment>
            {
                new MoveToSegment(P(srcPrimary, srcCrossPos)),
                new BezierSegment(P(midPrimary, srcCrossPos), P(midPrimary, tgtCrossPos), P(tgtPrimary, tgtCrossPos)),
                new LineToSegment(P(tgtPrimary, tgtCrossPos + tgtBand)),
                new BezierSegment(P(midPrimary, tgtCrossPos + tgtBand), P(midPrimary, srcCrossPos + srcBand),
                    P(srcPrimary, srcCrossPos + srcBand)),
                new CloseSegment()
            };

            Ctx.SetNextElementData("sankey-link-source", link.SourceIndex.ToString(culture));
            Ctx.SetNextElementData("sankey-link-target", link.TargetIndex.ToString(culture));

            bool drewGradient = false;
            if (series.LinkColorMode == SankeyLinkColorMode.Gradient && Ctx is SvgRenderContext svg)
            {
                var fromAlpha = ApplyAlpha(srcColor, series.LinkAlpha);
                var toAlpha = ApplyAlpha(tgtColor, series.LinkAlpha);
                // Gradient axis runs from source to target along the flow direction.
                var gradStart = P(srcPrimary, srcCrossPos);
                var gradEnd = P(tgtPrimary, tgtCrossPos);
                string gradId = svg.DefineLinearGradient(fromAlpha, toAlpha,
                    gradStart.X, gradStart.Y, gradEnd.X, gradEnd.Y);
                svg.DrawPathWithGradientFill(segments, gradId, null, 0);
                drewGradient = true;
            }
            if (!drewGradient)
            {
                var fillColor = series.LinkColorMode switch
                {
                    SankeyLinkColorMode.Target => ApplyAlpha(tgtColor, series.LinkAlpha),
                    _ => ApplyAlpha(srcColor, series.LinkAlpha),
                };
                Ctx.DrawPath(segments, fillColor, null, 0);
            }

            sourceOffsets[link.SourceIndex] += srcBand;
            targetOffsets[link.TargetIndex] += tgtBand;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Node + label drawing
    // ──────────────────────────────────────────────────────────────────────────

    private void DrawNodesAndLabels(SankeySeries series, int[] columns, Rect[] nodeRects, int maxCol, bool vert)
    {
        int n = series.Nodes.Count;
        var themeFont = Context?.Theme?.DefaultFont;
        var labelFont = themeFont is not null
            ? new Font { Family = themeFont.Family, Size = Math.Min(10, themeFont.Size), Color = themeFont.Color }
            : new Font { Size = 10 };
        var subLabelFontBase = new Font
        {
            Family = labelFont.Family,
            Size = labelFont.Size * 0.8,
            Color = labelFont.Color
        };
        var outerCandidates = new List<LabelCandidate>();
        var outerNodeIndex = new List<int>();

        var culture = System.Globalization.CultureInfo.InvariantCulture;
        for (int i = 0; i < n; i++)
        {
            var color = series.Nodes[i].Color ?? ResolveColor(null);
            Ctx.SetNextElementData("sankey-node-id", i.ToString(culture));
            Ctx.DrawRectangle(nodeRects[i], color, null, 0);

            string? label = series.Nodes[i].Label;
            if (string.IsNullOrEmpty(label)) continue;

            var measured = ChartServices.FontMetrics.Measure(label, labelFont);

            // Inside-labels: draw centred inside the rect if requested AND the rect is wide
            // enough (horizontal) / tall enough (vertical) to host the text.
            double crossExtent = vert ? nodeRects[i].Width : nodeRects[i].Height;
            if (series.InsideLabels && measured.Width + 8 <= crossExtent)
            {
                double cx = nodeRects[i].X + nodeRects[i].Width / 2;
                double cy = nodeRects[i].Y + nodeRects[i].Height / 2 + measured.Height * 0.3;
                var insideFont = new Font { Family = labelFont.Family, Size = labelFont.Size, Color = Colors.White };
                Ctx.DrawText(label, new Point(cx, cy), insideFont, TextAlignment.Center);
                DrawSubLabelIfAny(series.Nodes[i], new Point(cx, cy + measured.Height * 0.8),
                    subLabelFontBase, TextAlignment.Center);
                continue;
            }

            // Outer-label anchor placement. For horizontal: first column → right of rect,
            // last column → left of rect. For vertical: first column (top row) → above rect
            // (text sits above the top edge), last column (bottom row) → below rect.
            bool onFarEdge = columns[i] == maxCol;
            Point anchor;
            TextAlignment align;
            if (vert)
            {
                double cx = nodeRects[i].X + nodeRects[i].Width / 2;
                double yAnchor = onFarEdge
                    ? nodeRects[i].Y + nodeRects[i].Height + measured.Height + 2   // below
                    : nodeRects[i].Y - 4;                                          // above
                anchor = new Point(cx, yAnchor);
                align = TextAlignment.Center;
            }
            else
            {
                double labelX = onFarEdge
                    ? nodeRects[i].X - 4
                    : nodeRects[i].X + nodeRects[i].Width + 4;
                anchor = new Point(labelX, nodeRects[i].Y + nodeRects[i].Height / 2 + measured.Height * 0.3);
                align = onFarEdge ? TextAlignment.Right : TextAlignment.Left;
            }
            outerCandidates.Add(new LabelCandidate(anchor, label, labelFont, align));
            outerNodeIndex.Add(i);
        }

        // Batch-place outer labels through the collision-avoidance engine.
        if (outerCandidates.Count > 0)
        {
            var placements = LabelLayoutEngine.Place(
                outerCandidates,
                Context.Area.PlotBounds,
                ChartServices.FontMetrics);
            var leaderColor = Context?.Theme?.ForegroundText ?? Colors.Black;
            for (int k = 0; k < placements.Count; k++)
            {
                var p = placements[k];
                if (p.LeaderLineStart is { } anchor)
                    CalloutBoxRenderer.DrawLeaderLine(Ctx, anchor, p.FinalPoint, leaderColor);
                Ctx.DrawText(p.Text, p.FinalPoint, p.Font, p.Alignment);

                // Draw the sub-label one line below the primary label's final position.
                int nodeIdx = outerNodeIndex[k];
                var subLabelSize = ChartServices.FontMetrics.Measure(p.Text, p.Font);
                DrawSubLabelIfAny(series.Nodes[nodeIdx],
                    new Point(p.FinalPoint.X, p.FinalPoint.Y + subLabelSize.Height * 0.9),
                    subLabelFontBase, p.Alignment);
            }
        }
    }

    private void DrawSubLabelIfAny(SankeyNode node, Point position, Font subLabelFontBase, TextAlignment alignment)
    {
        if (string.IsNullOrEmpty(node.SubLabel)) return;
        var color = node.SubLabelColor ?? subLabelFontBase.Color;
        var subFont = new Font
        {
            Family = subLabelFontBase.Family,
            Size = subLabelFontBase.Size,
            Color = color,
        };
        Ctx.DrawText(node.SubLabel!, position, subFont, alignment);
    }
}
