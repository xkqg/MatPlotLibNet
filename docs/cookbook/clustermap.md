# Clustermap

A clustermap composites a heatmap with optional row and column dendrograms into a single
subplot — the seaborn `sns.clustermap` idiom. When trees are provided, rows and/or columns
are automatically reordered to match the dendrogram leaf traversal order so cells align
visually with the tree structure.

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
        s.ColorMap = ColorMaps.RdBu;
        s.ShowLabels = true;
        s.LabelFormat = "F2";
    }))
    .ToSvg();
```

## Tree leaf convention

Leaf `TreeNode.Value` must be the **zero-based original index** of the row or column in the
data matrix (e.g. `0`, `1`, `2` for a 3-row matrix). Internal nodes carry the **merge
distance** (any positive double). If the tree is malformed (out-of-range indices, duplicates,
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
