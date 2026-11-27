---
title: "Subplots and GridSpec layouts in C#"
description: "Lay out several charts in one figure from C# with subplots and GridSpec, including shared axes and spanning cells."
---

# Subplots & GridSpec

> **Install:** `dotnet add package MatPlotLibNet`

## Multi-subplot dashboard

```csharp
string[] categories = ["Q1", "Q2", "Q3", "Q4", "Q5"];
double[] values = [23, 45, 12, 67, 34];
double[] histData = [1.2, 2.3, 2.1, 3.4, 3.5, 3.6, 4.1, 4.8, 5.2, 5.5, 6.1, 6.3];

Plt.Create()
    .WithTitle("Dashboard")
    .WithTheme(Theme.Dark)
    .Bar(categories, values, bar => { bar.Color = Colors.Orange; bar.Label = "Units sold"; })
    .AddSubPlot(1, 2, 2, ax => ax.Hist(histData, 6))
    .Save("dashboard.svg");
```

![Dashboard](../images/dashboard.png)

## GridSpec with ratio control

Use `WithGridSpec` to control row and column proportions:

```csharp
Plt.Create()
    .WithGridSpec(2, 2, heightRatios: [2.0, 1.0], widthRatios: [3.0, 1.0])
    .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot(x, y).WithTitle("Main plot"))
    .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Scatter(x, y).WithTitle("Scatter"))
    .AddSubPlot(new GridPosition(1, 2, 0, 2), ax => ax.Bar(categories, catValues).WithTitle("Wide bar"))
    .TightLayout()
    .Save("gridspec_layout.svg");
```

![GridSpec](../images/gridspec_layout.png)

## Mosaic layout (string-based)

Define the subplot layout with a string pattern. Each letter is a panel:

```csharp
Plt.Mosaic("AAB\nCCB")
    .Panel("A", ax => ax.Plot(x, y).WithTitle("Main chart"))
    .Panel("B", ax => ax.Hist(data, 20).WithTitle("Distribution"))
    .Panel("C", ax => ax.Bar(categories, values).WithTitle("Categories"))
    .Configure(fig => fig.TightLayout())
    .Save("mosaic.svg");
```

## Spanning subplot

A single subplot can span multiple grid cells:

```csharp
Plt.Create()
    .WithGridSpec(2, 3)
    // Top row: one wide chart spanning 3 columns
    .AddSubPlot(new GridPosition(0, 1, 0, 3), ax => ax
        .Plot(x, y, s => s.Label = "Full width")
        .WithTitle("Top — spans 3 columns")
        .WithLegend())
    // Bottom row: 3 individual charts
    .AddSubPlot(GridPosition.Single(1, 0), ax => ax.Bar(["A", "B"], [10, 20]))
    .AddSubPlot(GridPosition.Single(1, 1), ax => ax.Pie([60, 40], ["Yes", "No"]))
    .AddSubPlot(GridPosition.Single(1, 2), ax => ax.Scatter(x, y))
    .TightLayout()
    .Save("spanning.svg");
```

## Subplot spacing

```csharp
Plt.Create()
    // Margins as fractions of the figure, the way matplotlib's subplot params read. The gaps between
    // panels are pixels, so they stay the same whatever size the figure is.
    .WithSubPlotSpacing(_ => SubPlotSpacing.FromFractions(
        left: 0.10, right: 0.05, top: 0.10, bottom: 0.10,
        horizontalGap: 30, verticalGap: 40))
    .AddSubPlot(2, 2, 1, ax => ax.Plot(x, y1).WithTitle("A"))
    .AddSubPlot(2, 2, 2, ax => ax.Plot(x, y2).WithTitle("B"))
    .AddSubPlot(2, 2, 3, ax => ax.Plot(x, y3).WithTitle("C"))
    .AddSubPlot(2, 2, 4, ax => ax.Plot(x, y4).WithTitle("D"))
    .Save("spacing.svg");
```

## Inset axes

`AddInset` adds a nested axes inside a parent subplot:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => s.Label = "Full data")
        .AddInset(0.55, 0.55, 0.4, 0.4, inset => inset
            .Plot(xDetail, yDetail, s => s.Color = Colors.Red)
            .WithTitle("Detail"))
        .WithLegend())
    .Save("inset.svg");
