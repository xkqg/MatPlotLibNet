---
title: "Contour plots in C# and .NET"
description: "Draw contour and filled contour plots in C# from a two-dimensional grid, with colormaps and labelled contour levels."
---

# Contour Plots

> **Install:** `dotnet add package MatPlotLibNet`

## Contour lines with labels

```csharp
double[] x = Enumerable.Range(0, 20).Select(i => i * 0.5 - 5.0).ToArray();
double[] y = Enumerable.Range(0, 20).Select(i => i * 0.5 - 5.0).ToArray();
var z = new double[20, 20];
for (int r = 0; r < 20; r++)
    for (int c = 0; c < 20; c++)
        z[r, c] = Math.Sin(x[c]) * Math.Cos(y[r]);

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Contour with Labels")
        .Contour(x, y, z, s =>
        {
            s.ShowLabels = true;
            s.LabelFormat = "F2";
            s.LabelFontSize = 9;
        })
        .WithColorMap("coolwarm"))
    .Save("contour_labels.svg");
```

![Contour with labels](../images/contour_labels.png)

Every line is drawn first and every label after them, as matplotlib's `clabel` does, so no line runs across a label. A
label stands at the middle of its line's length, turned along the line and never upside down (it follows an inverted Y
axis too), in the line's colour on a box of the theme's background. A line shorter than 1.2 times its label carries none.
The level is written the same way on every machine, with a point for the decimals whatever the culture.

> **A colour map name is checked.** `AxesBuilder.WithColorMap(string)` throws `ArgumentException` when the name is unknown, for example the typo `"virdis"`, and the exception lists the registered names, so a typo never falls back to a default map in silence. If you want a lookup that does not throw, fetch the instance yourself with `ColorMapRegistry.Get(name)` and call the `WithColorMap(IColorMap)` overload only when the result is not null.

## Filled contour (contourf)

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Filled Contour")
        .Contourf(x, y, z, s =>
        {
            s.Levels = 15;
            s.ColorMap = ColorMaps.Viridis;
        })
        .WithColorBar(cb => cb with { Label = "Amplitude" }))
    .Save("contourf.svg");
```

## Custom contour levels

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Contour(x, y, z, s =>
        {
            // Explicit level values instead of auto count
            s.LevelValues = [-0.8, -0.4, 0, 0.4, 0.8];
            s.ShowLabels = true;
            s.LabelFormat = "F1";
        })
        .WithColorMap("RdBu"))
    .Save("contour_custom_levels.svg");
```

The levels you name are the levels drawn, ascending and each once; `Levels` is ignored while `LevelValues` holds a
value. A level outside the range of the grid crosses no cell, so it draws no line, and a level that is not a number
is left out. A filled contour (`Contourf`) takes `LevelValues` too: it fills one band between each pair of
neighbouring levels and runs its colour map and colour bar from the first level to the last, whatever range the
grid spans. Its lowest band is painted over the whole grid, so where the grid's values lie below the first level you name,
the grid shows that band's colour; matplotlib leaves that area empty unless asked to extend the colours. Outside the grid
nothing is painted.

A filled contour (`Contourf`, and a `Contour` with `Filled` set) fills the bands over the points of the grid by one rule, with
the level lines as the edges of the bands. The fill follows the axes' range and the limits you set, so a grid cut to a window
by `SetXLim` and `SetYLim` shows the same bands over that window, and a cell with a value that cannot be drawn is left empty.

## Contour with level count

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Contour(x, y, z, s =>
        {
            s.Levels = 20;           // 20 contour lines
            s.ShowLabels = false;    // no inline labels
        })
        .WithColorMap("plasma")
        .WithColorBar())
    .Save("contour_dense.svg");
```

## Contour + filled contour overlay

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Filled + Line Contour")
        .Contourf(x, y, z, s => { s.Levels = 12; s.ColorMap = ColorMaps.Coolwarm; })
        .Contour(x, y, z, s =>
        {
            s.Levels = 12;
            s.ShowLabels = true;
            s.LabelFormat = "F2";
        })
        .WithColorBar())
    .Save("contour_overlay.svg");
```

## Triangulated contour

Use a triangulated contour for irregularly spaced data. The points are connected by Delaunay triangulation:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Tricontour(xIrregular, yIrregular, zIrregular)
        .WithColorMap("viridis")
        .WithColorBar())
    .Save("tricontour.svg");
```

## The mesh of a triangulation (triplot)

`Triplot` draws the edges of the triangles a triangulated contour or a triangle plot is built on. Give the triangles as a
flat list of point indices, three for each, or leave them out and the points are triangulated by the rule `Tripcolor` uses,
so the two draw one mesh. An edge two triangles share is drawn once, and all the edges are one line series:

```csharp
double[] x = [0, 4, 8, 2, 6, 10, 5];
double[] y = [0, 1, 0, 3, 3.5, 3, 6];

Plt.Create()
    .WithTitle("Sensor mesh")
    .AddSubPlot(1, 1, 1, ax => ax
        .Triplot(x, y, configure: line => line.Label = "mesh")
        .Scatter(x, y)
        .SetXLabel("East (m)")
        .SetYLabel("North (m)")
        .WithLegend())
    .Save("mesh_sensors.svg");
```

![The Delaunay mesh of seven sensor positions, with the sensors as points](../images/mesh_sensors.png)

The edges are drawn at the coordinates you gave. A point that is missing or infinite takes every edge that runs to it out
whole; a list that is not whole triangles, or an index that names no point, is refused.

## Fluent API reference — ContourSeries

| Property | Type | Default | Description |
|---|---|---|---|
| `Levels` | `int` | `10` | Number of contour levels |
| `LevelValues` | `double[]` | none | The levels to draw, ascending, each once; `Levels` is ignored when set |
| `Filled` | `bool` | `false` | Filled areas (contourf) instead of lines only |
| `ShowLabels` | `bool` | `false` | Show level values on contour lines |
| `LabelFormat` | `string` | `"G4"` | .NET format string for labels |
| `LabelFontSize` | `double` | `10` | Label font size |
| `ColorMap` | `IColorMap` | auto | Colormap for level colors |
