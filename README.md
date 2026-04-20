# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel SVG rendering, polymorphic export (SVG / PNG / PDF / GIF), and multi-platform output to Blazor, MAUI, Avalonia, Uno Platform, ASP.NET Core, Angular, React, and Vue.

[![CI](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml/badge.svg)](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/xkqg/MatPlotLibNet)](https://github.com/xkqg/MatPlotLibNet)

## 🧭 Stabilisation phase

After eleven feature releases (v1.0 → v1.7.2) MatPlotLibNet now covers the **practical 90% of matplotlib's surface**: 74 series types, 13 map projections with embedded Natural Earth data, 26 themes, MathText with operator limits and matrices, streaming with O(1) indicators, native UI controls for Blazor / Avalonia / Uno / WPF / MAUI, fidelity tests against a pinned matplotlib reference, and 13 NuGet packages.

**v1.7.1 started the stabilisation period; v1.7.2 continues it.** The focus shifts from "ship more features" to:

- 🐛 **Bug fixes only** (no new public API), driven by community use and the `≥90/90` coverage gate
- 🧪 **Test coverage uplift** (see [`docs/COVERAGE.md`](docs/COVERAGE.md)) — **7 325 main-project tests green** across 9 test projects, **97.26% line / 90.50% branch** (branch rate above the 90 % aggregate floor for the first time); strict-90 refactor (2026-04-20) decomposed three god-classes (AxesRenderer.RenderColorBar, CartesianAxesRenderer.Render, SankeySeriesRenderer) into 32 extracted SOLID subclasses, each at 100L/100B via direct TDD; dead code removed, `SvgRenderContext` tightened via composition + extension methods. Strict-mode flip still blocked by ~40 residual sub-90 classes — tractable follow-up. Byte-level SVG output unchanged vs shipped v1.7.2 (verified by 10 033-case equivalence fuzz)
- 📚 **Documentation polish** — cookbook examples, API XML doc completeness
- 🌱 **Listening** — what should v2 be? Open a [Discussion](https://github.com/xkqg/MatPlotLibNet/discussions) or [Issue](https://github.com/xkqg/MatPlotLibNet/issues) with what's missing for your use case. The next major direction will be guided by what real users need, not by a feature checklist.

No timeline for v1.8.0 yet — when it ships, it will be community-driven.

> **Browser interactions are automatic.** `FigureBuilder.WithBrowserInteraction()` is the
> single switch — pan/zoom, legend toggle + drag, treemap drilldown, sankey hover, 3D
> rotation, rich tooltips, highlight, and brush selection all light up together. Each
> script self-detects whether it has anything to do (e.g. the treemap script is silent
> when the chart has no treemap nodes), so there's no per-feature toggle for the user
> to manage.

---

## 🩹 Bug fixes since last NuGet release

Bugs found and fixed on `main` after the v1.7.2 NuGet packages were published. Pull `main` (or wait for the next NuGet rev) to pick these up.

| Reported | Bug (why + where) | Fix (how) |
|---|---|---|
| 2026-04-19 | **Plot disappeared on legend press.** `SvgLegendToggleScript` fired the series-toggle on `pointerdown` instead of `click`, so the series hid the moment the user pressed — single/two-series charts looked empty, and grabbing a legend to drag was impossible. | Move toggle to `click` only (keep `Enter`/`Space` for keyboard). +1 regression test. |
| 2026-04-19 | **Legend was fixed in place** — no way to reposition without rebuilding server-side. | New `SvgLegendDragScript`: press-and-hold any legend item, drag the `<g class="legend">` group, release to drop. Coexists with pan/zoom and toggle via capture-phase `stopPropagation`. Translation is client-only (lost on full re-render). +4 behavioural tests. |
| 2026-04-19 | **Treemap parent tile click didn't expand children** — two compounding causes in `SvgTreemapDrilldownScript`: (a) `pointermove` latched the drag-suppression flag on plain hover, killing the next click; (b) pan/zoom's `setPointerCapture` redirected the synthetic click target to the SVG root, so the script's walk-up from `e.target` found nothing. | (a) Gate the move-threshold on an `isPointerDown` flag (false during hover). (b) Fall back to `document.elementFromPoint(x, y)` when walk-up fails. +2 regression tests + Jint harness uplift (`StubElementFromPoint`, `<remarks>` listing what the harness can't simulate). |

For the full v1.7.2 release notes (Phases L–Ω, matplotlib-parity follow-on, coverage uplift, depth-3 treemap, Phase X+Y+Z+Ω test-only sub-90/90 graduation waves), see the [CHANGELOG](CHANGELOG.md).

---

## Documentation

Full documentation is on the **[GitHub Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)**, the **[Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/)** (runnable examples with rendered images), and the **[API Reference](https://xkqg.github.io/MatPlotLibNet/api/)** (generated from XML doc comments):

- [Playground](https://xkqg.github.io/MatPlotLibNet/playground/) — try charts live in the browser — pick an example, tweak parameters, see the SVG update instantly
- [Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/) — copy-paste code examples with rendered output for every chart type
- [API Reference](https://xkqg.github.io/MatPlotLibNet/api/) — full API documentation from source
- [Getting Started](https://github.com/xkqg/MatPlotLibNet/wiki/Getting-Started) — installation, output formats, subplots
- [Fluent Cheatsheet](https://github.com/xkqg/MatPlotLibNet/wiki/Fluent-Cheatsheet) — one-page reference for `Plt` / `FigureBuilder` / `AxesBuilder`
- [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map) — all 13 NuGet + 3 npm packages in detail
- [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types) — all 74 series with code examples
- [Streaming & Realtime](https://github.com/xkqg/MatPlotLibNet/wiki/Streaming) — ring buffers, StreamingFigure, axis scaling, 11 streaming indicators, platform controls
- [Interactive Controls](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) — Avalonia + Uno native controls, managed interaction layer
- [Bidirectional SignalR](https://github.com/xkqg/MatPlotLibNet/wiki/Bidirectional-SignalR) — server-authoritative interactive charts, event hierarchy, hub wiring
- [DataFrame](https://github.com/xkqg/MatPlotLibNet/wiki/DataFrame) — indicators, polynomial regression from `Microsoft.Data.Analysis.DataFrame`
- [Notebooks](https://github.com/xkqg/MatPlotLibNet/wiki/Notebooks) — Polyglot Notebooks + Jupyter inline rendering
- [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling) — themes, colormaps, PropCycler
- [Accessibility](https://github.com/xkqg/MatPlotLibNet/wiki/Accessibility) — SVG semantics, keyboard navigation, color-blind palette
- [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced) — date axes, math text, animations, GIF, real-time
- [Benchmarks](https://github.com/xkqg/MatPlotLibNet/wiki/Benchmarks) — SVG rendering, SIMD transforms, indicators
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

**74 series types** — line, scatter, bar, histogram, pie, box, violin, heatmap, contour, candlestick, OHLC, treemap, sunburst, Sankey, polar, polar heatmap, 3D surface, Bar3D, PlanarBar3D, Line3D, Trisurf3D, Contour3D, Quiver3D, Voxels, Text3D, radar, waterfall, funnel, gauge, streaming line/scatter/signal/candlestick, and more.

**Native UI controls** — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Avalonia 12 and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Uno Platform render charts natively via SkiaSharp — no browser, no WebView, no SignalR required. 9 interaction modifiers: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click), crosshair (passive), hover tooltip. Toolbar state model, view history (back/forward), data cursor (click-to-pin), tick mirroring, tight margins.

**MathText** — LaTeX-like inline math in any label or title: `$\alpha^{2}$`, `$\frac{a}{b}$`, `$\sqrt{x}$`, `$\hat{x}$`, `$\mathbf{F}$`, `$\mathbb{R}$`. 96 symbol mappings (Greek, math operators, arrows, relations, set/logic, blackboard bold), fractions, square roots, accents, font variants, spacing, and scaling delimiters.

**3-D charts** — 12 series types: Surface, Scatter3D, Bar3D, PlanarBar3D, Line3D, Trisurf3D (Delaunay), Contour3D (marching squares), Quiver3D (vector field), Voxels (face-culled cubes), Text3D (annotations). Full `Projection3D` pipeline, `DepthQueue3D` painter's algorithm, `LightingHelper` shading, `Svg3DRotationScript` client-side rotation with depth re-sorting, configurable `Pane3DConfig` (floor/wall colors), and 3D colorbar support.

**Streaming & Realtime** — `StreamingLineSeries`, `StreamingScatterSeries`, `StreamingSignalSeries`, `StreamingCandlestickSeries` backed by `DoubleRingBuffer` with `AppendPoint(x, y)`. `StreamingFigure` provides throttled re-rendering and auto-scaling axes (`SlidingWindow`, `StickyRight`, `AutoScale`). 11 streaming indicators (SMA, EMA, RSI, Bollinger, MACD, OBV, ATR, Stochastic, WilliamsR, CCI, VWAP) auto-attach to candlestick data. Streaming controls for Avalonia, Uno, MAUI, Blazor, and ASP.NET Core. SVG diff engine for bandwidth optimization. Rx `IObservable<T>` adapter.

**Geographic projections** — `MatPlotLibNet.Geo` package with 5 map projections (PlateCarree, Mercator, Robinson, Orthographic, LambertConformal), GeoJSON parser, Natural Earth 110m embedded data, and `GeoPolygonSeries` for coastlines/borders/choropleth. Symlog axis scale for data spanning positive and negative ranges.

**Bidirectional SignalR** — server-authoritative interactive charts with mutation events (zoom, pan, reset, legend toggle) and notification events (brush-select, hover). Stacked-record event hierarchy, natural coalescing, per-caller hover responses.

**104 colormaps** — viridis, plasma, turbo, coolwarm, and 100 more. NumPy-style SIMD numerics (`Vec`, `Mat`, `Linalg`, `Fft`). Accessibility (ARIA, keyboard, Okabe-Ito palette, high-contrast theme). Matplotlib look-alike themes. DataFrame integration with 16 financial indicators. Broken axes. Publication-quality SVG/PNG/PDF/GIF export.

---

## License

[MIT](LICENSE) — free for any use, open-source or commercial, with no copyleft conditions.
