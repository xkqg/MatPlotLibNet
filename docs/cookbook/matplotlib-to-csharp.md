---
title: "matplotlib in C#: the pyplot call and the line that replaces it"
description: "A translation table from Python's matplotlib to C#. plt.plot, plt.subplots, plt.imshow, savefig and the rest, each with the MatPlotLibNet line that draws the same chart in .NET 10 and .NET 8."
---

# matplotlib in C#

> **Install:** `dotnet add package MatPlotLibNet` — add `MatPlotLibNet.Skia` as well for PNG, PDF and GIF.

If you know matplotlib and you are now writing C#, you do not need to learn a new charting library from
scratch. This library was built around matplotlib's own model — a figure holds axes, an axes holds series,
a series carries its own style — so most of what you know transfers name for name. This page is the
lookup table: the pyplot call on the left, the line that draws the same chart on the right.

Two differences are worth reading before the tables, because they explain almost every row.

**There is no global current figure.** In pyplot, `plt.plot(...)` draws on whichever figure is current, and
`plt.gca()` hands you the axes behind it. Here you always hold the object you are drawing on, so there is
nothing to get and nothing to close. That removes `plt.gca`, `plt.gcf`, `plt.cla`, `plt.clf`, `plt.close`
and `plt.hold` from the table: none of them has an equivalent, because none of them has anything to do.

**Calls chain.** Every verb returns the thing you called it on, so a chart is one expression instead of a
paragraph of statements. That is why the C# column reads `ax.Plot(...).WithTitle(...)` where Python reads
two lines.

## The same chart, twice

```python
# Python
import matplotlib.pyplot as plt

fig, ax = plt.subplots(figsize=(8, 6))
ax.plot(x, y, color='steelblue', linewidth=2, label='Signal')
ax.set_xlabel('Time (s)')
ax.set_ylabel('Amplitude')
ax.set_title('Measurement')
ax.legend()
ax.grid(True)
fig.savefig('chart.png', dpi=150)
```

```csharp
// C#
using MatPlotLibNet;
using MatPlotLibNet.Styling;

Plt.Create()
    .WithSize(800, 600)
    .WithDpi(150)
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => { s.Color = Colors.SteelBlue; s.LineWidth = 2; s.Label = "Signal"; })
        .SetXLabel("Time (s)")
        .SetYLabel("Amplitude")
        .WithTitle("Measurement")
        .WithLegend()
        .WithGrid(g => g with { Visible = true }))
    .Save("chart.png");
```

## Figures and axes

