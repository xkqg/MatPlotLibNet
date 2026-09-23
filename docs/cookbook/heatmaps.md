---
title: "Heatmaps in C# with colormaps and colorbars"
description: "Draw heatmaps in C# with 148 colormaps, annotated cells, colorbars and custom normalisation."
---

# Heatmaps & Colormaps

> **Install:** `dotnet add package MatPlotLibNet`

## Heatmap with colorbar

```csharp
double[,] matrix = new double[10, 10];
for (int r = 0; r < 10; r++)
    for (int c = 0; c < 10; c++)
        matrix[r, c] = Math.Sin(r * 0.5) * Math.Cos(c * 0.5);

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Heatmap — Plasma")
        .Heatmap(matrix)
        .WithColorMap("plasma")
        .WithColorBar(cb => cb with { Label = "Intensity" }))
    .TightLayout()
    .Save("heatmap.svg");
```

![Heatmap](../images/heatmap_colormap.png)

## Colormap comparison

MatPlotLibNet ships 148 colormaps (74 base maps, each with an auto-registered reversed `_r` variant). Here are four popular ones side by side:

```csharp
string[] maps = ["viridis", "turbo", "coolwarm", "greys"];
var builder = Plt.Create()
    .WithTitle("Colormap Comparison")
    .WithSize(1200, 800);

for (int i = 0; i < maps.Length; i++)
{
    var mapName = maps[i];
    builder.AddSubPlot(2, 2, i + 1, ax => ax
        .WithTitle(mapName)
        .Heatmap(matrix)
        .WithColorMap(mapName)
        .WithColorBar());
}

builder.TightLayout().Save("colormap_comparison.svg");
```

![Colormap comparison](../images/colormap_comparison.png)

## Colorbar customization

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Heatmap(matrix)
        .WithColorMap("viridis")
        .WithColorBar(cb => cb with
        {
            Label = "Temperature (°C)",
            Orientation = ColorBarOrientation.Horizontal,
        }))
    .TightLayout()
    .Save("colorbar_custom.svg");
```

## Color normalization

Control how data values map to colors:

```csharp
// Log normalization — useful when data spans orders of magnitude
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Heatmap(logData)
        .WithColorMap("plasma")
        .WithNormalizer(Normalizer.Log())
        .WithColorBar(cb => cb with { Label = "Log scale" }))
    .Save("heatmap_log.svg");

// Two-slope normalization — center on zero
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Heatmap(divergingData)
        .WithColorMap("coolwarm")
        .WithNormalizer(Normalizer.TwoSlope(vCenter: 0))
        .WithColorBar(cb => cb with { Label = "Anomaly" }))
    .Save("heatmap_twoslope.svg");
```

## Heatmap with custom series config

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Heatmap(matrix, s =>
        {
            s.ColorMap = ColorMaps.Turbo;
        })
        .WithColorBar())
    .Save("heatmap_series.svg");
```

## Annotated heatmap (correlation matrix)

`ShowLabels = true` renders each cell's numeric value on top of the colour fill. The text
colour is chosen per cell, black or white, using Rec. 709 luminance, so labels stay
readable across the colour map. Use `LabelFormat` with any standard .NET numeric
format string, for example `"F2"` (the default) for two decimals or `"P1"` for percent.

```csharp
double[,] corr = ComputeCorrelationMatrix(returns);

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Asset Correlation Matrix")
        .Heatmap(corr, s =>
        {
            s.ColorMap = new ReversedColorMap(DivergingColorMaps.RdBu);
            s.ShowLabels = true;
            s.LabelFormat = "F2";
        })
        .WithColorBar())
    .Save("heatmap_annotated.svg");
```

Override the auto-contrast text colour with `CellValueColor` when you want the labels to
match a brand colour or stay constant across the matrix.

## Triangular-mask heatmap