```

The callback gets a builder over the inset itself, so what it sets is what the inset has: its own legend, tick locators and
formatters, scales and overlays. An inset can hold an inset, and its grid is the theme's grid of the figure it is drawn under.

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

double[] signalX = [.. Enumerable.Range(0, 50).Select(i => i / 5.0)];
double[] signalY = [.. signalX.Select(v => Math.Sin(3 * v) * Math.Exp(-v / 4))];
double[] zoomX = [.. signalX.Where(v => v <= 1.5)];
double[] zoomY = [.. zoomX.Select(v => Math.Sin(3 * v) * Math.Exp(-v / 4))];

Plt.Create()
    .WithTitle("An inset that keeps its legend")
    .WithSize(750, 450)
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(signalX, signalY, s => s.Label = "Full signal")
        .WithLegend(LegendPosition.LowerLeft)
        .AddInset(0.5, 0.45, 0.45, 0.45, inset => inset
            .Plot(zoomX, zoomY, s => { s.Label = "Detail"; s.Color = Colors.Crimson; })
            .WithLegend()))
    .Save("inset_legend.svg");
```

![A damped sine wave with the legend "Full signal" at the lower left and an inset of its first 1.5 units, whose own legend reads "Detail"](../images/inset_legend.png)

The four numbers are fractions of the parent's plot: the inset's left edge, its top edge counted down from the parent's top,
its width and its height. matplotlib's `inset_axes` counts its y up from the bottom instead, so (0.55, 0.55) stands lower
here than there.

## Zoom indicator

`IndicateZoom`, called on the inset's own builder, has the parent mark the region the inset shows (matplotlib's
`indicate_inset_zoom`): a rectangle over the limits the inset drew, the ones you set or the ones it scaled to its data, and
the two lines from it to the inset's corners that matplotlib's rule keeps, so they do not cross the inset.

```csharp
double[] waveX = [.. Enumerable.Range(0, 200).Select(i => i / 20.0)];
double[] waveY = [.. waveX.Select(v => Math.Sin(v) + 0.3 * Math.Sin(5 * v))];

Plt.Create()
    .WithTitle("A closer look at the wiggle")
    .WithSize(760, 440)
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(waveX, waveY)
        .SetYLim(-1.5, 3.5)
        .AddInset(0.5, 0.03, 0.45, 0.35, inset => inset
            .Plot(waveX, waveY)
            .SetXLim(6, 7.5)
            .SetYLim(-0.7, 1)
            .IndicateZoom()))
    .Save("inset_zoom.svg");
```

![A wave with an inset of the stretch from 6 to 7.5, its region marked by a grey rectangle on the parent and two lines to the inset](../images/inset_zoom.png)

The indicator is matplotlib's: the theme's neutral grey at half opacity, one pixel wide, solid, with connectors; the callback
of `IndicateZoom(zoom => …)` sets its `Color`, `Alpha`, `LineWidth`, `LineStyle` and `Connectors`. An inset that stands
inside its region, or a region inside its inset, draws no connector. A Cartesian, ternary or polar parent draws an
indicator (on a polar parent the region's corners are placed by angle and radius and joined straight, as matplotlib joins
them); a 3-D parent draws none, as matplotlib places that rectangle in the flat view of the 3-D axes and not in its data. The
indicator is not cut off at the parent's plot: an inset that reaches zero or below on a log parent has its region's side a
thousand decades below the axis, where matplotlib's log scale puts it, so the rectangle runs off the figure as in matplotlib. It
is drawn over the parent's marks and under the inset, as matplotlib draws it, so the inset covers the part of the region it
overlaps. Called on a subplot that is not an inset, `IndicateZoom` throws an
`InvalidOperationException`. The indicator travels through JSON as the inset's `zoomIndicator`.


## Shared axes

`ShareX` links the axis ranges of the subplots that pass the same key:

```csharp
Plt.Create()
    .AddSubPlot(2, 1, 1, ax => ax
        .Plot(x, y1).WithTitle("Sensor A").ShareX("group1"))
    .AddSubPlot(2, 1, 2, ax => ax
        .Plot(x, y2).WithTitle("Sensor B").ShareX("group1"))
    .TightLayout()
    .Save("shared_axes.svg");
```

## Text, a legend and an image on the figure

`FigText`, `FigLegend` and `FigImage` put something on the figure itself rather than in a subplot (matplotlib's `figtext`,
`figlegend` and `figimage`). A text stands at fractions of the figure counted from its bottom-left corner. The figure's
legend lists the labelled series and [patches](patches.md) of every subplot, in subplot order and in the order they were
added. Under the matplotlib themes it lists every one, as `fig.legend()` does, so a line is named once to be listed once: the
example below names the lines of the first panel only. Under the library's own themes an entry drawn exactly like an earlier
one is listed once. An image is one pixel for each value,
at its native size, its offsets in pixels from the bottom-left corner:

