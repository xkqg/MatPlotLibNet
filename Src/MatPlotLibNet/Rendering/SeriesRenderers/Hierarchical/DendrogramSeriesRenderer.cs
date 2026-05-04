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
    public override void Render(DendrogramSeries series)
    {
        // ReferenceEqualityComparer.Instance: TreeNode is a value-equal record, so two
        // sibling leaves with identical labels would otherwise collide in this dictionary.
        // Layout/cluster-color tables are keyed by node identity, not by node content.
        var leaves = new List<TreeNode>();
        var layout = new Dictionary<TreeNode, NodeLayout>(ReferenceEqualityComparer.Instance);
        ComputeLayout(series.Root, leaves, layout);

        double maxMerge = ComputeMaxMerge(layout);
        var clusterColors = AssignClusterColors(series, layout);
        var bounds = Context.Area.PlotBounds;
        Color stroke = SeriesColor;

        DrawSegments(series, layout, bounds, leaves.Count, maxMerge, clusterColors, stroke);

        if (series.CutHeight is double cut)
            DrawCutLine(series, bounds, leaves.Count, maxMerge, cut);

        if (series.ShowLabels)
            DrawLabels(series, leaves, layout, bounds, maxMerge);
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    /// <summary>Coordinate pair for one tree node in dendrogram space: leaf-axis position
    /// (0..N-1 for evenly-spaced leaves, mean of children for internal nodes) and merge-axis
    /// position (0 for leaves, <c>node.Value</c> for internal nodes).</summary>
    private readonly record struct NodeLayout(double LeafCoord, double MergeCoord);

    /// <summary>Recursively assigns each node its leaf-axis and merge-axis coordinates.</summary>
    /// <remarks>For binary trees (the standard <c>scipy.linkage</c> output) the
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
    private static Dictionary<TreeNode, Color> AssignClusterColors(DendrogramSeries series,
        Dictionary<TreeNode, NodeLayout> layout)
    {
        // ReferenceEqualityComparer: see Render() comment — keyed by node identity, not value.
        var map = new Dictionary<TreeNode, Color>(ReferenceEqualityComparer.Instance);
        if (series.CutHeight is not double cut || !series.ColorByCluster) return map;

        var roots = new List<TreeNode>();
        CollectClusterRoots(series.Root, cut, roots);

        IColorMap cmap = series.GetColorMapOrDefault(QualitativeColorMaps.Tab10);
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

    private void DrawSegments(DendrogramSeries series, Dictionary<TreeNode, NodeLayout> layout,
        Rect bounds, int leafCount, double maxMerge, Dictionary<TreeNode, Color> clusterColors,
        Color defaultStroke)
    {
        DrawNodeSegments(series, series.Root, layout, bounds, leafCount, maxMerge, clusterColors, defaultStroke);
    }

    private void DrawNodeSegments(DendrogramSeries series, TreeNode node,
        Dictionary<TreeNode, NodeLayout> layout, Rect bounds, int leafCount, double maxMerge,
        Dictionary<TreeNode, Color> clusterColors, Color defaultStroke)
    {
        if (node.Children.Count == 0) return;

        var parentLayout = layout[node];
        double parentMergePx = MergePixel(series.Orientation, parentLayout.MergeCoord, bounds, maxMerge);

        foreach (var child in node.Children)
        {
            DrawNodeSegments(series, child, layout, bounds, leafCount, maxMerge, clusterColors, defaultStroke);

            var childLayout = layout[child];
            double childLeafPx = LeafPixel(series.Orientation, childLayout.LeafCoord, bounds, leafCount);
            double childMergePx = MergePixel(series.Orientation, childLayout.MergeCoord, bounds, maxMerge);
            Color stroke = clusterColors.TryGetValue(child, out var c) ? c : defaultStroke;
            DrawAxisAlignedLine(series.Orientation, parentMergePx, childLeafPx, childMergePx, childLeafPx, stroke);
        }

        var first = layout[node.Children[0]];
        var last  = layout[node.Children[^1]];
        double firstLeafPx = LeafPixel(series.Orientation, first.LeafCoord, bounds, leafCount);
        double lastLeafPx  = LeafPixel(series.Orientation, last.LeafCoord,  bounds, leafCount);
        Color spanStroke = clusterColors.TryGetValue(node, out var sc) ? sc : defaultStroke;
        DrawAxisAlignedLine(series.Orientation, parentMergePx, firstLeafPx, parentMergePx, lastLeafPx, spanStroke);
    }

    /// <summary>Emits one "U"-segment leg whose endpoints are expressed in (merge-axis,
    /// leaf-axis) coordinates. The actual <c>(x,y)</c> mapping depends on
    /// <see cref="DendrogramOrientation"/>.</summary>
    private void DrawAxisAlignedLine(DendrogramOrientation orientation,
        double mergePx1, double leafPx1, double mergePx2, double leafPx2, Color stroke)
    {
        Point p1, p2;
        if (orientation == DendrogramOrientation.Top || orientation == DendrogramOrientation.Bottom)
        {
            p1 = new Point(leafPx1, mergePx1);
            p2 = new Point(leafPx2, mergePx2);
        }
        else
        {
            p1 = new Point(mergePx1, leafPx1);
            p2 = new Point(mergePx2, leafPx2);
        }
        Ctx.DrawLine(p1, p2, stroke, HierarchicalLayout.Dendrogram.LineThickness, LineStyle.Solid);
    }

    // ── Cut line ─────────────────────────────────────────────────────────────

    private void DrawCutLine(DendrogramSeries series, Rect bounds, int leafCount, double maxMerge, double cut)
    {
        Color color = series.CutLineColor ?? Colors.DarkGray;
        double mergePx = MergePixel(series.Orientation, cut, bounds, maxMerge);
        double firstLeafPx = LeafPixel(series.Orientation, 0,             bounds, leafCount);
        double lastLeafPx  = LeafPixel(series.Orientation, leafCount - 1, bounds, leafCount);

        Point p1, p2;
        if (series.Orientation is DendrogramOrientation.Top or DendrogramOrientation.Bottom)
        {
            p1 = new Point(firstLeafPx, mergePx);
            p2 = new Point(lastLeafPx,  mergePx);
        }
        else
        {
            p1 = new Point(mergePx, firstLeafPx);
            p2 = new Point(mergePx, lastLeafPx);
        }
        Ctx.DrawLine(p1, p2, color, HierarchicalLayout.Dendrogram.CutLineThickness, LineStyle.Dashed);
    }

    // ── Labels ───────────────────────────────────────────────────────────────

    private void DrawLabels(DendrogramSeries series, List<TreeNode> leaves,
        Dictionary<TreeNode, NodeLayout> layout, Rect bounds, double maxMerge)
    {
        var font = Context.Theme.DefaultFont;
        for (int i = 0; i < leaves.Count; i++)
        {
            var leaf = leaves[i];
            double leafPx = LeafPixel(series.Orientation, layout[leaf].LeafCoord, bounds, leaves.Count);
            double mergeBaselinePx = MergePixel(series.Orientation, 0, bounds, maxMerge);

            switch (series.Orientation)
            {
                case DendrogramOrientation.Top:
                    Ctx.DrawText(leaf.Label, new Point(leafPx, mergeBaselinePx + HierarchicalLayout.Dendrogram.LabelOffsetPx + font.Size),
                        font, TextAlignment.Center);
                    break;
                case DendrogramOrientation.Bottom:
                    Ctx.DrawText(leaf.Label, new Point(leafPx, mergeBaselinePx - HierarchicalLayout.Dendrogram.LabelOffsetPx),
                        font, TextAlignment.Center);
                    break;
                case DendrogramOrientation.Left:
                    Ctx.DrawText(leaf.Label, new Point(mergeBaselinePx + HierarchicalLayout.Dendrogram.LabelOffsetPx, leafPx),
                        font, TextAlignment.Left, rotation: 90);
                    break;
                default:
                    Ctx.DrawText(leaf.Label, new Point(mergeBaselinePx - HierarchicalLayout.Dendrogram.LabelOffsetPx, leafPx),
                        font, TextAlignment.Right, rotation: 90);
                    break;
            }
        }
    }

    // ── Pixel mapping ────────────────────────────────────────────────────────

    /// <summary>Maps a leaf index (0..N-1) to its pixel coordinate along the leaf axis.
    /// For a single-leaf tree (<paramref name="leafCount"/> ≤ 1) the leaf is centred at
    /// the midpoint of the plot extent so the lone leaf is visually anchored.</summary>
    private static double LeafPixel(DendrogramOrientation orientation, double leafCoord, Rect bounds, int leafCount)
    {
        double t = leafCount <= 1 ? 0.5 : (leafCoord + 0.5) / leafCount;
        return orientation is DendrogramOrientation.Top or DendrogramOrientation.Bottom
            ? bounds.X + t * bounds.Width
            : bounds.Y + t * bounds.Height;
    }

    /// <summary>Maps a merge distance to its pixel coordinate along the merge axis.</summary>
    private static double MergePixel(DendrogramOrientation orientation, double mergeCoord, Rect bounds, double maxMerge)
    {
        double t = mergeCoord / maxMerge;
        return orientation switch
        {
            DendrogramOrientation.Top    => bounds.Bottom - t * bounds.Height,
            DendrogramOrientation.Bottom => bounds.Y      + t * bounds.Height,
            DendrogramOrientation.Left   => bounds.Right  - t * bounds.Width,
            _                            => bounds.X      + t * bounds.Width,
        };
    }
}
