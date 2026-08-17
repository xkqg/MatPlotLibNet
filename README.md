# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel server-side SVG rendering, polymorphic export (SVG / PNG / PDF / animated GIF), and 82 series types across line, scatter, bar, 3D, streaming, polar, financial, statistical, hierarchical, Sankey, vector, and rotation-graph families. Ships with 13 map projections and embedded Natural Earth data, 30 themes, LaTeX-style MathText, O(1) streaming with **53 technical indicators** (classical moving averages + volatility, momentum, trend-follower, cycle, microstructure, entropy, change-point, and cross-asset causality), financial drawing tools (trendlines, Fibonacci retracements, horizontal levels), frame-based animation with 6 easing curves + Pause/Resume playback, native UI controls for **Blazor, WPF, MAUI, Avalonia, Uno Platform, ASP.NET Core**, and TypeScript clients for **Angular, React, and Vue** — no JavaScript framework, no WebView, no SaaS.

[![CI](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml/badge.svg)](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/xkqg/MatPlotLibNet)](https://github.com/xkqg/MatPlotLibNet)

## 🧭 What's next

**v1.14.2 (2026-08-17) — 3-D axis titles clear their own tick labels**: the title pad was a
constant (42 px for X, 60 for Y/Z) while the tick labels sit barely a line height inside it, so a
title printed through a tick label at every camera once the labels were wide enough
([#18](https://github.com/xkqg/MatPlotLibNet/issues/18) follow-up). The pad is now measured per
render from the labels the axis actually draws, so a wider tick font or a custom `TickFormatter`
moves the title with it. Also: axis titles now travel with the camera-derived face selection during
a drag, and the 3-D grid, the tick rows and the clearance measurement share one tick rule (the grid
used to ignore `MajorTicks.Spacing`). All 13 packages at 1.14.2; 10,714 tests, 0 failures; strict
coverage gate 659/659 classes at ≥90/90.

Prior: **v1.14.1 (2026-08-15) — the 3-D axis frame follows the camera**: the shaded panes, the drawn
cube edges, the wall grids and the X/Y/Z tick rows were pinned to the faces that are back-facing at
matplotlib's default view, so any camera outside `azimuth ∈ [−90°, 0°]` painted a pane onto a face
that had rotated to the front and drew two tick rows behind the data
([#18](https://github.com/xkqg/MatPlotLibNet/issues/18)). Which faces are at the back is now derived
from the camera on every render — a port of matplotlib's own rule, verified against matplotlib 3.11.1
over 144 cameras including its edge-on tie handling — and interactive rotation re-runs the same
selection per frame instead of re-projecting a fixed frame. Inside the historical quadrant the output
is unchanged: the five matplotlib pixel-fidelity fixtures pass untouched. Also fixes tick labels
flipping to the wrong side of their axis on the first drag frame, and `Pane3DConfig.Alpha`, which had
shipped for releases without any renderer reading it. All 13 packages at 1.14.1; strict coverage gate
659/659 classes at ≥90/90 (99.6% line / 97.2% branch); 10,709 tests, 0 failures.

Prior: **v1.14.0 (2026-07-12) — control-room pack** replaced the 1.13.1 dashboard template with
`Plt.OpsDashboard()`, a fluent composition API for KPI tiles, state timelines, and a shared trend
panel pinned to one caller-supplied time window (the library never reads the wall clock). Adds
`BulletGraphSeries` (Stephen Few's bar-plus-target-plus-bands replacement for radial gauges),
`Theme.Alarm`/`AlarmPalette` (a theme now names its reserved alarm colours), four operator
backgrounds (`OpsNight`/`OpsPanel`/`OpsWarm`/`OpsContrast`), and the full `StatTileSeries` tile
anatomy (`Target`, `Caption`, `Trend`, `Hatch`). Also fixes five defects the work surfaced,
including `HatchPattern` — shipped for releases but never actually painted by any renderer — now
wired end-to-end through `ShapeStyle` on every backend, a `ThemeBuilder.Build()` that silently
discarded 7 of its 15 theme properties (now clones instead of rebuilding), and a rolling time axis
that re-shuffled its tick labels every frame. Strict coverage gate 656/656 classes at ≥90/90;
10,252 tests. Design record:
[docs/contrib/v1-14-control-room-pack.md](docs/contrib/v1-14-control-room-pack.md).
**v1.13.1** shipped `FigureTemplates.OpsDashboard` (since superseded by `Plt.OpsDashboard()`
above) and a fix for `MplLiveChart`'s relative `HubUrl` silently never connecting. **v1.13.0
(2026-07-04) — refactor & cleanup release** (no new chart features): breaking API cleanups —
`IGeoProjection.Forward`/`Inverse`/`Bounds` now return record structs instead of tuples,
`StreamingSeriesBase`/`StreamingIndicatorBase` are renamed to `StreamingSeries`/
`StreamingIndicator`, `IRenderContext` draw methods take `StrokeStyle`/`ShapeStyle` records, and
the synchronous `InteractiveExtensions.Show(Figure)` was removed in favor of the async-only
`ShowAsync()`. Full migration notes for every release are in the [CHANGELOG](CHANGELOG.md).

Future releases are **community-driven** — there is no fixed feature roadmap:

- 🐛 **Bug fixes** — driven by community use and the strict `≥90/90` per-class coverage gate (every class passes; see [Coverage Policy](docs/COVERAGE.md) for the current numbers).
- 📚 **Documentation polish** — cookbook examples, API XML doc completeness.
- 🌱 **Listening** — Open a [Discussion](https://github.com/xkqg/MatPlotLibNet/discussions) or [Issue](https://github.com/xkqg/MatPlotLibNet/issues) with what's missing for your use case. The next direction will be guided by what real users need, not by a feature checklist.

For the full release notes, see the [CHANGELOG](CHANGELOG.md).

---

## Documentation

Full documentation is on the **[GitHub Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)**, the **[Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/)** (runnable examples with rendered images), and the **[API Reference](https://xkqg.github.io/MatPlotLibNet/api/)** (generated from XML doc comments):

- [Playground](https://xkqg.github.io/MatPlotLibNet/playground/) — try charts live in the browser — pick an example, tweak parameters, see the SVG update instantly
- [Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/) — copy-paste code examples with rendered output for every chart type
- [API Reference](https://xkqg.github.io/MatPlotLibNet/api/) — full API documentation from source
- [Getting Started](https://github.com/xkqg/MatPlotLibNet/wiki/Getting-Started) — installation, output formats, subplots
- [Fluent Cheatsheet](https://github.com/xkqg/MatPlotLibNet/wiki/Fluent-Cheatsheet) — one-page reference for `Plt` / `FigureBuilder` / `AxesBuilder`
- [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map) — all 13 NuGet + 3 npm packages in detail
- [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types) — all 82 series with code examples
- [Streaming & Realtime](https://github.com/xkqg/MatPlotLibNet/wiki/Streaming) — ring buffers, StreamingFigure, axis scaling, 11 streaming indicators, platform controls
- [Interactive Controls](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) — Avalonia + Uno native controls, managed interaction layer
- [Bidirectional SignalR](https://github.com/xkqg/MatPlotLibNet/wiki/Bidirectional-SignalR) — server-authoritative interactive charts, event hierarchy, hub wiring
- [DataFrame](https://github.com/xkqg/MatPlotLibNet/wiki/DataFrame) — indicators, polynomial regression from `Microsoft.Data.Analysis.DataFrame`
- [Notebooks](https://github.com/xkqg/MatPlotLibNet/wiki/Notebooks) — Polyglot Notebooks + Jupyter inline rendering
- [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling) — themes, colormaps, PropCycler
- [Accessibility](https://github.com/xkqg/MatPlotLibNet/wiki/Accessibility) — SVG semantics, keyboard navigation, color-blind palette
- [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced) — date axes, math text, animations, GIF, real-time
- [Benchmarks](BENCHMARKS.md) — SVG rendering, SIMD transforms, indicators, Skia export (`BENCHMARKS.md` in the repo root; also mirrored on the [wiki](https://github.com/xkqg/MatPlotLibNet/wiki/Benchmarks))
- [Roadmap](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap) — version history and planned phases
- [Contributing](https://github.com/xkqg/MatPlotLibNet/wiki/Contributing) — build, test, coding conventions

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

**Native UI controls** — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Avalonia 12 and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Uno Platform render charts natively via SkiaSharp — no browser, no WebView, no SignalR required. 9 interaction modifiers: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click), crosshair (passive), hover tooltip. Toolbar state model, view history (back/forward), data cursor (click-to-pin), tick mirroring, tight margins.

**MathText** — LaTeX-like inline math in any label or title: `$\alpha^{2}$`, `$\frac{a}{b}$`, `$\sqrt{x}$`, `$\hat{x}$`, `$\mathbf{F}$`, `$\mathbb{R}$`. 96 symbol mappings (Greek, math operators, arrows, relations, set/logic, blackboard bold), fractions, square roots, accents, font variants, spacing, and scaling delimiters.

**3-D charts** — 12 series types: Surface, Scatter3D, Bar3D, PlanarBar3D, Line3D, Trisurf3D (Delaunay), Contour3D (marching squares), Quiver3D (vector field), Voxels (face-culled cubes), Text3D (annotations). Full `Projection3D` pipeline, `DepthQueue3D` painter's algorithm, `Vec3.FaceNormal` + `Color.Shade()`/`Color.Modulate()` extension-based shading, `Svg3DRotationScript` client-side rotation with depth re-sorting, camera-derived back faces (`CubeFaceSelection` — the panes, cube edges, wall grids and tick rows follow the camera at any azimuth, server-side and during a drag), configurable `Pane3DConfig` (floor/wall colors), and 3D colorbar support.

**Streaming & Realtime** — `StreamingLineSeries`, `StreamingScatterSeries`, `StreamingSignalSeries`, `StreamingCandlestickSeries` backed by `DoubleRingBuffer` with `AppendPoint(x, y)`. `StreamingFigure` provides throttled re-rendering and auto-scaling axes (`SlidingWindow`, `StickyRight`, `AutoScale`). 11 streaming indicators (SMA, EMA, RSI, Bollinger, MACD, OBV, ATR, Stochastic, WilliamsR, CCI, VWAP) auto-attach to candlestick data. Streaming controls for Avalonia, Uno, MAUI, Blazor, and ASP.NET Core. SVG diff engine for bandwidth optimization. Rx `IObservable<T>` adapter.

**Geographic projections** — `MatPlotLibNet.Geo` package with 5 map projections (PlateCarree, Mercator, Robinson, Orthographic, LambertConformal), GeoJSON parser, Natural Earth 110m embedded data, and `GeoPolygonSeries` for coastlines/borders/choropleth. Symlog axis scale for data spanning positive and negative ranges.

**Bidirectional SignalR** — server-authoritative interactive charts with mutation events (zoom, pan, reset, legend toggle) and notification events (brush-select, hover). Stacked-record event hierarchy, natural coalescing, per-caller hover responses.

**142 colormaps** — viridis, plasma, turbo, coolwarm, and 138 more (71 base maps, each with an auto-registered reversed `_r` variant). NumPy-style SIMD numerics (`Vec`, `Mat`, `Linalg`, `Fft`). Accessibility (ARIA, keyboard, Okabe-Ito palette, high-contrast theme). Matplotlib look-alike themes. DataFrame integration with **53 technical indicators** (v1.9.0 added 12 — Klinger, Twiggs MF, Ease of Movement, VWAP Z-Score, Supertrend, CG Oscillator, Inverse Fisher, YZ Vol Ratio, Ehlers iTrend, Decycler, Ehlers SuperSmoother, Transfer Entropy; v1.11.0 added Roc). Broken axes. Publication-quality SVG/PNG/PDF/GIF export.

---

## License

[MIT](LICENSE) — free for any use, open-source or commercial, with no copyleft conditions.