A symmetric matrix (correlation, covariance, distance) holds the same values above and
below the diagonal. `MaskMode` hides the redundant half, so each pair is shown once.
The strict variants also hide the diagonal, which is a constant 1 for correlation matrices.

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Lower-triangle correlation")
        .Heatmap(corr, s =>
        {
            s.ColorMap = new ReversedColorMap(DivergingColorMaps.RdBu);
            s.ShowLabels = true;
            s.MaskMode = HeatmapMaskMode.UpperTriangleStrict;  // hide upper half + diagonal
        })
        .WithColorBar())
    .Save("heatmap_lower_triangle.svg");
```

`HeatmapMaskMode` values:

| Value | Hides cells where | Diagonal kept? |
|---|---|---|
| `None` | (nothing) | yes |
| `UpperTriangle` | `col > row` | yes |
| `LowerTriangle` | `col < row` | yes |
| `UpperTriangleStrict` | `col >= row` | **no** |
| `LowerTriangleStrict` | `col <= row` | **no** |

## Image (imshow)

Display 2D arrays as images:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Image(matrix)
        .WithColorMap("gray")
        .WithColorBar())
    .Save("imshow.svg");
```

## 2D histogram (density)

```csharp
var rng = new Random(42);
// Box-Muller: a normally distributed sample built from two uniform ones.
double Gaussian(double mean, double sd) =>
    mean + sd * Math.Sqrt(-2 * Math.Log(rng.NextDouble())) * Math.Cos(2 * Math.PI * rng.NextDouble());
double[] x = Enumerable.Range(0, 5000).Select(_ => Gaussian(0, 1)).ToArray();
double[] y = Enumerable.Range(0, 5000).Select(_ => Gaussian(0, 1)).ToArray();

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Histogram2D(x, y, bins: 30)
        .WithColorMap("viridis")
        .WithColorBar(cb => cb with { Label = "Count" }))
    .Save("hist2d.svg");
```

## Pseudocolor mesh

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Pcolormesh(xEdges, yEdges, data)
        .WithColorMap("inferno")
        .WithColorBar())
    .Save("pcolormesh.svg");
```

## Popular colormaps

| Category | Colormaps |
|---|---|
| Perceptual | viridis, plasma, inferno, magma, cividis |
| Sequential | greys, purples, blues, greens, oranges, reds |
| Diverging | coolwarm, RdBu, PiYG, PRGn, BrBG, seismic |
| Cyclic | twilight, hsv |
| Qualitative | tab10, tab20, Set1, Set2, Set3, Pastel1, Paired |
| Accessible | okabe_ito, petroff6, petroff8, petroff10 |
| Other | turbo, jet, hot, cool, spring, summer, autumn, winter |

The accessible row is the one to reach for when a chart has to be read by everyone. `okabe_ito` is the
eight-colour palette from Okabe and Ito; `petroff6`, `petroff8` and `petroff10` are the sequences from
Petroff's *Accessible Color Sequences for Data Visualization*, which pick colours so that every pair inside
a sequence stays apart under the common forms of colour blindness, and in grayscale as well. Pick the one
whose number matches how many things you are drawing: a six-series chart drawn with the ten-colour sequence
gets six colours that were chosen to sit beside four others. They are the same values matplotlib ships under
those names.

## Calendar heatmap (GitHub-style)

The heatmap is 52 × 7. Each row is a week and each column is a day of the week.

```csharp
var rng = new Random(7);
var data = new double[52, 7];
for (int w = 0; w < 52; w++)
    for (int d = 0; d < 7; d++)
    {
        double base_ = (d < 5) ? rng.NextDouble() * 8 : rng.NextDouble() * 2;
        data[w, d] = Math.Max(0, base_ + w * 0.05 + rng.NextDouble() * 2 - 1);
    }

Plt.Create()
    .WithTitle("Calendar Heatmap — Contributions")
    .WithSize(1100, 300)
    .AddSubPlot(1, 1, 1, ax => ax
        .Heatmap(data, s => { s.ColorMap = ColorMaps.Viridis; })
        .WithColorBar()
        .SetXLabel("Week")
        .SetYLabel("Day"))
    .Save("calendar_heatmap.svg");
```

![Calendar heatmap](../images/calendar_heatmap.png)
