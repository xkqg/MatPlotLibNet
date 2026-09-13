# MatPlotLibNet — matplotlib for .NET

MatPlotLibNet is a charting library for C#, .NET 10 and .NET 8, inspired by
[matplotlib](https://matplotlib.org/). It draws 83 chart types and saves them as SVG, PNG, PDF or an animated GIF,
straight from your server code. It does not need a JavaScript framework, a WebView or a hosted service.

It has a fluent API and it works with dependency injection. It ships with 148 colormaps, 30 themes, 13 map
projections with embedded Natural Earth data, LaTeX-style MathText, text in any script, O(1) streaming with 53
technical indicators, a control room (`Plt.OpsDashboard()` — KPI tiles, state timelines, one shared trend window),
and an MCP server so an AI agent can draw with it. Charts render natively in Blazor, WPF, MAUI, Avalonia, Uno and
ASP.NET Core, and TypeScript clients are there for Angular, React and Vue.

[![CI](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml/badge.svg)](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/xkqg/MatPlotLibNet)](https://github.com/xkqg/MatPlotLibNet)

## Where this is going

**1.17** draws text in any script. Arabic letters join, Hebrew reads right to left, and a number or a Latin word
inside Arabic or Hebrew text is placed in the correct position, using the font that ships in the package. The
Unicode bidi algorithm is implemented in full and checked against Unicode's own 861,948 test cases; HarfBuzz shapes
each run. Other scripts need a font file: `SkiaFonts.Register(path)` registers one, and the MCP server reads font
files from the `MATPLOTLIBNET_FONTS` variable. Latin text is now kerned too, so text widths changed by up to 2 px
per letter pair.

**1.16** adds a data table beside every chart, for a reader who cannot see the picture.
`figure.ToDataTables()` returns the figure's data as tables with typed columns, and you can write each table
as HTML, Markdown or CSV. Every host serves the table: an ASP.NET Core endpoint next to the SVG endpoint, a
`ShowDataTable` option on the Blazor component, a `chartDataTable` field in the GraphQL schema, and a fifth MCP
tool. The same release fixed the streaming series. A point and a candle are now stored as one value, so a snapshot
can no longer show a point or a bar that never existed. The buffer behind them is now the generic `RingBuffer<T>`.

**1.15** added support for AI agents. `MatPlotLibNet.Mcp` is a Model Context Protocol server. A model can render
any of these chart types from a JSON spec and look at the result. When the spec is wrong, the server reports which
field is wrong instead of returning a blank picture.

**1.14** added the control room: `Plt.OpsDashboard()` and the tile anatomy arrived with it. The releases since
then contain the fixes and extensions that real control-room use required: a secondary Y axis with the same
capabilities as the primary axis, streaming that draws in SVG, 3-D axis titles that do not overlap their own
labels, tile captions that wrap and stay inside their tile, and an ops window whose ticks are formatted as time.

After that, releases follow what the community needs: bug fixes, documentation, and whatever comes up in real use.
Open a [Discussion](https://github.com/xkqg/MatPlotLibNet/discussions) or an
[Issue](https://github.com/xkqg/MatPlotLibNet/issues); that is what decides the next release. Every release, with
its migration notes, is listed in the [CHANGELOG](CHANGELOG.md).

Quality bar: every class must pass a strict coverage gate of ≥90 % line and branch coverage (706 classes, at
99.6 % line / 97.5 % branch) across 11,757 tests. The test suite verifies rendering against matplotlib
pixel-fidelity fixtures.

---

## Documentation

- **[Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)** — getting started, cheatsheet, all 83 chart types, the
  control room, styling, streaming, SignalR, packages
- **[Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/)** — copy-paste examples with rendered output
- **[Playground](https://xkqg.github.io/MatPlotLibNet/playground/)** — try charts live in the browser
- **[API Reference](https://xkqg.github.io/MatPlotLibNet/api/)** — generated from the XML docs
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
| **MatPlotLibNet.Wpf** | `dotnet add package MatPlotLibNet.Wpf` | Native WPF `MplChartControl` via SkiaSharp — all 9 interaction modifiers |
| **MatPlotLibNet.Geo** | `dotnet add package MatPlotLibNet.Geo` | 13 map projections, GeoJSON parser, Natural Earth 110m data, geographic polygons |
| **MatPlotLibNet.Notebooks** | `#r "nuget: MatPlotLibNet.Notebooks"` | Inline SVG in Polyglot / Jupyter notebooks. Microsoft has ended Polyglot Notebooks and .NET Interactive, which this package builds on, so existing notebooks keep working but nothing new is coming — for new work use **MatPlotLibNet.Interactive** |
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
    .Plot(x, y, s => { s.Color = Color.Blue; s.Label = "Data"; })
    .WithLegend()
    .Save("chart.svg");
```

---

## Features

**83 series types** — line, scatter (plain or coloured by point density), bar (plain, stacked, or several groups per category in one call), histogram, pie, box, violin, heatmap, contour, candlestick, OHLC, treemap, sunburst, Sankey, polar, polar heatmap, 3D surface, Bar3D, PlanarBar3D, Line3D, Trisurf3D, Contour3D, Quiver3D, Voxels, Text3D, radar, waterfall, funnel, gauge, stat tile (single-value KPI), state timeline (discrete state segments over time), pair grid, relative rotation graph, streaming line/scatter/signal/candlestick, and more.

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

**Native UI controls** — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Avalonia 12 and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Uno Platform render charts natively via SkiaSharp. They need no browser, no WebView and no SignalR. There are 9 interaction modifiers: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click), crosshair (passive), hover tooltip. The controls also have a toolbar state model, view history (back/forward), a data cursor (click-to-pin), tick mirroring, and tight margins. Clicking a data point raises `DataPointClicked` with the point the reader clicked — series, value, pixel position and axes — so an application can open a detail panel or select a row beside the chart. A server-driven chart reports the same click to its `OnDataCursor` handler.

**MathText** — LaTeX-like inline math in any label or title: `$\alpha^{2}$`, `$\frac{a}{b}$`, `$\sqrt{x}$`, `$\hat{x}$`, `$\mathbf{F}$`, `$\mathbb{R}$`. It supports 96 symbol mappings (Greek, math operators, arrows, relations, set/logic, blackboard bold), fractions, square roots, accents, font variants, spacing, and scaling delimiters.

**International text** — labels, titles and legend entries in any script. Arabic and Hebrew work with the bundled font: the Unicode bidi algorithm (UAX #9, all 861,948 conformance cases) finds the reading direction, HarfBuzz shapes each run, and the runs are laid out in visual order. `SkiaFonts.Register(path)` adds a font file for Devanagari, Thai, Chinese and other scripts; the MCP server takes them from `MATPLOTLIBNET_FONTS`. The same shaping pass produces the text measurement, the SVG glyph outlines and the PNG pixels.

**3-D charts** — 12 series types: Surface, Scatter3D, Bar3D, PlanarBar3D, Line3D, Trisurf3D (Delaunay), Contour3D (marching squares), Quiver3D (vector field), Voxels (face-culled cubes), Text3D (annotations). The 3-D support includes a full `Projection3D` pipeline, a `DepthQueue3D` painter's algorithm, shading from `Vec3.FaceNormal` plus the `Color.Shade()`/`Color.Modulate()` extension methods, `Svg3DRotationScript` for client-side rotation with depth re-sorting, camera-derived back faces (`CubeFaceSelection`: the panes, cube edges, wall grids and tick rows follow the camera at any azimuth, both server-side and during a drag), a configurable `Pane3DConfig` (floor/wall colors), and 3D colorbar support.

**Streaming & Realtime** — `StreamingLineSeries`, `StreamingScatterSeries`, `StreamingSignalSeries` and `StreamingCandlestickSeries` are backed by `RingBuffer<T>` and add points with `AppendPoint(x, y)`. A `RingBuffer<T>` is a fixed-capacity circular sequence of any type (64 M appends/s, and it never allocates on append). `StreamingFigure` provides throttled re-rendering and auto-scaling axes (`SlidingWindow`, `StickyRight`, `AutoScale`). 11 streaming indicators (SMA, EMA, RSI, Bollinger, MACD, OBV, ATR, Stochastic, WilliamsR, CCI, VWAP) attach automatically to candlestick data. Streaming controls exist for Avalonia, Uno, MAUI, Blazor, and ASP.NET Core. The library includes an SVG diff engine to save bandwidth, and an Rx `IObservable<T>` adapter.

**Geographic projections** — the `MatPlotLibNet.Geo` package has 5 map projections (PlateCarree, Mercator, Robinson, Orthographic, LambertConformal), a GeoJSON parser, embedded Natural Earth 110m data, and `GeoPolygonSeries` for coastlines, borders and choropleths. A symlog axis scale handles data that spans positive and negative ranges.

**Bidirectional SignalR** — server-authoritative interactive charts with mutation events (zoom, pan, reset, legend toggle) and notification events (brush-select, hover, the clicked data point). The events form a hierarchy of stacked records, they merge on their own, and the server sends each caller its own hover response.

**Accessibility** — `figure.ToDataTables()` gives every chart a data table beside the picture (`ToHtml()`, `ToMarkdown()`, `ToCsv()`). That table is the text alternative that WCAG 1.1.1 requires for a complex image, and an `<svg role="img">` cannot provide it on its own. The table is served by `MapChartTableEndpoint`, by `MplChart.ShowDataTable`, by the GraphQL `chartDataTable` field and by the MCP `chart_data_table` tool, which returns it as markdown to read and as structured values to compute with. The library also has ARIA roles and titles, keyboard navigation, the Okabe-Ito colour-blind-safe palette, the three Petroff accessible colour sequences and a high-contrast theme.

**148 colormaps** — viridis, plasma, turbo, coolwarm, and 144 more (74 base maps, each with an auto-registered reversed `_r` variant). The library also has NumPy-style SIMD numerics (`Vec`, `Mat`, `Linalg`, `Fft`), themes that look like matplotlib's, DataFrame integration with **58 technical indicators**, broken axes, and publication-quality SVG/PNG/PDF/GIF export.

---

## License

[MIT](LICENSE) — free for any use, open-source or commercial, with no copyleft conditions.
