---
title: "Charts in a Verso notebook (C#)"
description: "Draw a chart in a Verso notebook cell. Return a figure from C# and it renders inline as SVG — line, bar, candlestick, heatmap, 3D, maps."
---

# Verso notebooks

> **Install:** the extension comes from Verso's Extensions panel — search for `MatPlotLibNet.Verso`. The charting
> library itself is an ordinary package: `dotnet add package MatPlotLibNet` in a project, or `#r "nuget:
> MatPlotLibNet"` at the top of a cell.

[Verso](https://www.versonotebooks.com/) is an open-source notebook for C#. This extension teaches it to draw this
library's charts: a cell that ends on a figure shows the chart instead of a line of type names.

## The whole cell

```csharp
#r "nuget: MatPlotLibNet"

using MatPlotLibNet;

Plt.Create()
   .WithTitle("Revenue")
   .AddSubPlot(1, 1, 1, ax => ax
       .SetXLabel("Quarter")
       .SetYLabel("€M")
       .Plot([1, 2, 3, 4], [12, 18, 15, 22], s => s.Label = "2026")
       .WithLegend())
```

That is all of it. There is nothing to import from the extension and nothing to call — the chart is the cell's last
expression, and it draws inline as SVG.

There is no `.Build()` in that cell either, on purpose. `Plt.Create()` hands back a builder, and a notebook shows
you whatever the cell ended on, so the builder is what usually arrives. It is drawn like a figure. `Plt.Mosaic()`
is too, and so is a `Figure` you built earlier and kept in a variable:

```csharp
var figure = Plt.Create()
                .WithTitle("Revenue, once more")
                .Plot([1, 2, 3, 4], [12, 18, 15, 22])
                .Build();

figure
```

## Every chart this library draws

The extension does not draw anything itself — it hands the library's own SVG to the notebook. So a cell can show
any of the 83 chart types, with any of the 148 colormaps, drawn exactly as they are on a server or in a file:

```csharp
#r "nuget: MatPlotLibNet"

using MatPlotLibNet;
using MatPlotLibNet.Styling;

string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"];
double[] inbound = [120, 145, 138, 170, 162, 191];

Plt.Create()
   .WithTheme(Theme.Dark)
   .WithTitle("Inbound volume")
   .Bar(months, inbound, s => s.Color = Color.FromName("steelblue"))
```

Candlesticks with indicators, heatmaps, Sankey diagrams, 3-D surfaces and map projections all work the same way:
build it, end the cell on it.

## How big the picture gets

The chart fills the width of the cell and keeps its proportions — the SVG carries a `viewBox`, so there is nothing
to set.

Height is the one case that needs a decision. A figure taller than the cell can show gets a scroll box; it is not
resized. Rewriting the height would re-run the layout, and a chart drawn at a size nobody chose is worse than one
you scroll — twelve rows of labels squeezed into the space for four.

```csharp
Plt.Create()
   .WithSize(800, 2400)          // twelve stacked panels: taller than any cell
   .Plot([1, 2, 3], [4, 5, 6])
```

## Asking for the picture on its own

A cell that asks for `image/svg+xml` gets the SVG bare, with no HTML around it — useful when you are going to save
it or pass it on. The default, `text/html`, is the one that keeps the links, the accessible name and the
responsive width.

## When a chart cannot be drawn

The cell says so in words, with the message from the renderer itself. A mosaic whose rows are not the same
length, for instance:

```
MatPlotLibNet could not draw this chart. All rows must have the same length. Row 0 has 2 cells; row 1 has 3. (Parameter 'pattern')
```

Not a stack trace from inside the charting library. If a cell hands over something that is not a chart at all, the
extension says that too, and names what it got.

## Notebooks, and the other one

`MatPlotLibNet.Notebooks` draws the same charts in Polyglot Notebooks and Jupyter. Microsoft has ended the runtime
underneath those, so that package stands still at the version it reached, existing notebooks keep working, and new
notebook work belongs here — Verso is the open-source notebook built to replace Polyglot.

If what you want is a chart outside a notebook that keeps updating while your program runs, that is
[`MatPlotLibNet.Interactive`](streaming.md): it opens a browser window from any program and pushes new data into
the chart.

## See also

- [From matplotlib to C#](matplotlib-to-csharp.md) — the pyplot call you know, and the line that draws it here
- [Accessibility](accessibility.md) — the data table beside the chart, which a notebook can print as markdown
- [Themes](themes.md) — a dark theme for a dark notebook
