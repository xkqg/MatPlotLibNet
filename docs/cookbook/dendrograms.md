# Dendrograms

Hierarchical-clustering visualisation: every internal node is a *merge* whose vertical
position equals the merge distance produced by the clustering algorithm. Leaves are evenly
spaced along the leaf-axis; "U"-shaped segments connect each merge to its children, matching
SciPy's `scipy.cluster.hierarchy.dendrogram` rendering convention.

`DendrogramSeries` accepts any `TreeNode` tree where internal-node `Value` carries the
merge distance — bring your own clustering output (`HierarchicalClustering.Cluster()` from
the `MatPlotLibNet.Numerics` namespace, or any external algorithm).

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

Set `CutHeight` to draw a dashed reference line at the chosen merge distance and recolour
each connected component below the cut from a qualitative colormap. The default
colormap is `QualitativeColorMaps.Tab10` (10 distinct categorical colours).

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

`CutHeight` semantics use **strict less-than** (`node.Value < cut`), matching SciPy's
`dendrogram(color_threshold=…)` visual convention. A node whose `Value` equals the cut
exactly is treated as *above* the cut and is rendered in the default series colour.

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

- **Binary-tree assumption.** Internal-node leaf-coordinates are computed as the mean
  of immediate children. For binary trees (the standard `scipy.linkage` output), this
  matches SciPy's `(left + right) / 2` placement exactly. N-ary trees are accepted but
  may render off-centre relative to a leftmost+rightmost-descendant midpoint.
- **Degenerate inputs.** Single-leaf trees render the lone label at the plot centre.
  All-zero merge distances collapse the U-shapes to the leaf baseline (the renderer
  falls back to a unit `maxMerge` to avoid division by zero).
- **Disable labels.** Set `ShowLabels = false` to suppress every leaf label — useful
  for embedding the tree in a clustermap margin where the heatmap rows already carry labels.
