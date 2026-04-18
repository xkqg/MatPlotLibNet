# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel SVG rendering, polymorphic export (SVG / PNG / PDF / GIF), and multi-platform output to Blazor, MAUI, Avalonia, Uno Platform, ASP.NET Core, Angular, React, and Vue.

[![CI](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml/badge.svg)](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/xkqg/MatPlotLibNet)](https://github.com/xkqg/MatPlotLibNet)

> **v1.7.2 — Browser-interaction subsystem hardened end-to-end (13-phase TDD plan + matplotlib-parity follow-on) + bug fixes + coverage uplift + CI hardening.** Continuation of the v1.7.1 stabilisation track. **Now the stable release.**
>
> 0. **Matplotlib-parity follow-on (Phases A–C):** **3D drag now ROTATES the camera** (was hijacked by 2D pan via `setPointerCapture` last-call-wins; fixed via 3D-script `stopPropagation` + 2D-script bails on `.mpl-3d-scene`); **full matplotlib `Projection3D` ported to JS** for first-drag visual continuity (server vs. client agree to within 1 px under both axis-infrastructure polygons and surface data quads at the playground's 20×20 sinc + `[-3, 3]` ranges); **drag math = matplotlib `_on_move`** (`dazim/delev = ±dx/w·180`); **2D wheel-zoom rate = `0.85^step`** (matplotlib `NavigationToolbar2.scroll_handler`); **`x` / `y` modifier keys** lock pan axis.
> 1. **2D scroll-wheel actually zooms** now (was passive-listener silently scrolling page); **3D rotation moves the entire scene** — axes, grid, panes, ticks, labels — not just data polygons; **3D scroll-wheel zoom** + Home-key full reset; **Pointer Events + pinch-to-zoom** for touch parity; **per-chart isolation** (eight scripts that previously cross-talked between charts on one page now self-locate via `currentScript.parentNode`).
> 2. **`FigureBuilder.WithBrowserInteraction()` now also enables 3D rotation, treemap drilldown, and sankey hover** in addition to the 2D scripts. **`Theme Comparison` cookbook image** now renders six actual themes via SkiaSharp grid composite (was six identical Default-theme renders).
> 3. **`WithInteractionTheme(InteractionTheme theme)` builder** — themable opacity / transition tokens; **URL-hash state persistence** (opt-in via `data-mpl-persist="true"`) — refresh keeps zoom/pan; **3D lighting recomputation hooks** under rotation; **original-opacity preservation** across hover cycles; **treemap "Press Esc to zoom out" hint** when drilled; **tooltip focus position** uses element bounds.
> 4. **Behavioural test harness** — Jint-backed DOM stub replaces static-emission-only test pattern with real event simulation + DOM-mutation assertions across **42 new behavioural tests**.
> 5. **6-batch coverage uplift (Phases A-F) + Phase-9 dedup** — +1 192 tests, sub-90/90 class count 241 → **154**, 14 documented exemptions added for sample / interface / JS-template code.
> 6. **CI hardening** — Skia tests now ship `SkiaSharp.NativeAssets.{Linux.NoDependencies, Win32, macOS}` so `libSkiaSharp.so` actually loads on Linux runners.
>
> **5 538 tests green** across 9 test projects covering 13 NuGet packages (was 4 275 at v1.7.1, 3 967 in v1.7.0).
>
> For earlier releases, see the [full CHANGELOG](CHANGELOG.md).

---

## 🧭 Stabilisation phase

After eleven feature releases (v1.0 → v1.7.2) MatPlotLibNet now covers the **practical 90% of matplotlib's surface**: 74 series types, 13 map projections with embedded Natural Earth data, 26 themes, MathText with operator limits and matrices, streaming with O(1) indicators, native UI controls for Blazor / Avalonia / Uno / WPF / MAUI, fidelity tests against a pinned matplotlib reference, and 13 NuGet packages.

**v1.7.1 started the stabilisation period; v1.7.2 continues it.** The focus shifts from "ship more features" to:

- 🐛 **Bug fixes only** (no new public API), driven by community use and the `≥90/90` coverage gate
- 🧪 **Test coverage uplift** (the eight-phase plan in [`docs/COVERAGE.md`](docs/COVERAGE.md)) — was 85.2% line / 68.4% branch at v1.7.0, now ≈90.9% line / 76.5% branch with 154 classes still below absolute 90/90; strict-mode flip is the next milestone
- 📚 **Documentation polish** — cookbook examples, API XML doc completeness
- 🌱 **Listening** — what should v2 be? Open a [Discussion](https://github.com/xkqg/MatPlotLibNet/discussions) or [Issue](https://github.com/xkqg/MatPlotLibNet/issues) with what's missing for your use case. The next major direction will be guided by what real users need, not by a feature checklist.

No timeline for v1.8.0 yet — when it ships, it will be community-driven.

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
