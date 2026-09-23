# MatPlotLibNet for Verso

Charts inside a [Verso](https://www.versonotebooks.com/) notebook cell.

Install this from Verso's Extensions panel, then return a figure from a C# cell:

```csharp
#r "nuget: MatPlotLibNet"

using MatPlotLibNet;

Plt.Create()
   .WithTitle("Revenue")
   .AddSubPlot(1, 1, 1, ax => ax
       .Plot([1, 2, 3, 4], [12, 18, 15, 21], s => s.Label = "2026")
       .WithLegend())
```

That is the whole cell. There is nothing to import from this package and nothing to call: the chart is the
cell's last expression, and it draws inline as SVG. A figure works, and so does the builder that makes one —
you do not have to remember `.Build()`.

The picture is drawn by [MatPlotLibNet](https://github.com/xkqg/MatPlotLibNet), a charting library for C# with
83 chart types and 148 colormaps, so anything it can draw is a cell away: line, scatter, bar, histogram,
candlestick, heatmap, 3-D surfaces, maps.

## What it does with the space it is given

The chart fills the width of the cell and keeps its proportions. If a figure is taller than the cell can show,
it gets a scroll box rather than being squeezed — a chart drawn at a size nobody chose is worse than one you
scroll.

## If something cannot be drawn

The cell says so in words, with the message from the renderer itself, instead of a stack trace from inside the
charting library.

MIT licensed.
