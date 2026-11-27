---
title: "Pair plot matrix in C#"
description: "Draw a pair grid in C#: an N by N matrix of subplots with distributions on the diagonal and scatter plots off it."
---

# Pair Grid (PairPlot)

> **Install:** `dotnet add package MatPlotLibNet`

A pair grid renders an N×N matrix of subplots from N variables. Each diagonal cell shows
the univariate distribution of variable *i*, as a histogram or a KDE. Each off-diagonal
cell shows a bivariate scatter of *(i, j)*. Optional hue groups colour the off-diagonal
scatters by category. Use hue groups to validate clusters and to explore data per
category (EDA).

This is the .NET port of seaborn's `pairplot` / `PairGrid`. The whole grid is one
`PairGridSeries`, which `PairGridSeriesRenderer` draws as a composite.

## Basic pair grid

```csharp
double[][] vars = [petalLength, petalWidth, sepalLength, sepalWidth];

var figure = Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.PairGrid(vars))
    .ToSvg(); // or .ToPng(), .Build(), ...
```

## With axis labels

Set `Labels` to name the variables, one name each: a variable's name is drawn under its column in the bottom row and, turned
a quarter, beside its row in the left column, as seaborn's `pairplot` names them. Without `Labels` no name is drawn, and a
variable past the end of a shorter list goes unnamed.

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.PairGrid(vars, s =>
    {
        s.Labels = ["Petal L", "Petal W", "Sepal L", "Sepal W"];
    }))
    .ToSvg();
```

## With hue groups

```csharp
int[]    species = irisSpecies.Select(s => (int)s).ToArray();
string[] speciesNames = ["Setosa", "Versicolor", "Virginica"];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.PairGrid(vars, s =>
    {
        s.Labels    = ["Petal L", "Petal W", "Sepal L", "Sepal W"];
        s.HueGroups = species;       // length must match Variables[0].Length
        s.HueLabels = speciesNames;  // legend labels per group ID
    }))
    .ToSvg();
```

`HuePalette` defaults to `QualitativeColorMaps.Tab10`. Set it to use your own colours:

```csharp
s.HuePalette = [Colors.Tab10Blue, Colors.Tab10Orange, Colors.Tab10Green];
```

## KDE on the diagonal

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.PairGrid(vars, s =>
    {
        s.DiagonalKind = PairGridDiagonalKind.Kde;
    }))
    .ToSvg();
```

`PairGridDiagonalKind.None` suppresses the diagonal entirely, which leaves an
off-diagonal-only view.

## Hexbin off-diagonal (high-cardinality EDA)

Above roughly **1000 points** per cell, the scatter dots overlap so much that the density
is no longer visible: every cell becomes one uniform blob of dots. Switch the off-diagonal
kind to `Hexbin` instead. It draws a grid of hexagons, with a vertex up and down as matplotlib's hexbin draws them, and the colour of each
hexagon shows how many points fall in it:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.PairGrid(vars, s =>
    {
        s.OffDiagonalKind     = PairGridOffDiagonalKind.Hexbin;
        s.HexbinGridSize      = 20;                             // default 15
        s.OffDiagonalColorMap = ColorMaps.Plasma;               // default Viridis
    }))
    .ToSvg();
```

`HexbinGridSize` controls the resolution: a higher value gives finer hexes. Hexbin starts
to work better than Scatter at roughly `samples > gridSize²`, the point where each hex
absorbs more than one point on average.

> ⚠️ **Hue is ignored when `OffDiagonalKind = Hexbin`.** One colour scale cannot carry
> both the count and the group, so the cell shows a single aggregate density. seaborn
> does the same. If you need the groups kept apart, use `OffDiagonalKind.Scatter` with
> `HueGroups` instead.

## Triangular suppression

For large N (≥ 6 variables), drawing both halves of the symmetric scatter wastes space:
the upper triangle carries no information that the lower one does not already show.
Hide one half:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.PairGrid(vars, s =>
    {
        s.Triangular = PairGridTriangle.LowerOnly;
    }))
    .ToSvg();
```

| `PairGridTriangle` | Behaviour |
|---|---|
| `Both`      | Full N×N grid (default). |
| `LowerOnly` | Hide cells where row < col. |
| `UpperOnly` | Hide cells where row > col. |

## From a DataFrame

The DataFrame package's `df.PairGrid(...)` takes the names of numeric columns, plus the name
of an optional hue column. It reads the hue column as strings. The distinct values become
the `HueLabels`, and their lexicographically sorted order becomes the integer group IDs.

```csharp
using MatPlotLibNet;
using Microsoft.Data.Analysis;

DataFrame df = ...; // columns: petal_l, petal_w, sepal_l, sepal_w, species

string svg = df.PairGrid(
        ["petal_l", "petal_w", "sepal_l", "sepal_w"],
        hue: "species")
    .WithTitle("Iris dataset")
    .ToSvg();
```

## The pair plot template

`FigureTemplates.PairPlot(columns, columnNames, bins)` and `new PairPlotFigure(columns) { ColumnNames = …, Hue = … }` draw this
same series on a figure 200 pixels a variable square: the column names are its `Labels`, the bins its `DiagonalBins`, and a hue
label per observation splits it into hue groups, numbered in the order the labels are first seen and named by them.

```csharp
string svg = FigureTemplates.PairPlot(vars, ["Petal L", "Petal W", "Sepal L", "Sepal W"]).ToSvg();
```

## Configuration reference

| Property | Type | Default | Effect |
|---|---|---|---|
| `Variables` | `double[][]` | (constructor arg) | The N variables. All sub-arrays must have equal length. |
| `Labels` | `string[]?` | `null` (no names) | The variables' names, under the bottom row and beside the left column. |
| `HueGroups` | `int[]?` | `null` | One group ID per sample. |
| `HueLabels` | `string[]?` | `null` (legend shows IDs) | Human-readable labels indexed by group ID. |
| `HuePalette` | `Color[]?` | `null` → the theme's cycle | Optional explicit palette. |
| `DiagonalKind` | `PairGridDiagonalKind` | `Histogram` | `Histogram` / `Kde` / `None`. |
| `OffDiagonalKind` | `PairGridOffDiagonalKind` | `Scatter` | `Scatter` / `None` / `Hexbin` (high-cardinality density). |
| `Triangular` | `PairGridTriangle` | `Both` | `Both` / `LowerOnly` / `UpperOnly`. |
| `DiagonalBins` | `int` | `20` | Histogram bin count per diagonal cell. |
| `MarkerSize` | `double` | `3.0` | Off-diagonal scatter dot radius (px). Note: not pt² (differs from `ScatterSeries.MarkerSize`). Left alone, the matplotlib themes draw the dot at the size `Axes.scatter` draws where it is given none (6 points across under v2, 4.47 under classic), as seaborn's `pairplot` does, and the library's own themes at 3 px; a radius you set is drawn as pixels. |
| `CellSpacing` | `double` | `0.02` | Gutter between cells, clamped `[0, 0.2]`. |
| `HexbinGridSize` | `int` | `15` | Hex tiling resolution per cell when `OffDiagonalKind = Hexbin`. |
| `OffDiagonalColorMap` | `IColorMap?` | `null` → Viridis | Colormap for off-diagonal density when Hexbin is active. |

## See also

- [Heatmaps](heatmaps.md) — single-cell colour matrix
- [Clustermap](clustermap.md) — heatmap with row/column dendrograms
- [Distribution](distribution.md) — standalone histogram / KDE / violin / box / rug
