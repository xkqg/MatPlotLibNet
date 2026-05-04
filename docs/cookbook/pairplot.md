# Pair Grid (PairPlot)

A pair-grid renders an N×N matrix of subplots from N variables: each diagonal cell shows
the univariate distribution of variable *i* (histogram or KDE), each off-diagonal cell
shows a bivariate scatter of *(i, j)*. Optional hue groups colour the off-diagonal scatters
by category — the killer feature for cluster validation and category-aware EDA.

This is the seaborn `pairplot` / `PairGrid` idiom, ported to .NET as a single
`PairGridSeries` rendered as a composite by `PairGridSeriesRenderer`.

## Basic pair grid

```csharp
double[][] vars = [petalLength, petalWidth, sepalLength, sepalWidth];

var figure = Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax.PairGrid(vars))
    .ToSvg(); // or .ToPng(), .Build(), ...
```

## With axis labels

The diagonal cells carry the variable names; supply `Labels` (length must equal
`Variables.Length`) to override the default `"v0"`, `"v1"`, … placeholders.

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

The `HuePalette` defaults to `QualitativeColorMaps.Tab10`; supply your own to override:

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

`PairGridDiagonalKind.None` suppresses the diagonal entirely (off-diagonal-only view).

## Hexbin off-diagonal (high-cardinality EDA)

When sample counts per cell exceed roughly **1000 points**, scatter overplotting hides
density structure — every cell becomes a uniform blob of dots. Switch the off-diagonal
kind to `Hexbin` for a flat-top hexagonal density grid where colour encodes count:

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

`HexbinGridSize` controls the resolution: higher = finer hexes. The cliff-point where
Hexbin starts beating Scatter is approximately when `samples > gridSize²` (each hex
absorbs more than one point on average).

> ⚠️ **Hue is ignored when `OffDiagonalKind = Hexbin`.** Density encoding cannot
> cleanly carry both count and group dimensions, so a single aggregate density is
> rendered — seaborn's convention. If per-group separation matters, use
> `OffDiagonalKind.Scatter` with `HueGroups` instead.

## Triangular suppression

For large N (≥ 6 variables) rendering both halves of the symmetric scatter is wasteful —
the upper triangle carries no information not already in the lower. Hide one half:

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

`MatPlotLibNet.DataFrame.PairGrid(...)` accepts named numeric columns plus an optional
hue column. The hue column is read as strings; distinct values become the `HueLabels`,
their lexicographically sorted order becomes the integer group IDs.

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

## Configuration reference

| Property | Type | Default | Effect |
|---|---|---|---|
| `Variables` | `double[][]` | (constructor arg) | The N variables. All sub-arrays must have equal length. |
| `Labels` | `string[]?` | `null` → `"v0"`, `"v1"`, … | Diagonal axis labels. |
| `HueGroups` | `int[]?` | `null` | Group IDs parallel to samples. |
| `HueLabels` | `string[]?` | `null` (legend shows IDs) | Human-readable labels indexed by group ID. |
| `HuePalette` | `Color[]?` | `null` → Tab10 | Optional explicit palette. |
| `DiagonalKind` | `PairGridDiagonalKind` | `Histogram` | `Histogram` / `Kde` / `None`. |
| `OffDiagonalKind` | `PairGridOffDiagonalKind` | `Scatter` | `Scatter` / `None` / `Hexbin` (high-cardinality density). |
| `Triangular` | `PairGridTriangle` | `Both` | `Both` / `LowerOnly` / `UpperOnly`. |
| `DiagonalBins` | `int` | `20` | Histogram bin count per diagonal cell. |
| `MarkerSize` | `double` | `3.0` | Off-diagonal scatter dot radius (px). Note: not pt² (differs from `ScatterSeries.MarkerSize`). |
| `CellSpacing` | `double` | `0.02` | Gutter between cells, clamped `[0, 0.2]`. |
| `HexbinGridSize` | `int` | `15` | Hex tiling resolution per cell when `OffDiagonalKind = Hexbin`. |
| `OffDiagonalColorMap` | `IColorMap?` | `null` → Viridis | Colormap for off-diagonal density when Hexbin is active. |

## See also

- [Heatmaps](heatmaps.md) — single-cell colour matrix
- [Clustermap](clustermap.md) — heatmap with row/column dendrograms
- [Distribution](distribution.md) — standalone histogram / KDE / violin / box / rug
