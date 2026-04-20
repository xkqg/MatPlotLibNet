// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers.Flow;
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
    private readonly SankeyLayoutEngine _layoutEngine = new();

    /// <inheritdoc />
    public SankeySeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SankeySeries series)
    {
        var bounds = Context.Area.PlotBounds;
        // Phase B.10 — layout computation is now a separate class/responsibility.
        var layout = _layoutEngine.Compute(series, bounds);
        if (layout is null) return;

        // Draw links, then nodes+labels, using the pre-computed layout.
        DrawLinks(series, layout.Columns, layout.NodeRects, layout.NodeValues, bounds, layout.Vertical);
        DrawNodesAndLabels(series, layout.Columns, layout.NodeRects, layout.MaxCol, layout.Vertical);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Orientation-agnostic rect accessors (mirror SankeyLayoutEngine)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Cross-axis midpoint: Y-centre (horizontal) / X-centre (vertical).</summary>
    private static double CrossCentre(Rect r, bool vert) =>
        vert ? r.X + r.Width / 2 : r.Y + r.Height / 2;

    /// <summary>Cross-axis lower bound: Y (horizontal) / X (vertical).</summary>
    private static double CrossStart(Rect r, bool vert) => vert ? r.X : r.Y;

    /// <summary>Cross-axis extent: Height (horizontal) / Width (vertical).</summary>
    private static double CrossSize(Rect r, bool vert) => vert ? r.Width : r.Height;

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
                Context!.Area.PlotBounds,
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