```csharp
using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

double[] month = [1, 2, 3, 4, 5, 6];
double[] northOrders = [12, 15, 14, 18, 21, 24];
double[] southOrders = [9, 11, 15, 14, 17, 19];
double[] northRevenue = [30, 36, 33, 41, 47, 52];
double[] southRevenue = [22, 27, 35, 33, 38, 44];
var strip = new double[10, 140];
for (int row = 0; row < 10; row++)
    for (int column = 0; column < 140; column++)
        strip[row, column] = column;

Plt.Create()
    .WithTitle("Orders and revenue by region")
    .WithSize(860, 480)
    .AddSubPlot(1, 2, 1, ax => ax
        .WithTitle("Orders")
        .WithLegend(visible: false)
        .Plot(month, northOrders, s => s.Label = "North")
        .Plot(month, southOrders, s => s.Label = "South")
        .SetXLabel("Month"))
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle("Revenue")
        .WithLegend(visible: false)
        .Plot(month, northRevenue)
        .Plot(month, southRevenue)
        .SetXLabel("Month"))
    .FigLegend(legend => legend with { Position = LegendPosition.UpperRight })
    .FigText("Source: field survey", 0.01, 0.01)
    .FigText("Preliminary", 0.01, 0.97, text =>
    {
        text.VerticalAlignment = TextVerticalAlignment.Top;
        text.Color = Colors.Red;
    })
    .FigImage(strip, 700, 8)
    .Save("figure_overlays.svg");
```

![Two line charts with one legend for both at the upper right of the figure, a source line at the bottom left, a red note at the top left and a gradient strip at the bottom right](../images/figure_overlays.png)

