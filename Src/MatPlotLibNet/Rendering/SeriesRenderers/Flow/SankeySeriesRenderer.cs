// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.SeriesRenderers.Flow;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering.TextMeasurement;
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
///   <item><description><c>SankeyLayoutEngine.ComputeColumns</c>: explicit <see cref="SankeyNode.Column"/>
///   overrides take priority; unspecified nodes get their column from BFS distance-from-source.</description></item>
///   <item><description><c>SankeyLayoutEngine.ApplyAlignment</c>: post-processes the column indices so the
///   overall grid honours the series' <see cref="SankeySeries.NodeAlignment"/> (Justify / Left /
///   Right / Center).</description></item>
///   <item><description>Greedy vertical packing: nodes sorted by total value within their column,
///   stacked top-to-bottom with <see cref="SankeySeries.NodePadding"/> between them.</description></item>
///   <item><description><c>SankeyLayoutEngine.Relax</c>: <see cref="SankeySeries.Iterations"/> passes that
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
        DrawLinks(series, layout.NodeRects, layout.NodeValues, layout.Vertical);
        DrawNodesAndLabels(series, layout.Columns, layout.NodeRects, layout.MaxCol, layout.Vertical);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Link drawing
    // ──────────────────────────────────────────────────────────────────────────

    private void DrawLinks(SankeySeries series, Rect[] nodeRects, double[] nodeValues, bool crossAlongX)
    {
        int n = series.Nodes.Count;
        // Per-node running offsets tracking how much of the source / target bar has already
        // been consumed by outgoing / incoming links. Sorted by the cross-axis position of
        // each endpoint so the visual stacking order mirrors the relaxed node order.
        var sourceOffsets = new double[n];
        var targetOffsets = new double[n];

        var sortedLinks = series.Links
            .OrderBy(l => nodeRects[l.SourceIndex].StartAlong(crossAlongX))
            .ThenBy(l => nodeRects[l.TargetIndex].StartAlong(crossAlongX))
            .ToList();

        foreach (var link in sortedLinks)
        {
            var srcRect = nodeRects[link.SourceIndex];
            var tgtRect = nodeRects[link.TargetIndex];
            if (nodeValues[link.SourceIndex] <= 0 || nodeValues[link.TargetIndex] <= 0) continue;

            double srcBand = srcRect.LengthAlong(crossAlongX) * (link.Value / nodeValues[link.SourceIndex]);
            double tgtBand = tgtRect.LengthAlong(crossAlongX) * (link.Value / nodeValues[link.TargetIndex]);

            double srcCrossPos = srcRect.StartAlong(crossAlongX) + sourceOffsets[link.SourceIndex];
            double tgtCrossPos = tgtRect.StartAlong(crossAlongX) + targetOffsets[link.TargetIndex];

            // Primary-axis entry / exit positions on each endpoint. For horizontal Sankeys
            // the link exits the source at its right edge (x + NodeWidth) and enters the
            // target at its left edge (x); the bezier control points share the same primary
            // midpoint. For vertical Sankeys, swap: source exit is the bottom edge, target
            // entry is the top edge, and the midpoint is the primary-axis average.
            double srcPrimary = srcRect.EndAlong(!crossAlongX);      // exit at the right of the source column (bottom of its row)
            double tgtPrimary = tgtRect.StartAlong(!crossAlongX);    // entry at the left of the target column (top of its row)
            double midPrimary = (srcPrimary + tgtPrimary) / 2;

            var srcColor = series.Nodes[link.SourceIndex].Color ?? SeriesColor;
            var tgtColor = series.Nodes[link.TargetIndex].Color ?? SeriesColor;

            // Build the 4-point band polygon in (primary, cross) coordinates, then map back
            // to (X, Y) based on orientation. Using a local helper keeps the bezier control
            // logic orientation-agnostic.
            Point P(double primary, double cross) => Point.Along(!crossAlongX, primary, cross);

            var segments = new List<PathSegment>
            {
                new MoveToSegment(P(srcPrimary, srcCrossPos)),
                new BezierSegment(P(midPrimary, srcCrossPos), P(midPrimary, tgtCrossPos), P(tgtPrimary, tgtCrossPos)),
                new LineToSegment(P(tgtPrimary, tgtCrossPos + tgtBand)),
                new BezierSegment(P(midPrimary, tgtCrossPos + tgtBand), P(midPrimary, srcCrossPos + srcBand),
                    P(srcPrimary, srcCrossPos + srcBand)),
                new CloseSegment()
            };

            Ctx.SetNextElementData("sankey-link-source", link.SourceIndex.InvariantText());
            Ctx.SetNextElementData("sankey-link-target", link.TargetIndex.InvariantText());

            var fromAlpha = srcColor.WithOpacityGiven(series.LinkAlpha);
            if (series.LinkColorMode == SankeyLinkColorMode.Gradient)
            {
                // The blend runs from source to target along the flow direction, a property of the fill like a hatch
                // is, so every backend that can paint a gradient (SVG, PNG, PDF) paints it.
                var toAlpha = tgtColor.WithOpacityGiven(series.LinkAlpha);
                var blend = new LinearGradient(P(srcPrimary, srcCrossPos), P(tgtPrimary, tgtCrossPos), fromAlpha, toAlpha);
                Ctx.DrawPath(segments, ShapeStyle.Filled(fromAlpha) with { Gradient = blend });
            }
            else
            {
                var fillColor = series.LinkColorMode == SankeyLinkColorMode.Target ? tgtColor.WithOpacityGiven(series.LinkAlpha) : fromAlpha;
                Ctx.DrawPath(segments, ShapeStyle.Filled(fillColor));
            }

            sourceOffsets[link.SourceIndex] += srcBand;
            targetOffsets[link.TargetIndex] += tgtBand;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Node + label drawing
    // ──────────────────────────────────────────────────────────────────────────

    private void DrawNodesAndLabels(SankeySeries series, int[] columns, Rect[] nodeRects, int maxCol, bool crossAlongX)
    {
        int n = series.Nodes.Count;
        var labelFont = Context.Theme.DataTextFont(series.LabelSizeUnder(Context.Theme));
        var subLabelFontBase = labelFont with { Size = labelFont.Size * 0.8 };
        var outerCandidates = new List<LabelCandidate>();
        var outerNodeIndex = new List<int>();

        for (int i = 0; i < n; i++)
        {
            var color = series.Nodes[i].Color ?? SeriesColor;
            Ctx.SetNextElementData("sankey-node-id", i.InvariantText());
            Ctx.DrawRectangle(nodeRects[i], ShapeStyle.Filled(color));

            string? label = series.Nodes[i].Label;
            if (string.IsNullOrEmpty(label)) continue;

            var measured = Ctx.ExtentOf(TextLabel.Of(label), labelFont);

            // Inside-labels: draw centred inside the rect if requested AND the rect is wide
            // enough (horizontal) / tall enough (vertical) to host the text.
            double crossExtent = nodeRects[i].LengthAlong(crossAlongX);
            if (series.InsideLabels && measured.Width + 8 <= crossExtent)
            {
                // Both texts stand on the node, centred on its middle and the sub-label one line under it: the ink that reads against
                // its colour, unless the node names one for its sub-label.
                var middle = new TextPlacement(nodeRects[i].Center, TextAlignment.Center) { Vertical = TextVerticalAlignment.Center };
                var insideFont = Context.Theme.DataTextFontOn(color, labelFont.Size, GroundInk.Readable);
                Ctx.Draw(TextLabel.Of(label), insideFont, middle);
                DrawSubLabelIfAny(series.Nodes[i], middle.LineUnder(measured), subLabelFontBase with { Color = insideFont.Color });
                continue;
            }

            bool onFarEdge = columns[i] == maxCol;
            var at = ComputeNodeLabelAnchor(nodeRects[i], onFarEdge, series.Orient);
            outerCandidates.Add(new LabelCandidate(at.Anchor, label, labelFont, at.Alignment) { VerticalAlignment = at.Vertical });
            outerNodeIndex.Add(i);
        }

        // Batch-place outer labels through the collision-avoidance engine.
        if (outerCandidates.Count > 0)
        {
            var placements = PlaceLabels(outerCandidates, Context.Area.PlotBounds);
            for (int k = 0; k < placements.Count; k++)
            {
                var p = placements[k];
                DrawPlacedLabel(p);

                // The sub-label one line under the primary label's final position, standing on it as the label does.
                int nodeIdx = outerNodeIndex[k];
                var placed = new TextPlacement(p.FinalPoint, p.Alignment) { Vertical = p.VerticalAlignment };
                DrawSubLabelIfAny(series.Nodes[nodeIdx], placed.LineUnder(Ctx.ExtentOf(TextLabel.Of(p.Text), p.Font)), subLabelFontBase);
            }
        }
    }

    /// <summary>Where the outer label of a node stands: beside a node of a flow along X, centred on its middle, 4 pixels off the side
    /// away from the flow's far edge; under the last node of a flow down the page, hung 2 pixels below it; above any other, on its
    /// baseline 4 pixels over it. Pure function, no render context required.</summary>
    internal static TextPlacement ComputeNodeLabelAnchor(Rect nodeRect, bool onFarEdge, SankeyOrientation orientation)
    {
        if (!orientation.FlowsAlongX)
        {
            double cx = nodeRect.Center.X;
            return onFarEdge
                ? new(new Point(cx, nodeRect.Y + nodeRect.Height + 2), TextAlignment.Center) { Vertical = TextVerticalAlignment.Top }
                : new(new Point(cx, nodeRect.Y - 4), TextAlignment.Center);
        }
        double labelX = onFarEdge
            ? nodeRect.X - 4
            : nodeRect.X + nodeRect.Width + 4;
        return new(new Point(labelX, nodeRect.Center.Y), onFarEdge ? TextAlignment.Right : TextAlignment.Left)
            { Vertical = TextVerticalAlignment.Center };
    }

    /// <summary>Draws the sub-label of <paramref name="node"/>, when it has one, where <paramref name="position"/> puts it.</summary>
    private void DrawSubLabelIfAny(SankeyNode node, TextPlacement position, Font subLabelFontBase)
    {
        if (string.IsNullOrEmpty(node.SubLabel)) return;
        var color = node.SubLabelColor ?? subLabelFontBase.Color;
        Ctx.Draw(TextLabel.Of(node.SubLabel!), subLabelFontBase with { Color = color }, position);
    }
}
