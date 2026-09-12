---
title: "Dendrograms in C# from hierarchical clustering"
description: "Draw a dendrogram in C# from hierarchical clustering, with merge distances, color thresholds and truncated trees."
---

# Dendrograms

A dendrogram draws the result of hierarchical clustering. Every internal node is a *merge*.
Its vertical position equals the merge distance that the clustering algorithm produced.
Leaves are spaced evenly along the leaf axis. "U"-shaped segments connect each merge to its
children, which is how SciPy's `scipy.cluster.hierarchy.dendrogram` renders them.

`DendrogramSeries` accepts any `TreeNode` tree in which the `Value` of an internal node
carries the merge distance. You supply the clustering yourself: use
`HierarchicalClustering.Cluster()` from the `MatPlotLibNet.Numerics` namespace, or any
external algorithm.

## Basic dendrogram

```csharp
var tree = new TreeNode
{
    Label = "root", Value = 3.0,
    Children =
    [
        new TreeNode
        {
            Label = "left", Value = 2.0,
            Children =
            [
                new TreeNode
                {
                    Label = "AB", Value = 1.0,
                    Children = [new TreeNode { Label = "A" }, new TreeNode { Label = "B" }],
                },
                new TreeNode { Label = "C" },
            ],
        },
        new TreeNode
        {
            Label = "DE", Value = 1.0,
            Children = [new TreeNode { Label = "D" }, new TreeNode { Label = "E" }],
        },
    ],
};

Plt.Create()
    .WithTitle("Hierarchical clustering")
    .WithSize(640, 420)
    .Dendrogram(tree)
    .Save("dendrogram_basic.svg");
```

## Cut-height with cluster colours

Set `CutHeight` to draw a dashed reference line at the merge distance you choose. Each
connected component below the cut is then recoloured from a qualitative colormap. The
default colormap is `QualitativeColorMaps.Tab10`, which holds 10 distinct categorical
colours.

```csharp
Plt.Create()
    .WithTitle("Cut at distance 1.5 → 3 clusters")
    .WithSize(640, 420)
    .Dendrogram(tree, s =>
    {
        s.CutHeight = 1.5;
        s.CutLineColor = Colors.Red;
        s.ColorByCluster = true;
        s.ColorMap = QualitativeColorMaps.Tab10;
    })
    .Save("dendrogram_cut.svg");
```

`CutHeight` compares with **strict less-than** (`node.Value < cut`). That matches how SciPy's
`dendrogram(color_threshold=…)` colours a tree. A node whose `Value` equals the cut
exactly counts as *above* the cut, so it keeps the default series colour.

## Orientations

`DendrogramOrientation` has four values: `Top` (default), `Bottom`, `Left`, `Right`.
`Left`/`Right` are essential for the row dendrogram in a forthcoming `ClustermapSeries`
(Phase 3 of the v1.10 Pair-Selection Visualisation Pack).

```csharp
foreach (var orient in new[]
{
    DendrogramOrientation.Top,
    DendrogramOrientation.Bottom,
    DendrogramOrientation.Left,
    DendrogramOrientation.Right,
})
{
    Plt.Create()
        .WithTitle($"Orientation: {orient}")
        .WithSize(480, 320)
        .Dendrogram(tree, s => s.Orientation = orient)
        .Save($"dendrogram_{orient.ToString().ToLowerInvariant()}.svg");
}
```

For `Left` and `Right`, leaf labels are rotated 90° so they read along the leaf axis.

## Notes

- **Binary-tree assumption.** The leaf coordinate of an internal node is the mean of its
  immediate children. For binary trees, which is what `scipy.linkage` produces, this
  matches SciPy's `(left + right) / 2` placement exactly. N-ary trees are accepted, but
  they may render off-centre compared with the midpoint of the leftmost and rightmost
  descendant.
- **Degenerate inputs.** A tree with one leaf renders that single label at the centre of
  the plot. If every merge distance is zero, the U-shapes collapse onto the leaf baseline;
  the renderer falls back to a `maxMerge` of one so it never divides by zero.
- **Disable labels.** Set `ShowLabels = false` to hide every leaf label. This helps when
  you embed the tree in a clustermap margin, where the heatmap rows already carry labels.
