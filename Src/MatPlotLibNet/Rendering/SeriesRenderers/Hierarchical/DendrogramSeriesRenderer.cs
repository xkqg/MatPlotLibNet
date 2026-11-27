// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="DendrogramSeries"/> as a tree of "U"-shaped segments.
/// Leaves are evenly spaced along the leaf-axis (0..N-1); internal nodes are positioned at
/// the mean leaf-coordinate of their children with merge-axis coordinate equal to
/// <c>node.Value</c> (the merge distance produced by the clustering algorithm).</summary>
internal sealed class DendrogramSeriesRenderer : SeriesRenderer<DendrogramSeries>
{
    /// <inheritdoc />
    public DendrogramSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(DendrogramSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(DendrogramSeries series)
    {
        // ReferenceEqualityComparer.Instance: TreeNode is a value-equal record, so two
        // sibling leaves with identical labels would otherwise collide in this dictionary.
        // Layout/cluster-color tables are keyed by node identity, not by node content.
        var leaves = new List<TreeNode>();
        var layout = new Dictionary<TreeNode, NodeLayout>(ReferenceEqualityComparer.Instance);
        ComputeLayout(series.Root, leaves, layout);

        double maxMerge = ComputeMaxMerge(layout);
        var clusterColors = AssignClusterColors(series, Context.Theme);
        var bounds = Context.Area.PlotBounds;
        Color stroke = SeriesColor;
        // Where the tree stands is resolved once: every leg, the cut line and the labels are placed through the frame.
        var frame = DendrogramFrame.Of(series.Orientation);

        DrawSegments(series, frame, layout, bounds, leaves.Count, maxMerge, clusterColors, stroke);

        if (series.CutHeight is double cut)
            DrawCutLine(series, frame, bounds, leaves.Count, maxMerge, cut);

        if (series.ShowLabels)
            DrawLabels(frame, leaves, layout, bounds, maxMerge);
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    /// <summary>Coordinate pair for one tree node in dendrogram space: leaf-axis position
    /// (0..N-1 for evenly-spaced leaves, mean of children for internal nodes) and merge-axis
    /// position (0 for leaves, <c>node.Value</c> for internal nodes).</summary>
    private readonly record struct NodeLayout(double LeafCoord, double MergeCoord);

    /// <summary>Recursively assigns each node its leaf-axis and merge-axis coordinates.</summary>
    /// <remarks>For binary trees (the standard <c>scipy.cluster.hierarchy.linkage</c> output) the
    /// mean-of-immediate-children leaf-coord is identical to SciPy's
    /// <c>(left_child + right_child) / 2</c> placement. For N-ary trees this is a natural
    /// generalisation but is not the canonical SciPy algorithm — heavily unbalanced N-ary
    /// merges may render off-centre relative to a leftmost+rightmost-descendant midpoint.</remarks>
    private static void ComputeLayout(TreeNode node, List<TreeNode> leaves, Dictionary<TreeNode, NodeLayout> layout)
    {
        if (node.Children.Count == 0)
        {
            layout[node] = new NodeLayout(leaves.Count, 0.0);
            leaves.Add(node);
            return;
        }
        double sum = 0;
        foreach (var child in node.Children)
        {
            ComputeLayout(child, leaves, layout);
            sum += layout[child].LeafCoord;
        }
        layout[node] = new NodeLayout(sum / node.Children.Count, node.Value);
    }

    private static double ComputeMaxMerge(Dictionary<TreeNode, NodeLayout> layout)
    {
        double maxMerge = 0;
        foreach (var l in layout.Values)
            if (l.MergeCoord > maxMerge) maxMerge = l.MergeCoord;
        return maxMerge < 1e-10 ? 1.0 : maxMerge;
    }

    // ── Cluster assignment ───────────────────────────────────────────────────

    /// <summary>Top-down walk that assigns every node below the cut to a cluster colour.
    /// Nodes whose merge distance is at-or-above the cut stay uncoloured (rendered with
    /// the series' cycle colour). Returns an empty map when no cut height is set or when
    /// <see cref="DendrogramSeries.ColorByCluster"/> is <see langword="false"/>.</summary>
    private static Dictionary<TreeNode, Color> AssignClusterColors(DendrogramSeries series, Theme theme)
    {
        // ReferenceEqualityComparer: see Render() comment — keyed by node identity, not value.
        var map = new Dictionary<TreeNode, Color>(ReferenceEqualityComparer.Instance);
        if (series.CutHeight is not double cut || !series.ColorByCluster) return map;

        var roots = new List<TreeNode>();
        CollectClusterRoots(series.Root, cut, roots);

        IColorMap cmap = series.ColorMap ?? theme.CycleMap;
        for (int k = 0; k < roots.Count; k++)
        {
            Color color = cmap.GetColor(k.ColormapFraction(roots.Count, singletonT: 0.0));
            ColorSubtree(roots[k], color, map);
        }
        return map;
    }

    /// <summary>Walks the tree top-down, collecting nodes whose merge distance is strictly
    /// less than <paramref name="cut"/>. Strict-less-than matches SciPy's
    /// <c>scipy.cluster.hierarchy.dendrogram</c> <c>color_threshold</c> visual convention
    /// (a node whose <see cref="TreeNode.Value"/> equals the cut exactly is treated as
    /// above the cut and is NOT a cluster root). This differs from
    /// <c>scipy.cluster.hierarchy.fcluster</c>'s <c>distance</c> criterion which uses
    /// less-than-or-equal.</summary>
    private static void CollectClusterRoots(TreeNode node, double cut, List<TreeNode> roots)
    {
        if (node.Value < cut || node.Children.Count == 0)
        {
            roots.Add(node);
            return;
        }
        foreach (var child in node.Children)
            CollectClusterRoots(child, cut, roots);
    }

    private static void ColorSubtree(TreeNode node, Color color, Dictionary<TreeNode, Color> map)
    {
        foreach (var n in node.Walk()) map[n] = color;
    }

    // ── Segment emission ─────────────────────────────────────────────────────

    private void DrawSegments(DendrogramSeries series, DendrogramFrame frame, Dictionary<TreeNode, NodeLayout> layout,
        Rect bounds, int leafCount, double maxMerge, Dictionary<TreeNode, Color> clusterColors,
        Color defaultStroke)
    {
        DrawNodeSegments(frame, series.Root, layout, bounds, leafCount, maxMerge, clusterColors, defaultStroke);
    }

    private void DrawNodeSegments(DendrogramFrame frame, TreeNode node,
        Dictionary<TreeNode, NodeLayout> layout, Rect bounds, int leafCount, double maxMerge,
        Dictionary<TreeNode, Color> clusterColors, Color defaultStroke)
    {
        if (node.Children.Count == 0) return;

        var parentLayout = layout[node];
        double parentMergePx = frame.MergePixel(parentLayout.MergeCoord, bounds, maxMerge);

        foreach (var child in node.Children)
        {
            DrawNodeSegments(frame, child, layout, bounds, leafCount, maxMerge, clusterColors, defaultStroke);

            var childLayout = layout[child];
            double childLeafPx = frame.LeafPixel(childLayout.LeafCoord, bounds, leafCount);
            double childMergePx = frame.MergePixel(childLayout.MergeCoord, bounds, maxMerge);
            Color stroke = clusterColors.TryGetValue(child, out var c) ? c : defaultStroke;
            DrawLeg(frame.At(parentMergePx, childLeafPx), frame.At(childMergePx, childLeafPx), stroke);
        }

        var first = layout[node.Children[0]];
        var last  = layout[node.Children[^1]];
        double firstLeafPx = frame.LeafPixel(first.LeafCoord, bounds, leafCount);
        double lastLeafPx  = frame.LeafPixel(last.LeafCoord,  bounds, leafCount);
        Color spanStroke = clusterColors.TryGetValue(node, out var sc) ? sc : defaultStroke;
        DrawLeg(frame.At(parentMergePx, firstLeafPx), frame.At(parentMergePx, lastLeafPx), spanStroke);
    }

    /// <summary>Emits one leg of a "U" between two points the frame placed.</summary>
    private void DrawLeg(Point p1, Point p2, Color stroke) =>
        Ctx.DrawLine(p1, p2, StrokeStyle.Solid(stroke, HierarchicalLayout.Dendrogram.LineThickness));

    // ── Cut line ─────────────────────────────────────────────────────────────

    private void DrawCutLine(DendrogramSeries series, DendrogramFrame frame, Rect bounds, int leafCount, double maxMerge,
        double cut)
    {
        Color color = series.CutLineColor ?? Colors.DarkGray;
        double mergePx = frame.MergePixel(cut, bounds, maxMerge);
        double firstLeafPx = frame.LeafPixel(0,             bounds, leafCount);
        double lastLeafPx  = frame.LeafPixel(leafCount - 1, bounds, leafCount);

        Ctx.DrawLine(frame.At(mergePx, firstLeafPx), frame.At(mergePx, lastLeafPx),
            Slot.StrokeOf(color, LineWidthRule.OwnPixel, LineStyle.Dashed));
    }

    // ── Labels ───────────────────────────────────────────────────────────────

    private void DrawLabels(DendrogramFrame frame, List<TreeNode> leaves,
        Dictionary<TreeNode, NodeLayout> layout, Rect bounds, double maxMerge)
    {
        var font = Context.Theme.DataTextFontInThemeWeight();
        for (int i = 0; i < leaves.Count; i++)
        {
            var leaf = leaves[i];
            double leafPx = frame.LeafPixel(layout[leaf].LeafCoord, bounds, leaves.Count);
            double mergeBaselinePx = frame.MergePixel(0, bounds, maxMerge);
            frame.DrawLabel(Ctx, leaf.Label, leafPx, mergeBaselinePx, font);
        }
    }
}
