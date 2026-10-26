# MatPlotLibNet — matplotlib for .NET

MatPlotLibNet is a charting library for C#, .NET 10 and .NET 8, inspired by
[matplotlib](https://matplotlib.org/). It draws 83 chart types and saves them as SVG, PNG, PDF or an animated GIF,
straight from your server code. It does not need a JavaScript framework, a WebView or a hosted service.

It has a fluent API and it works with dependency injection. It ships with 148 colormaps, 30 themes, 13 map
projections with embedded Natural Earth data, LaTeX-style MathText, text in any script, O(1) streaming with 58
technical indicators, a control room (`Plt.OpsDashboard()` — KPI tiles, state timelines, one shared trend window),
and an MCP server so an AI agent can draw with it. Charts render natively in Blazor, WPF, MAUI, Avalonia, Uno and
ASP.NET Core, inside a Verso notebook cell, and TypeScript clients are there for Angular, React and Vue.

[![CI](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml/badge.svg)](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/xkqg/MatPlotLibNet)](https://github.com/xkqg/MatPlotLibNet)

## Where this is going

**1.23.0** moves the technical indicators that read a candle's open, high, low, close and volume from separate
array parameters to one `OhlcvBars` value, built with the factory that names the columns the indicator actually
uses (`OhlcvBars.Hlc(high, low, close)`, `OhlcvBars.Cv(close, volume)`, and so on) instead of repeating the same
arrays across every indicator on a chart. A chart with more than two scales now has somewhere to put the extra
ones: `WithOutwardYAxis` stacks a second axis past the first on either side — four scales total, two left and two
right, each with its own range, ticks, series and legend entry, the way matplotlib moves a twin's spine outward. A
secondary axis restores through the same registry the primary one does, so a bar or an area survives there and on
the top axis too, not only a line or a point; and a wheel zoom, a drag pan, a rectangle zoom and Home now move
every one of those axes, each by its own amount, whether the server answers the gesture or a native control does.

**1.22.0** lets several series share one colour instead of each taking the next one from the theme's cycle, and
spends it on three things: a Kaplan-Meier curve now draws its confidence band and its censor marks in the curve's
own colour, and two new verbs, `Raincloud` (a half violin, a narrow box and a group's raw points in one colour) and
`Horizon` (a signed series folded into bands of two hues), bring the axes builder to seventeen verbs.

**1.21.0** makes every chart type and every axes setting round-trip through JSON whole. A Sankey's nodes and
links, a treemap's, sunburst's, dendrogram's and clustermap's trees, a tree grid's rows, a polar chart's data, a
streaming chart's current points and a planar 3-D bar's data all come back from JSON as themselves now, not a
placeholder — the [MCP server](https://github.com/xkqg/MatPlotLibNet/wiki/MCP-Server) lists and accepts all eight
again, where it used to refuse them by name. An axes' Z axis, its 3-D pane colours and its tooltip switch come back
too. The one series this release did not reach is `MatPlotLibNet.Geo`'s `GeoPolygonSeries`, named on the
[roadmap](ROADMAP.md).

**1.20.0** turns charts that took a page of code into one call. Fifteen verbs on the axes builder, among them
`Pareto`, `Ridgeline`, `CalendarHeatmap`, `QQPlot`, `ControlChart`, `KaplanMeier`, `StreamGraph` and `Marimekko`, take
the data as you hold it (names, groups, samples, dates) and build the chart from the series the library already has.
Underneath them, a figure written to JSON now reads back as the picture it was, with every setting it draws, so the
MCP server and every transport carry it whole. NaN is the one missing value: a line breaks at a gap, as in
matplotlib, and nothing is drawn at NaN. Bars on a log axis reach the bottom edge, horizontal bars work, category
names are tick labels with room for the axis title, and `WithDpi` and `SaveOptions` now reach the file. Every new call
is shown in code on the wiki's [examples page](https://github.com/xkqg/MatPlotLibNet/wiki/Examples#1200).

**1.19.0** is for charts with more than one line and more than one axis. `WithDirectLabel()` writes a line's name at
its end, in the line's own colour, instead of in the legend, and `SetYAxisColor` / `SetXAxisColor` colour an axis to
match its line in one call, on the left, bottom, right and top axis alike. The right-hand and top axes now follow their
own tick settings, get room of their own in the layout and keep their colours through JSON, and a line on them hides
when its legend entry is clicked. On a server-interactive chart and in the WPF, Avalonia and Uno controls, hover,
selection, zoom, pan and the data cursor report the values the reader sees, on log axes too, and an axis limit you
set, reversed or not, is now exactly the range drawn. Every new call
is shown in code on the wiki's [examples page](https://github.com/xkqg/MatPlotLibNet/wiki/Examples#1190), and every C#
example in the wiki now compiles.

After that, releases follow what the community needs: bug fixes, documentation, and whatever comes up in real use.
Open a [Discussion](https://github.com/xkqg/MatPlotLibNet/discussions) or an
[Issue](https://github.com/xkqg/MatPlotLibNet/issues); that is what decides the next release. Every release, with
its migration notes, is listed in the [CHANGELOG](CHANGELOG.md), and the [roadmap](ROADMAP.md) says what is done,
what is still to come and what is left out on purpose, with a section for each row on the
[wiki's page](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap).

Quality bar: every class must pass a strict coverage gate of ≥90 % line and branch coverage (848 classes, at
99.7 % line / 98.5 % branch) across 19,995 tests. A separate suite (`Tst/MatPlotLibNet.Fidelity`) compares the
rendering with reference images drawn by matplotlib; it is not part of the CI run.

---

## Documentation

- **[Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)** — getting started, cheatsheet, all 83 chart types, the
  control room, styling, streaming, SignalR, packages
- **[From matplotlib to C#](https://xkqg.github.io/MatPlotLibNet/cookbook/matplotlib-to-csharp.html)** — the pyplot call you know, and the line that draws it here
- **[Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/)** — copy-paste examples with rendered output
- **[Playground](https://xkqg.github.io/MatPlotLibNet/playground/)** — try charts live in the browser
- **[API Reference](https://xkqg.github.io/MatPlotLibNet/api/)** — generated from the XML docs
- **[Roadmap](ROADMAP.md)** — what is done and what is still to come, in the order each row needs the one above it;
  the [wiki's page](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap) says what each row takes and what is left out
  on purpose
- **[Benchmarks](BENCHMARKS.md)** · **[Coverage policy](docs/COVERAGE.md)** · **[CHANGELOG](CHANGELOG.md)**

---

## Packages

| Package | Install | What it does |
|---|---|---|
| **MatPlotLibNet** | `dotnet add package MatPlotLibNet` | Core: models, fluent API, SVG rendering, JSON, transforms |
| **[MatPlotLibNet.DataFrame](https://www.nuget.org/packages/MatPlotLibNet.DataFrame)** | `dotnet add package MatPlotLibNet.DataFrame` | `Microsoft.Data.Analysis.DataFrame` extension methods — plot, indicators (SMA/EMA/RSI/MACD/…), and polynomial regression from named columns |
| **MatPlotLibNet.Skia** | `dotnet add package MatPlotLibNet.Skia` | PNG, PDF, and animated GIF export via SkiaSharp |
| **MatPlotLibNet.Blazor** | `dotnet add package MatPlotLibNet.Blazor` | `MplChart` + `MplLiveChart` Razor components with SignalR |
| **MatPlotLibNet.AspNetCore** | `dotnet add package MatPlotLibNet.AspNetCore` | REST endpoints, SignalR hub, `IChartPublisher` |
| **MatPlotLibNet.Interactive** | `dotnet add package MatPlotLibNet.Interactive` | `figure.ShowAsync()` — browser popup, no server needed |
| **MatPlotLibNet.GraphQL** | `dotnet add package MatPlotLibNet.GraphQL` | GraphQL queries + subscriptions via HotChocolate |
| **MatPlotLibNet.Maui** | `dotnet add package MatPlotLibNet.Maui` | Native `MplChartView` via Microsoft.Maui.Graphics |
| **MatPlotLibNet.Avalonia** | `dotnet add package MatPlotLibNet.Avalonia` | Native `MplChartControl` for Avalonia 12 — Skia backend, optional local interaction |
| **MatPlotLibNet.Uno** | `dotnet add package MatPlotLibNet.Uno` | Native `MplChartElement` for Uno Platform (WinUI 3 / Android / iOS / macCatalyst) |
| **MatPlotLibNet.Wpf** | `dotnet add package MatPlotLibNet.Wpf` | Native WPF `MplChartControl` via SkiaSharp — all 15 interaction modifiers — and `MplStreamingChartControl` for live data; your project targets `netX.0-windows10.0.19041.0` |
| **MatPlotLibNet.Geo** | `dotnet add package MatPlotLibNet.Geo` | 13 map projections, GeoJSON parser, Natural Earth 110m data, geographic polygons |
| **[MatPlotLibNet.Verso](https://www.nuget.org/packages/MatPlotLibNet.Verso)** | Verso → Extensions panel | A chart inside a Verso notebook cell, drawn as SVG ([cookbook](docs/cookbook/verso.md)) |
| **MatPlotLibNet.Notebooks** | `#r "nuget: MatPlotLibNet.Notebooks"` | Inline SVG in Polyglot / Jupyter notebooks. Microsoft has ended Polyglot Notebooks and .NET Interactive, which this package builds on, so existing notebooks keep working but nothing new is coming — for new work use **MatPlotLibNet.Verso** |
| **[MatPlotLibNet.Mcp](https://www.nuget.org/packages/MatPlotLibNet.Mcp)** | `dnx MatPlotLibNet.Mcp` | MCP server — an AI agent renders charts over stdio ([cookbook](docs/cookbook/mcp.md)) |
| **@matplotlibnet/angular** | `npm install @matplotlibnet/angular` | Angular components + TypeScript SignalR client |
| **@matplotlibnet/react** | `npm install @matplotlibnet/react` | React hooks + components + TypeScript SignalR client |
| **@matplotlibnet/vue** | `npm install @matplotlibnet/vue` | Vue 3 composables + TypeScript SignalR client |

---

## Quick start

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Styling;

double[] x = [1, 2, 3, 4, 5];
double[] y = [2, 4, 3, 5, 1];

Plt.Create()
    .WithTitle("My First Chart")
    .WithTheme(Theme.Dark)
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => { s.Color = Colors.Blue; s.Label = "Data"; })
        .WithLegend())
    .Save("chart.svg");
```

---

## Features

**83 series types** — line, scatter (plain or coloured by point density), bar (plain, stacked, or several groups per category in one call), histogram, pie, box, violin, heatmap, contour, candlestick, OHLC, treemap, sunburst, Sankey, polar, polar heatmap, 3D surface, Bar3D, PlanarBar3D, Line3D, Trisurf3D, Contour3D, Quiver3D, Voxels, Text3D, radar, waterfall, funnel, gauge, stat tile (single-value KPI), state timeline (discrete state segments over time), pair grid, relative rotation graph, streaming line/scatter/signal/candlestick, and more. On top of them, fifteen one-call charts (`Lollipop`, `Dumbbell`, `Pareto`, `SlopeGraph`, `DotPlot`, `ParallelCoordinates`, `Marimekko`, `Ridgeline`, `QQPlot`, `KaplanMeier`, `BumpChart`, `ControlChart`, `StreamGraph`, `CalendarHeatmap`, `Spy`) are built from those series, so they add none to the count and travel through JSON as their parts.

**MCP server — charts for an AI agent** — `MatPlotLibNet.Mcp` is a
[Model Context Protocol](https://modelcontextprotocol.io) server. It ships as a .NET tool that a host starts over
stdio. It has five tools: `render_chart` (returns a PNG plus a text summary of what was drawn), `save_chart`
(writes PNG/SVG/PDF to a file and returns the path it wrote), `chart_data_table` (returns the numbers the picture
is drawn from, as markdown, which an image alone cannot give), `list_chart_types` and `describe_chart_schema`. The
spec is the library's own figure JSON, so an agent can ask for anything the library draws. When a spec is wrong,
the server rejects it before rendering and names the wrong field, instead of returning a blank picture. See the
[cookbook](docs/cookbook/mcp.md).

```json
{ "servers": { "MatPlotLibNet.Mcp": { "type": "stdio", "command": "dnx", "args": ["MatPlotLibNet.Mcp", "--yes"] } } }
```

**Control room** — `Plt.OpsDashboard()` builds one operator screen: KPI tiles across the top, state timelines under them, a topology panel showing which service calls which (`AddTopology`), and a shared trend panel. All of them use one time window that the caller supplies; the library never reads the wall clock. A tile carries a `Target`, a multi-line `Caption` that wraps, an inline sparkline, and an `OpsCondition` saying what it is in. That condition has two axes on purpose: `OpsSeverity` (normal, warning, critical) is the only one that compares, and `OpsVisibility` (observed, unknown, shelved) says whether the reading can be believed and whether someone silenced it — a silent source is not a degraded one, and a muted alarm is not a solved problem. `OpsCondition.RollUp` reads a set of children the way an operator does: the worst child that can be seen decides the colour, the unseen and the muted are counted. `WithEventMarker` puts a deploy or an incident on every panel that has a clock. `Theme.Alarm` defines the reserved alarm colours, and four operator backgrounds ship with it. `BulletGraphSeries` replaces the radial gauge.

**Native UI controls** — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Avalonia 12 and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Uno Platform render charts natively via SkiaSharp. They need no browser, no WebView and no SignalR. There are 15 interaction modifiers: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click), reset (double-click, Home or Escape), data cursor (click on a point to pin it), the trendline, level and Fibonacci drawing tools, removing a selected drawing (Delete), crosshair (passive) and hover tooltip. The controls also have a toolbar state model, view history (back/forward), tick mirroring, and tight margins. Clicking a data point raises `DataPointClicked` with the point the reader clicked — series, value, pixel position and axes — so an application can open a detail panel or select a row beside the chart. A server-driven chart reports the same click to its `OnDataCursor` handler.

**MathText** — LaTeX-like inline math in any label or title: `$\alpha^{2}$`, `$\frac{a}{b}$`, `$\sqrt{x}$`, `$\hat{x}$`, `$\mathbf{F}$`, `$\mathbb{R}$`. It supports 96 symbol mappings (Greek, math operators, arrows, relations, set/logic, blackboard bold), fractions, square roots, accents, font variants, spacing, and scaling delimiters.

**International text** — labels, titles and legend entries in any script. Arabic and Hebrew work with the bundled font: the Unicode bidi algorithm (UAX #9, all 861,948 conformance cases) finds the reading direction, HarfBuzz shapes each run, and the runs are laid out in visual order. `SkiaFonts.Register(path)` adds a font file for Devanagari, Thai, Chinese and other scripts; the MCP server takes them from `MATPLOTLIBNET_FONTS`. The same shaping pass produces the text measurement, the SVG glyph outlines and the PNG pixels.

**3-D charts** — 12 series types: Surface, Wireframe, Scatter3D, Stem3D, Bar3D, PlanarBar3D, Line3D, Trisurf3D (Delaunay), Contour3D (marching squares), Quiver3D (vector field), Voxels (face-culled cubes), Text3D (annotations). The 3-D support includes a full `Projection3D` pipeline, a `DepthQueue3D` painter's algorithm, shading from `Vec3.FaceNormal` plus the `Color.Shade()`/`Color.Modulate()` extension methods, `Svg3DRotationScript` for client-side rotation with depth re-sorting, camera-derived back faces (`CubeFaceSelection`: the panes, cube edges, wall grids and tick rows follow the camera at any azimuth, both server-side and during a drag), a configurable `Pane3DConfig` (floor/wall colors), and 3D colorbar support.

**Streaming & Realtime** — `StreamingLineSeries`, `StreamingScatterSeries`, `StreamingSignalSeries` and `StreamingCandlestickSeries` are backed by `RingBuffer<T>` and add points with `AppendPoint(x, y)`. A `RingBuffer<T>` is a fixed-capacity circular sequence of any type (64 M appends/s, and it never allocates on append). `StreamingFigure` provides throttled re-rendering and auto-scaling axes (`SlidingWindow`, `StickyRight`, `AutoScale`). 11 streaming indicators (SMA, EMA, RSI, Bollinger, MACD, OBV, ATR, Stochastic, WilliamsR, CCI, VWAP) attach automatically to candlestick data. Streaming controls exist for WPF, Avalonia, Uno, MAUI, Blazor, and ASP.NET Core. The library includes an SVG diff engine to save bandwidth, and an Rx `IObservable<T>` adapter.

**Geographic projections** — the `MatPlotLibNet.Geo` package has 13 map projections (PlateCarree, Mercator, TransverseMercator, Robinson, Orthographic, LambertConformal, AlbersEqualArea, AzimuthalEquidistant, EqualEarth, Mollweide, Natural Earth, Sinusoidal, Stereographic), a GeoJSON parser, embedded Natural Earth 110m data, and `GeoPolygonSeries` for coastlines, borders and choropleths. A symlog axis scale handles data that spans positive and negative ranges.

**Bidirectional SignalR** — server-authoritative interactive charts with mutation events (zoom, pan, reset, legend toggle) and notification events (brush-select, hover, the clicked data point). The events form a hierarchy of stacked records, they merge on their own, and the server sends each caller its own hover response.

**Accessibility** — `figure.ToDataTables()` gives every chart a data table beside the picture (`ToHtml()`, `ToMarkdown()`, `ToCsv()`). That table is the text alternative that WCAG 1.1.1 requires for a complex image, and an `<svg role="img">` cannot provide it on its own. The table is served by `MapChartTableEndpoint`, by `MplChart.ShowDataTable`, by the GraphQL `chartDataTable` field and by the MCP `chart_data_table` tool, which returns it as markdown to read and as structured values to compute with. The library also has ARIA roles and titles, keyboard navigation, the Okabe-Ito colour-blind-safe palette, the three Petroff accessible colour sequences and a high-contrast theme.

**148 colormaps** — viridis, plasma, turbo, coolwarm, and 144 more (74 base maps, each with an auto-registered reversed `_r` variant). The library also has NumPy-style SIMD numerics (`Vec`, `Mat`, `Linalg`, `Fft`), themes that look like matplotlib's, DataFrame integration with **58 technical indicators**, broken axes, and publication-quality SVG/PNG/PDF/GIF export.

---

## License

[MIT](LICENSE) — free for any use, open-source or commercial, with no copyleft conditions.