The images are drawn under every subplot; the figure's colour bar, then its texts, then its legends over them, in the SVG,
the PNG, the PDF and the native controls alike, and on a figure that has no subplot at all. A text or an image placed off
the figure draws nothing. A figure legend is static (clicking an entry hides nothing, as in matplotlib), and
`LegendPosition.Best` is the upper right on a figure. Each `FigLegend` adds a legend, as each `fig.legend()` does, so a
figure can carry one for each group of series. The three travel through JSON as the figure's `texts`, `legends` and
`images`, and the [data table](accessibility.md#the-data-table) lists the texts and says what each image is.

A legend at an outside place (`OutsideRight`, `OutsideLeft`, `OutsideTop`, `OutsideBottom`) stands at the middle of that
edge of the figure, and `ConstrainedLayout` narrows the subplots to keep room for it, as matplotlib's constrained layout does
for `fig.legend(loc='outside right upper')`. A legend at any other place takes no room, and neither the tight nor the default
layout keeps room for a figure legend, as in matplotlib: leave a margin with `WithSubPlotSpacing`, or stand it in a corner
the charts leave free.

```csharp
using MatPlotLibNet.Models;

double[] month = [1, 2, 3, 4, 5, 6];
double[] observed = [12, 15, 14, 18, 21, 24];
double[] fitted = [12.5, 14.2, 15.6, 17.9, 20.8, 23.6];

Plt.Create()
    .WithSize(860, 400)
    .ConstrainedLayout()
    .AddSubPlot(1, 2, 1, ax => ax
        .WithLegend(visible: false)
        .Plot(month, observed, s => s.Label = "Observed")
        .Plot(month, fitted, s => s.Label = "Fitted"))
    .AddSubPlot(1, 2, 2, ax => ax
        .WithLegend(visible: false)
        .Plot(month, observed)
        .Plot(month, fitted))
    .FigLegend(legend => legend with { Position = LegendPosition.OutsideRight })
    .Save("figure_legend_outside.svg");
```

## Tight layout vs constrained layout

```csharp
// TightLayout: simple margin adjustment
Plt.Create()
    .AddSubPlot(2, 2, 1, ax => ax.Plot(x, y))
    .AddSubPlot(2, 2, 2, ax => ax.Plot(x, y))
    .TightLayout()
    .Save("tight.svg");

// ConstrainedLayout: advanced, handles tick labels + colorbars
Plt.Create()
    .AddSubPlot(2, 2, 1, ax => ax.Heatmap(matrix).WithColorBar())
    .AddSubPlot(2, 2, 2, ax => ax.Plot(x, y))
    .ConstrainedLayout()
    .Save("constrained.svg");
```

`TightLayout(rect)` fits the subplots, their labels and the figure's title into a rectangle in fractions of the figure, as
matplotlib's `tight_layout(rect=(left, bottom, right, top))` does, and keeps the rest of the figure free on top of what the
layout needs. Under the matplotlib themes it keeps matplotlib's pad, 1.08 times the font size, at every edge and once between two
subplots, past what the axes draw there; an edge where nothing is drawn keeps no pad, so a gap you declare narrower than the pad stays. `WithTitle(title, style => ...)` gives the figure's title a size, weight, family, slant, colour and extra room
below it, as `fig.suptitle(title, fontsize=14, fontweight='bold')` does, and the layout reserves the title in that style:

```csharp
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

double[] t = [0, 10, 20, 30, 40, 50];
double[] decay = [1.0, 0.45, 0.2, 0.09, 0.04, 0.02];
double[] noise = [0.1, -0.2, 0.25, -0.05, 0.15, -0.1];

Plt.Create()
    .WithTitle("Decay and noise", style => style with { FontSize = 14 * 100.0 / 72, FontWeight = FontWeight.Bold })
    .AddSubPlot(1, 2, 1, ax => ax.WithTitle("Decay").Plot(t, decay))
    .AddSubPlot(1, 2, 2, ax => ax.WithTitle("Noise").Plot(t, noise))
    .TightLayout(new LayoutRect(0, 0, 1, 0.93))
    .Save("title_and_rect.svg");
```

## Fluent API reference

| Method | Description |
|---|---|
| `.AddSubPlot(rows, cols, index, cfg)` | Grid-based subplot |
| `.AddSubPlot(GridPosition, cfg)` | GridSpec-based subplot (span rows/cols) |
| `.WithGridSpec(rows, cols, heightRatios?, widthRatios?)` | Row/column size ratios |
| `Plt.Mosaic(pattern)` | String-based layout (returns MosaicFigureBuilder) |
| `.Panel(char, cfg)` | Configure mosaic panel by letter |
| `.AddInset(x, y, w, h, cfg)` | Nested axes (fractional coordinates; y is the top edge, counted down) |
| `.IndicateZoom(configure?)` | On an inset's builder: the parent marks the region the inset shows |
| `.FigText(text, x, y, configure?)` | Text on the figure, at fractions from its bottom-left corner |
| `.FigLegend(configure?)` | Adds a legend for the labelled series of every subplot |
| `.FigImage(data, xOffset, yOffset, configure?)` | An image on the figure, one pixel per value, offsets in pixels |
| `.ShareX(key)` / `.ShareY(key)` | Link axis ranges between subplots |
| `.TightLayout()` | Auto-adjust margins |
| `.TightLayout(rect)` | Auto-adjust margins and keep the figure outside `rect` (fractions, matplotlib's order) free |
| `.WithTitle(title, style)` | The figure's title in a style of its own, reserved in that style |
| `.ConstrainedLayout()` | Advanced margin adjustment |
| `.WithSubPlotSpacing(cfg)` | Manual margin/gap control |

## Scatter with marginal histograms

Use `WithGridSpec` and `GridPosition` to lay out the scatter in the large cell, plus two marginal histograms.

```csharp
var rng = new Random(17);
double BM(Random r) { double u1 = 1 - r.NextDouble(), u2 = 1 - r.NextDouble(); return Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2); }
double[] xs = Enumerable.Range(0, 200).Select(_ => BM(rng)).ToArray();
double[] ys = xs.Select(x => 0.6 * x + BM(rng) * 0.8).ToArray();

Plt.Create()
    .WithTitle("Scatter with Marginals")
    .WithSize(800, 700)
    .WithGridSpec(2, 2, heightRatios: [3.0, 1.0], widthRatios: [3.0, 1.0])
    .AddSubPlot(new GridPosition(0, 1, 0, 1), ax => ax
        .Scatter(xs, ys, s => { s.Color = Color.FromHex("#2C3E50"); s.Alpha = 0.5; s.MarkerSize = 5; })
        .SetXLabel("X").SetYLabel("Y"))
    .AddSubPlot(new GridPosition(0, 1, 1, 2), ax => ax
        .Hist(ys, bins: 20, s => { s.Color = Color.FromHex("#E74C3C"); })
        .SetXLabel("Count"))
    .AddSubPlot(new GridPosition(1, 2, 0, 1), ax => ax
        .Hist(xs, bins: 20, s => { s.Color = Color.FromHex("#3498DB"); })
        .SetYLabel("Count"))
    .Save("scatter_marginals.svg");
```

`GridPosition(rowStart, rowEnd, colStart, colEnd)` uses zero-based exclusive indices.

![Scatter with marginals](../images/scatter_marginals.png)
