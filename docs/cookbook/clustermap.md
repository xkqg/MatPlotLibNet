---
title: "Clustermap in C#: a heatmap with dendrograms"
description: "Draw a clustermap in C#: a heatmap with row and column dendrograms, following the seaborn clustermap pattern."
---

# Clustermap

> **Install:** `dotnet add package MatPlotLibNet`

A clustermap combines a heatmap with optional row and column dendrograms in a single
subplot. It follows the seaborn `sns.clustermap` pattern. When you supply trees, the
clustermap reorders the rows and/or columns automatically to match the leaf traversal
order of the dendrogram, so the cells line up with the tree.

## Basic clustermap (heatmap only)

```csharp
double[,] data =
{
    { 0.1, 0.9, 0.3 },
    { 0.8, 0.2, 0.7 },
    { 0.4, 0.6, 0.5 },
};

var figure = Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.Clustermap(data))
    .ToSvg(); // or .ToPng(), .Build(), ...
```

## With row dendrogram

Add a `RowTree` whose leaves carry the original row indices in `TreeNode.Value`. The
`ClustermapSeries` renderer will reorder the rows to match the DFS leaf traversal of the
tree and render the dendrogram in the left panel.

```csharp
var rowTree = new TreeNode
{
    Value = 2.0, // merge distance
    Children =
    [
        new TreeNode { Value = 0 }, // original row 0
        new TreeNode
        {
            Value = 1.0,
            Children =
            [
                new TreeNode { Value = 1 }, // original row 1
                new TreeNode { Value = 2 }, // original row 2
            ]
        }
    ]
};

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.Clustermap(data, s =>
    {
        s.RowTree = rowTree;
        s.RowDendrogramWidth = 0.20; // fraction of total width, default 0.15
    }))
    .ToSvg();
```

## With both dendrograms

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.Clustermap(data, s =>
    {
        s.RowTree = rowTree;
        s.ColumnTree = colTree;
        s.RowDendrogramWidth = 0.15;      // default
        s.ColumnDendrogramHeight = 0.15;  // default
    }))
    .ToSvg();
```

## Custom colormap and labels

```csharp
using MatPlotLibNet.Styling.ColorMaps;

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.Clustermap(data, s =>
    {
        s.ColorMap = DivergingColorMaps.RdBu;
        s.ShowLabels = true;
        s.LabelFormat = "F2";
    }))
    .ToSvg();
```

## Names beside the rows and columns

`FigureTemplates.Clustermap` clusters the rows and the columns itself (Ward's method over their Euclidean distances) and takes a
name for each; it draws the same `ClustermapSeries`, its trees made from the two clusterings. The names go with the rows and
columns they were given for, wherever the clustering draws them: a row's name right of the matrix, a column's under it, never
left standing at the place its row stood in the data.

```csharp
double[,] expression =
{
    { 1.0, 2.0, 0.5 },
    { 5.0, 0.1, 4.0 },
    { 1.1, 1.9, 0.6 },
    { 4.8, 0.2, 3.9 },
};

var svg = FigureTemplates.Clustermap(
        expression,
        rowLabels: ["gene-a", "gene-b", "gene-c", "gene-d"],
        colLabels: ["sample-x", "sample-y", "sample-z"])
    .ToSvg();
```

Either list may be left out. A list with another length than the matrix has is refused, naming the parameter.

## Tree leaf convention

Leaf `TreeNode.Value` must be the **zero-based original index** of the row or column in the
data matrix (e.g. `0`, `1`, `2` for a 3-row matrix). Internal nodes carry the **merge
distance** (any positive double). A leaf's `Label` names its row (right of the matrix) or its column (under it); an empty label
names nothing. A clustering of your own becomes such a tree with `HierarchicalClustering.Cluster(distances).ToTree(names)`. If the tree is malformed (out-of-range indices, duplicates,
or wrong count) `ResolveLeafOrder` falls back to the identity order silently.

```csharp
// A valid 3-row tree: leaves have Value = original index
var validTree = new TreeNode
{
    Value = 2.0,
    Children =
    [
        new TreeNode { Value = 2 }, // row 2 comes first in display order
        new TreeNode
        {
            Value = 1.0,
            Children =
            [
                new TreeNode { Value = 0 }, // then row 0
                new TreeNode { Value = 1 }, // then row 1
            ]
        }
    ]
};
// Resulting display order: row 2, row 0, row 1
```

## Panel ratios

| Property | Type | Default | Range | Effect |
|---|---|---|---|---|
| `RowDendrogramWidth` | `double` | `0.15` | `[0.0, 0.9]` | Fraction of width for row dendrogram. `0` suppresses it. |
| `ColumnDendrogramHeight` | `double` | `0.15` | `[0.0, 0.9]` | Fraction of height for column dendrogram. `0` suppresses it. |

Values are clamped on assignment; no exception is thrown for out-of-range inputs.

## See also

- [Dendrograms](dendrograms.md) — standalone dendrogram charts
- [Heatmaps](heatmaps.md) — heatmap without reordering