| matplotlib | MatPlotLibNet | Notes |
|---|---|---|
| `plt.figure()` | `Plt.Create()` | Starts a figure. `.Build()` hands you the `Figure`; `.Save(…)` finishes it in one go. |
| `plt.subplots()` | `figure.AddSubPlot(1, 1, 1, ax => …)` | The axes is handed to the lambda instead of returned. |
| `plt.subplots(2, 3)` | `figure.AddSubPlot(2, 3, index, ax => …)` | Called once per panel; `index` runs 1…6, row by row, as in matplotlib. |
| `plt.figure(figsize=(8, 6))` | `figure.WithSize(800, 600)` | Pixels, not inches — there is no implicit DPI multiply. |
| `plt.figure(dpi=150)` | `figure.WithDpi(150)` | Scales the raster export; the SVG stays resolution-free. |
| `fig.suptitle('…')` | `figure.WithTitle("…")` | The figure's own title, above the panels. |
| `plt.tight_layout()` | `figure.TightLayout()` | |
| `plt.subplots(constrained_layout=True)` | `figure.ConstrainedLayout()` | |
| `gridspec.GridSpec(2, 2, height_ratios=…)` | `figure.WithGridSpec(2, 2, heightRatios)` | Ratios are arrays, same meaning. |
| `plt.subplots_adjust(hspace=…)` | `figure.WithSubPlotSpacing(sp => sp with { VerticalGap = … })` | |
| `plt.subplot_mosaic('AB;CC')` | `Plt.Mosaic("AB;CC")` | Same string layout language. |
| `fig.add_axes([…])` | `ax.AddInset(bounds, inset => …)` | An inset belongs to the axes it sits in. |
| `plt.savefig('c.png')` | `figure.Save("c.png")` | The extension picks the format: `.svg`, `.png`, `.pdf`, `.gif`. |
| `fig.canvas.tostring_rgb()` | `figure.ToSvg()` | The markup as a string, for a web response. |
| `plt.show()` | — | Nothing renders to a screen by itself; see the [Playground](https://xkqg.github.io/MatPlotLibNet/playground/) or the Blazor, WPF, MAUI, Avalonia and Uno controls. |
| `plt.gca()`, `plt.gcf()` | — | There is no current figure to get. |
| `plt.close()`, `plt.clf()` | — | Nothing is held open. |

## The plots

| matplotlib | MatPlotLibNet | Notes |
|---|---|---|
| `ax.plot(x, y)` | `ax.Plot(x, y)` | |
| `ax.scatter(x, y)` | `ax.Scatter(x, y)` | |
| `ax.scatter(x, y, c=density)` | `ax.DensityScatter(x, y)` | Counts the points per cell and colours by it; no density array to compute. |
| `ax.bar(labels, heights)` | `ax.Bar(labels, heights)` | |
| `ax.bar` with an offset per group | `ax.GroupedBar(categories, groups)` | The offset arithmetic every grouped bar chart repeats is done for you. |
| `ax.barh(labels, widths)` | `ax.Bar(labels, values, s => s.Orientation = BarOrientation.Horizontal)` | One series flag rather than a second verb. |
| `ax.step(x, y)` | `ax.Step(x, y)` | |
| `ax.stem(x, y)` | `ax.Stem(x, y)` | |
| `ax.stackplot(x, ys)` | `ax.StackPlot(x, ySets)` | |
| `ax.fill_between(x, y1, y2)` | `ax.FillBetween(x, y, y2)` | |
| `ax.errorbar(x, y, yerr=e)` | `ax.ErrorBar(x, y, errorLow, errorHigh)` | |
| `ax.pie(sizes, labels=…)` | `ax.Pie(sizes, labels)` | |
| donut via `wedgeprops` | `ax.Donut(sizes, labels)` | |
| `ax.eventplot(positions)` | `ax.Eventplot(positions)` | |
| `ax.table(cellText=…)` | `ax.Table(cells)` | |
| — | `ax.Sankey(nodes, links)` | |
| `squarify.plot` | `ax.Treemap(root)` | matplotlib has no treemap; this one is built in. |
| nested `ax.pie` | `ax.Sunburst(root)` | |

## Labels, limits, scales and the frame

| matplotlib | MatPlotLibNet | Notes |
|---|---|---|
| `ax.set_xlabel('…')` | `ax.SetXLabel("…")` | |
| `ax.set_ylabel('…')` | `ax.SetYLabel("…")` | |
| `ax.set_title('…')` | `ax.WithTitle("…")` | |
| `ax.set_xlim(0, 10)` | `ax.SetXLim(0, 10)` | |
| `ax.set_ylim(0, 10)` | `ax.SetYLim(0, 10)` | |
| `ax.set_xscale('log')` | `ax.SetXScale(AxisScale.Log)` | `Linear`, `Log`, `SymLog` and `Logit`, as in matplotlib. |
| `ax.set_yscale('log')` | `ax.SetYScale(AxisScale.Log)` | |
| `ax.legend()` | `ax.WithLegend()` | |
| `ax.legend(loc='upper left')` | `ax.WithLegend(LegendPosition.UpperLeft)` | |
| `ax.grid(True)` | `ax.WithGrid(g => g with { Visible = true })` | The grid is a value you change, so one call sets colour, style and which axis at once. |
| `ax.minorticks_on()` | `ax.WithMinorTicks()` | |
| `ax.margins(x=0.1)` | `ax.SetXMargin(0.1)` | |
| `ax.margins(0)` | `ax.WithTightMargins()` | |
| `ax.spines['top'].set_visible(False)` | `ax.HideTopSpine()` | `HideRightSpine()` for the other one. |
| `ax.axhline(y=0)` | `ax.AxHLine(0)` | |
| `ax.axvline(x=0)` | `ax.AxVLine(0)` | |
| `ax.axhspan(1, 2)` | `ax.AxHSpan(1, 2)` | |
| `ax.axvspan(1, 2)` | `ax.AxVSpan(1, 2)` | |
| `ax.annotate('…', xy=…)` | `ax.Annotate("…", x, y)` | Arrows, callout boxes and axes-fraction placement are all on the same call. |
| `ax.twinx()` | `ax.WithSecondaryYAxis(right => …)` | |
| `ax.sharex(other)` | `ax.ShareX("time")` | |
| `plt.xticks(rotation=45)` | `ax.WithXTickLabelRotation(45)` | |
| `ax.xaxis.set_major_formatter(f)` | `ax.SetXTickFormatter(formatter)` | |
| `ax.xaxis.set_major_locator(l)` | `ax.SetXTickLocator(locator)` | |
| `mdates.DateFormatter('%H:%M')` | `ax.SetXDateFormat("HH:mm")` | .NET format strings, not strftime. |

## Colour and style

| matplotlib | MatPlotLibNet | Notes |
|---|---|---|
| `color='steelblue'` | `Colors.SteelBlue` | All 148 CSS4 names are there. |
| `color='#1f77b4'` | `Color.FromHex("#1f77b4")` | |
| `color='red'` | `Color.FromName("red")` | |
| `linestyle='--'` | `LineStyle.Dashed` | |
| `marker='o'` | `MarkerStyle.Circle` | |
| `cmap='viridis'` | `ColorMaps.Viridis` | 148 colormaps, matplotlib's own included. |
| `cmap='coolwarm'` | `ColorMaps.Coolwarm` | |
| `plt.colormaps()` | `ColorMaps.All` | |
| `plt.style.use('dark_background')` | `figure.WithTheme(Theme.Dark)` | 30 themes; `Theme.MatplotlibV2` is matplotlib's own default palette. |
| `plt.style.use('ggplot')` | `figure.WithTheme(Theme.Ggplot)` | |
| `plt.style.use('seaborn')` | `figure.WithTheme(Theme.Seaborn)` | |
| `mpl.rcParams[…] = …` | `Theme.CreateFrom(Theme.Default)` | Build a theme from an existing one instead of mutating a global dictionary. |
| `fig.colorbar(im)` | `ax.WithColorBar()` | |

## Distributions and statistics

| matplotlib / seaborn | MatPlotLibNet | Notes |
|---|---|---|
| `ax.hist(data, bins=20)` | `ax.Hist(data, 20)` | |
| `ax.hist2d(x, y)` | `ax.Histogram2D(x, y)` | |
| `ax.boxplot(data)` | `ax.BoxPlot(data)` | |
| `ax.violinplot(data)` | `ax.Violin(data)` | |
| `ax.hexbin(x, y)` | `ax.Hexbin(x, y)` | |
| `sns.kdeplot(data)` | `ax.Kde(data)` | |
| `sns.rugplot(data)` | `ax.Rugplot(data)` | |
| `sns.stripplot(…)` | `ax.Stripplot(datasets)` | |
| `sns.swarmplot(…)` | `ax.Swarmplot(datasets)` | |
| `sns.countplot(…)` | `ax.Countplot(values)` | |
| `sns.pointplot(…)` | `ax.Pointplot(datasets)` | |
| `ax.ecdf(data)` | `ax.Ecdf(data)` | |
| `sns.heatmap(matrix)` | `ax.Heatmap(matrix)` | |
| `sns.clustermap(matrix)` | `ax.Clustermap(matrix)` | |
| `sns.pairplot(frame)` | `ax.PairGrid(variables)` | |
| `scipy.cluster.hierarchy.dendrogram` | `ax.Dendrogram(root)` | |

## Images, grids and vector fields

| matplotlib | MatPlotLibNet | Notes |
|---|---|---|
| `ax.imshow(matrix)` | `ax.Image(matrix)` | |
| `ax.pcolormesh(x, y, z)` | `ax.Pcolormesh(xEdges, yEdges, values)` | |
| `ax.contour(x, y, z)` | `ax.Contour(x, y, z)` | |
| `ax.contourf(x, y, z)` | `ax.Contourf(x, y, z)` | |
| `ax.quiver(x, y, u, v)` | `ax.Quiver(x, y, u, v)` | |
| `ax.quiverkey(q, …)` | `ax.QuiverKey(x, y, u, "5 m/s")` | |
| `ax.streamplot(x, y, u, v)` | `ax.Streamplot(x, y, u, v)` | |
| `ax.barbs(x, y, u, v)` | `ax.Barbs(x, y, u, v)` | |
| `ax.tricontour(…)` | `ax.Tricontour(x, y, z)` | |
| `ax.tripcolor(…)` | `ax.Tripcolor(x, y, z)` | |
| `ax.specgram(signal)` | `ax.Spectrogram(signal)` | |

## Three dimensions

| matplotlib | MatPlotLibNet | Notes |
|---|---|---|
| `fig.add_subplot(projection='3d')` | `ax.WithProjection(elevation, azimuth)` | The projection is a property of the axes, set where you draw. |
| `ax.plot_surface(x, y, z)` | `ax.Surface(x, y, z)` | |
| `ax.plot_wireframe(x, y, z)` | `ax.Wireframe(x, y, z)` | |
| `ax.plot_trisurf(x, y, z)` | `ax.Trisurf(x, y, z)` | |
| `ax.scatter(x, y, z)` | `ax.Scatter3D(x, y, z)` | |
| `ax.plot(x, y, z)` | `ax.Plot3D(x, y, z)` | |
| `ax.contour3D(x, y, z)` | `ax.Contour3D(x, y, z)` | |
| `ax.bar3d(…)` | `ax.Bar3D(x, y, z)` | |
| `ax.voxels(filled)` | `ax.Voxels(filled)` | |
| `ax.text(x, y, z, '…')` | `ax.Text3D(x, y, z, "…")` | |
| `ax.view_init(elev, azim)` | `ax.WithCamera(30, -60)` | |

## Polar

| matplotlib | MatPlotLibNet | Notes |
|---|---|---|
| `plt.subplot(projection='polar')` + `plot` | `ax.PolarPlot(theta, r)` | The polar frame comes with the series. |
| polar `scatter` | `ax.PolarScatter(theta, r)` | |
| polar `bar` | `ax.PolarBar(theta, r)` | |
| radar chart, by hand | `ax.Radar(labels, values)` | matplotlib has no radar verb; this one is built in. |

## Where to go next

- [Line charts](line-charts.md) and [bar charts](bar-charts.md) — the two you will reach for first.
- [Heatmaps and colormaps](heatmaps.md) — all 148 maps and how to normalise a scale.
- [Subplots and GridSpec](subplots.md) — the layouts `plt.subplots` and `gridspec` give you.
- [3D charts](threed.md) — surfaces, wireframes and the camera.
- The [API reference](https://xkqg.github.io/MatPlotLibNet/api/) — every verb, with its overloads.
