# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/): fluent API, DI-friendly,
server-side SVG / PNG / PDF / animated-GIF export, and 82 series types. Ships with 13 map projections and embedded
Natural Earth data, 30 themes, LaTeX-style MathText, O(1) streaming with 53 technical indicators, a control room
(`Plt.OpsDashboard()` — KPI tiles, state timelines, one shared trend window), and native controls for Blazor, WPF,
MAUI, Avalonia, Uno and ASP.NET Core, plus TypeScript clients for Angular, React and Vue. No JavaScript framework,
no WebView, no SaaS.

[![CI](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml/badge.svg)](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/xkqg/MatPlotLibNet)](https://github.com/xkqg/MatPlotLibNet)

## Where this is going

The **1.14** line is control-room work: `Plt.OpsDashboard()` and the tile anatomy arrived with it, and the
releases since are the fixes and extensions a live operator wall asked for — a secondary Y axis that is a full
citizen, streaming that draws in SVG, 3-D axis titles that clear their own labels, tile captions that wrap and
stay inside their tile, and an ops window whose ticks read as time.

After that the cadence is community-driven: bug fixes, documentation, and whatever real use turns up. Open a
[Discussion](https://github.com/xkqg/MatPlotLibNet/discussions) or an
[Issue](https://github.com/xkqg/MatPlotLibNet/issues) — that is what steers the next release. Every release,
with its migration notes, is in the [CHANGELOG](CHANGELOG.md).

Quality bar: a strict per-class coverage gate at ≥90 % line and branch (659 classes, 99.6 % / 97.2 %) across
10,025 tests, with rendering verified against matplotlib pixel-fidelity fixtures.

---

## Documentation

- **[Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)** — getting started, cheatsheet, all 82 chart types, the
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
| **MatPlotLibNet.Notebooks** | `#r "nuget: MatPlotLibNet.Notebooks"` | Inline SVG in Polyglot / Jupyter notebooks |
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

**82 series types** — line, scatter, bar, histogram, pie, box, violin, heatmap, contour, candlestick, OHLC, treemap, sunburst, Sankey, polar, polar heatmap, 3D surface, Bar3D, PlanarBar3D, Line3D, Trisurf3D, Contour3D, Quiver3D, Voxels, Text3D, radar, waterfall, funnel, gauge, stat tile (single-value KPI), state timeline (discrete state segments over time), pair grid, relative rotation graph, streaming line/scatter/signal/candlestick, and more.

**Control room** — `Plt.OpsDashboard()` composes one operator screen: KPI tiles across the top, state timelines under them, and a shared trend panel, all pinned to one caller-supplied time window (the library never reads the wall clock). The tile carries a `Target`, a wrapping multi-line `Caption`, an inline sparkline and a `Hatch` that means *no information*; `Theme.Alarm` names the reserved alarm colours and four operator backgrounds ship with it. `BulletGraphSeries` replaces the radial gauge.

**Native UI controls** — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Avalonia 12 and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Uno Platform render charts natively via SkiaSharp — no browser, no WebView, no SignalR required. 9 interaction modifiers: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click), crosshair (passive), hover tooltip. Toolbar state model, view history (back/forward), data cursor (click-to-pin), tick mirroring, tight margins.

**MathText** — LaTeX-like inline math in any label or title: `$\alpha^{2}$`, `$\frac{a}{b}$`, `$\sqrt{x}$`, `$\hat{x}$`, `$\mathbf{F}$`, `$\mathbb{R}$`. 96 symbol mappings (Greek, math operators, arrows, relations, set/logic, blackboard bold), fractions, square roots, accents, font variants, spacing, and scaling delimiters.

**3-D charts** — 12 series types: Surface, Scatter3D, Bar3D, PlanarBar3D, Line3D, Trisurf3D (Delaunay), Contour3D (marching squares), Quiver3D (vector field), Voxels (face-culled cubes), Text3D (annotations). Full `Projection3D` pipeline, `DepthQueue3D` painter's algorithm, `Vec3.FaceNormal` + `Color.Shade()`/`Color.Modulate()` extension-based shading, `Svg3DRotationScript` client-side rotation with depth re-sorting, camera-derived back faces (`CubeFaceSelection` — the panes, cube edges, wall grids and tick rows follow the camera at any azimuth, server-side and during a drag), configurable `Pane3DConfig` (floor/wall colors), and 3D colorbar support.

**Streaming & Realtime** — `StreamingLineSeries`, `StreamingScatterSeries`, `StreamingSignalSeries`, `StreamingCandlestickSeries` backed by `DoubleRingBuffer` with `AppendPoint(x, y)`. `StreamingFigure` provides throttled re-rendering and auto-scaling axes (`SlidingWindow`, `StickyRight`, `AutoScale`). 11 streaming indicators (SMA, EMA, RSI, Bollinger, MACD, OBV, ATR, Stochastic, WilliamsR, CCI, VWAP) auto-attach to candlestick data. Streaming controls for Avalonia, Uno, MAUI, Blazor, and ASP.NET Core. SVG diff engine for bandwidth optimization. Rx `IObservable<T>` adapter.

**Geographic projections** — `MatPlotLibNet.Geo` package with 5 map projections (PlateCarree, Mercator, Robinson, Orthographic, LambertConformal), GeoJSON parser, Natural Earth 110m embedded data, and `GeoPolygonSeries` for coastlines/borders/choropleth. Symlog axis scale for data spanning positive and negative ranges.

**Bidirectional SignalR** — server-authoritative interactive charts with mutation events (zoom, pan, reset, legend toggle) and notification events (brush-select, hover). Stacked-record event hierarchy, natural coalescing, per-caller hover responses.

**142 colormaps** — viridis, plasma, turbo, coolwarm, and 138 more (71 base maps, each with an auto-registered reversed `_r` variant). NumPy-style SIMD numerics (`Vec`, `Mat`, `Linalg`, `Fft`). Accessibility (ARIA, keyboard, Okabe-Ito palette, high-contrast theme). Matplotlib look-alike themes. DataFrame integration with **53 technical indicators**. Broken axes. Publication-quality SVG/PNG/PDF/GIF export.

---

## License

[MIT](LICENSE) — free for any use, open-source or commercial, with no copyleft conditions.
