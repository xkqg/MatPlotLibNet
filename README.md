# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel server-side SVG rendering, polymorphic export (SVG / PNG / PDF / animated GIF), and 78 series types across line, scatter, bar, 3D, streaming, polar, financial, statistical, hierarchical, Sankey, and vector families. Ships with 13 map projections and embedded Natural Earth data, 26 themes, LaTeX-style MathText, O(1) streaming with **52 technical indicators** (classical moving averages + volatility, momentum, trend-follower, cycle, microstructure, entropy, change-point, and cross-asset causality), financial drawing tools (trendlines, Fibonacci retracements, horizontal levels), frame-based animation with 6 easing curves + Pause/Resume playback, native UI controls for **Blazor, WPF, MAUI, Avalonia, Uno Platform, ASP.NET Core**, and TypeScript clients for **Angular, React, and Vue** — no JavaScript framework, no WebView, no SaaS.

[![CI](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml/badge.svg)](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/xkqg/MatPlotLibNet)](https://github.com/xkqg/MatPlotLibNet)

## 🧭 What's next

**v1.10.0 (2026-05-04) — Pair-Selection Visualisation Pack** ships the full 5-phase chart-type machinery for correlation-based asset clustering, dimensional EDA, and hierarchical risk parity. Future releases are **community-driven**:

- 🗺️ **Phase 1 — Annotated & triangular-mask heatmaps**: four new `HeatmapSeries` properties (`ShowLabels`, `LabelFormat`, `MaskMode`, `CellValueColor`) + `HeatmapMaskMode` enum unblock every realistic correlation-matrix figure.
- 🌲 **Phase 2 — `DendrogramSeries`**: hierarchical-clustering tree as canonical "U"-shape segments. Four `DendrogramOrientation` values (`Top`, `Bottom`, `Left`, `Right`); optional `CutHeight` draws a dashed reference line and recolours each cluster below the cut from a qualitative `IColorMap` (default `Tab10`). Fluent API: `Plt.Create().Dendrogram(root, s => s.CutHeight = 1.5)`.
- 🔥 **Phase 3 — `ClustermapSeries`**: composite heatmap with optional row/column dendrograms (the seaborn `clustermap` idiom). Automatic data-matrix reordering to align cells visually with the leaf order.
- 📊 **Phase 4 — `PairGridSeries`**: multi-panel scatter matrix (the seaborn `pairplot` idiom). N×N grid: histogram or KDE on the diagonal, scatter or hexbin on the off-diagonal, optional hue groups + `string[]?` `HueLabels` for category-aware EDA.
- 🔷 **Phase 5 — Hexbin off-diagonal**: activates `PairGridOffDiagonalKind.Hexbin = 2` for high-cardinality EDA where scatter overplots (~1000+ samples per cell).
- 🧹 **Convergence sweep**: `IColormappable.GetColorMapOrDefault(fallback)` and `int.ColormapFraction(count)` extensions collapse 17 + 3 inline duplications across renderers into one place; shared `HierarchicalLayout.MinPanelPx` and `Numerics/HistogramBinning` source-of-truth.
- 🗺️ **v1.10.0 heatmap extensions** (released):
  - **`ShowLabels` / `LabelFormat`** (`ILabelable`) — render each cell's value on top of the fill; any .NET numeric format string (e.g. `"P1"`, `"F0"`)
  - **`MaskMode`** — `HeatmapMaskMode` enum hides redundant cells in symmetric matrices (`UpperTriangle`, `LowerTriangle`, and strict variants that include the diagonal)
  - **`CellValueColor`** — explicit label colour; auto black/white contrast via `Color.ContrastingTextColor()` (Rec. 709) when null
- 🐛 **Bug fixes** — driven by community use and the strict `≥90/90` per-class coverage gate (all classes pass; **8 707 core tests**).
- 📚 **Documentation polish** — cookbook examples, API XML doc completeness.
- 🌱 **Listening** — Open a [Discussion](https://github.com/xkqg/MatPlotLibNet/discussions) or [Issue](https://github.com/xkqg/MatPlotLibNet/issues) with what's missing for your use case. The next direction will be guided by what real users need, not by a feature checklist.

For the full v1.10.0 release notes, see the [CHANGELOG](CHANGELOG.md).

---

## Documentation

Full documentation is on the **[GitHub Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)**, the **[Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/)** (runnable examples with rendered images), and the **[API Reference](https://xkqg.github.io/MatPlotLibNet/api/)** (generated from XML doc comments):

- [Playground](https://xkqg.github.io/MatPlotLibNet/playground/) — try charts live in the browser — pick an example, tweak parameters, see the SVG update instantly
- [Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/) — copy-paste code examples with rendered output for every chart type
- [API Reference](https://xkqg.github.io/MatPlotLibNet/api/) — full API documentation from source
- [Getting Started](https://github.com/xkqg/MatPlotLibNet/wiki/Getting-Started) — installation, output formats, subplots
- [Fluent Cheatsheet](https://github.com/xkqg/MatPlotLibNet/wiki/Fluent-Cheatsheet) — one-page reference for `Plt` / `FigureBuilder` / `AxesBuilder`
- [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map) — all 13 NuGet + 3 npm packages in detail
- [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types) — all 78 series with code examples
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

**78 series types** — line, scatter, bar, histogram, pie, box, violin, heatmap, contour, candlestick, OHLC, treemap, sunburst, Sankey, polar, polar heatmap, 3D surface, Bar3D, PlanarBar3D, Line3D, Trisurf3D, Contour3D, Quiver3D, Voxels, Text3D, radar, waterfall, funnel, gauge, pair grid, streaming line/scatter/signal/candlestick, and more.

**Native UI controls** — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Avalonia 12 and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Uno Platform render charts natively via SkiaSharp — no browser, no WebView, no SignalR required. 9 interaction modifiers: pan (drag), zoom (scroll), 3D rotation (right-drag), rectangle zoom (Ctrl+drag), brush select (Shift+drag), span select (Alt+drag), legend toggle (click), crosshair (passive), hover tooltip. Toolbar state model, view history (back/forward), data cursor (click-to-pin), tick mirroring, tight margins.

**MathText** — LaTeX-like inline math in any label or title: `$\alpha^{2}$`, `$\frac{a}{b}$`, `$\sqrt{x}$`, `$\hat{x}$`, `$\mathbf{F}$`, `$\mathbb{R}$`. 96 symbol mappings (Greek, math operators, arrows, relations, set/logic, blackboard bold), fractions, square roots, accents, font variants, spacing, and scaling delimiters.

**3-D charts** — 12 series types: Surface, Scatter3D, Bar3D, PlanarBar3D, Line3D, Trisurf3D (Delaunay), Contour3D (marching squares), Quiver3D (vector field), Voxels (face-culled cubes), Text3D (annotations). Full `Projection3D` pipeline, `DepthQueue3D` painter's algorithm, `Vec3.FaceNormal` + `Color.Shade()`/`Color.Modulate()` extension-based shading, `Svg3DRotationScript` client-side rotation with depth re-sorting, configurable `Pane3DConfig` (floor/wall colors), and 3D colorbar support.

**Streaming & Realtime** — `StreamingLineSeries`, `StreamingScatterSeries`, `StreamingSignalSeries`, `StreamingCandlestickSeries` backed by `DoubleRingBuffer` with `AppendPoint(x, y)`. `StreamingFigure` provides throttled re-rendering and auto-scaling axes (`SlidingWindow`, `StickyRight`, `AutoScale`). 11 streaming indicators (SMA, EMA, RSI, Bollinger, MACD, OBV, ATR, Stochastic, WilliamsR, CCI, VWAP) auto-attach to candlestick data. Streaming controls for Avalonia, Uno, MAUI, Blazor, and ASP.NET Core. SVG diff engine for bandwidth optimization. Rx `IObservable<T>` adapter.

**Geographic projections** — `MatPlotLibNet.Geo` package with 5 map projections (PlateCarree, Mercator, Robinson, Orthographic, LambertConformal), GeoJSON parser, Natural Earth 110m embedded data, and `GeoPolygonSeries` for coastlines/borders/choropleth. Symlog axis scale for data spanning positive and negative ranges.

**Bidirectional SignalR** — server-authoritative interactive charts with mutation events (zoom, pan, reset, legend toggle) and notification events (brush-select, hover). Stacked-record event hierarchy, natural coalescing, per-caller hover responses.

**104 colormaps** — viridis, plasma, turbo, coolwarm, and 100 more. NumPy-style SIMD numerics (`Vec`, `Mat`, `Linalg`, `Fft`). Accessibility (ARIA, keyboard, Okabe-Ito palette, high-contrast theme). Matplotlib look-alike themes. DataFrame integration with **52 technical indicators** (v1.9.0 adds 12 — Klinger, Twiggs MF, Ease of Movement, VWAP Z-Score, Supertrend, CG Oscillator, Inverse Fisher, YZ Vol Ratio, Ehlers iTrend, Decycler, Ehlers SuperSmoother, Transfer Entropy). Broken axes. Publication-quality SVG/PNG/PDF/GIF export.

---

## License

[MIT](LICENSE) — free for any use, open-source or commercial, with no copyleft conditions.
