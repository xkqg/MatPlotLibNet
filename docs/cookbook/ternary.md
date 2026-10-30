---
title: "Ternary plots and Piper diagrams in C#"
description: "Draw a ternary plot in C#: three parts that make a whole on triangular axes, such as soil texture, and a Piper diagram of the major ions of a set of waters."
---

# Ternary plots and Piper diagrams

> **Install:** `dotnet add package MatPlotLibNet`

A ternary plot places three parts that make a whole, such as the clay, silt and sand of a soil, in a triangle. A Piper
diagram is the water chemist's version: two triangles and a diamond.

## A ternary plot of soil samples

`TernaryScatter(top, left, right)` puts a marker on each composition and `TernaryPlot(top, left, right)` joins them with a line.
The corners are named by the labels the axes already has: `SetYLabel` at the apex, `SetZLabel` at the bottom left and `SetXLabel`
at the bottom right.

```csharp
using MatPlotLibNet;

double[] clay = [0.55, 0.30, 0.12, 0.20, 0.40, 0.08, 0.28, 0.65];
double[] silt = [0.25, 0.40, 0.18, 0.60, 0.15, 0.12, 0.22, 0.15];
double[] sand = [0.20, 0.30, 0.70, 0.20, 0.45, 0.80, 0.50, 0.20];

Plt.Create()
    .WithTitle("Soil samples by texture")
    .WithSize(620, 560)
    .TightLayout()
    .AddSubPlot(1, 1, 1, ax => ax
        .SetYLabel("Clay")
        .SetZLabel("Silt")
        .SetXLabel("Sand")
        .TernaryScatter(clay, silt, sand))
    .Save("soil_ternary.svg");
```

![Soil samples in a triangle with Clay at the top, Silt at the bottom left and Sand at the bottom right](../images/soil_ternary.png)

The ticks run counter-clockwise: the right part along the base, the top part up the right edge, the left part down the left edge,
and the grid stands at every fifth. The amounts can be counts, percentages or masses, because they are closed to fractions only to
be placed, so (2, 5, 3) and (20, 50, 30) stand at the same place and the table beside the chart shows what you typed. A sample with
a negative, infinite or missing part, or with three zeros, is refused by its row and its part.

The triangle is a fixed frame: a wheel, a drag or Home over it leaves the chart as it is, because a composition cannot lie outside
it. Hovering a point names its composition ("Clay 20 %, Silt 50 %, Sand 30 %").

## A Piper diagram

`Piper(samples)` takes what a laboratory delivers, a `WaterSample` in mg/L for each water, and draws its cations (calcium,
magnesium, sodium with potassium) on one triangle, its anions (bicarbonate with carbonate, sulphate, chloride) on a second and
both on a diamond between and above them. A water has one colour on all three panels, so a reader follows it from panel to panel.

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models.Series;

WaterSample[] waters =
[
    new() { Label = "Mountain spring", Ca = 60, Mg = 10, Na = 8, K = 2, HCO3 = 200, SO4 = 20, Cl = 10 },
    new() { Label = "Brine well", Ca = 400, Mg = 100, Na = 5000, K = 50, HCO3 = 150, SO4 = 300, Cl = 8000 },
    new() { Label = "Sodium spring", Ca = 10, Mg = 3, Na = 300, K = 5, HCO3 = 600, SO4 = 30, Cl = 40 },
];

Plt.Create()
    .WithTitle("Three waters on a Piper diagram")
    .WithSize(820, 560)
    .TightLayout()
    .AddSubPlot(1, 1, 1, ax => ax.Piper(waters))
    .Save("piper_water.svg");
```

![Two triangles and a diamond with a marker for each of three waters](../images/piper_water.png)

The percentages are of milliequivalents. The table gives the six percentages, the charge balance error and the word "balanced" or
"unbalanced" for each water: a water whose charges do not balance, beyond 5 %, is flagged and not dropped. A water with an amount
that is negative, infinite or missing, or with no cations or no anions, is refused by its row and its ion.
