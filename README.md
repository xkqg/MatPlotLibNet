# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel SVG rendering, polymorphic export (SVG / PNG / PDF / GIF), and multi-platform output to Blazor, MAUI, Avalonia, Uno Platform, ASP.NET Core, Angular, React, and Vue.

[![CI](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml/badge.svg)](https://github.com/xkqg/MatPlotLibNet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/xkqg/MatPlotLibNet)](https://github.com/xkqg/MatPlotLibNet)

> **v1.3.0 — Cross-platform native UI controls + MathText completion + 3-D round 2.** Three headline features:
>
> 1. **Native controls** — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) (Avalonia 12) and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) (Uno Platform) render charts natively via SkiaSharp — no browser, no WebView, no SignalR. `IsInteractive="True"` enables local pan / zoom / reset / brush-select with rubber-band overlay, legend-toggle hit-testing, and hover tooltips via `NearestPointFinder`. `.WithServerInteraction(hubConnection)` bridges native controls to a SignalR hub. Two new NuGet packages: [`MatPlotLibNet.Avalonia`](https://www.nuget.org/packages/MatPlotLibNet.Avalonia) and [`MatPlotLibNet.Uno`](https://www.nuget.org/packages/MatPlotLibNet.Uno).
> 2. **MathText completion** — `\frac{a}{b}`, `\sqrt{x}`, `\sqrt[n]{x}`, accents (`\hat`, `\bar`, `\vec`, `\tilde`, `\dot`), font variants (`\mathrm`, `\mathbf`, `\mathit`, `\mathcal`, `\mathbb`), `\text{}`, spacing (`\,`, `\:`, `\;`, `\quad`), scaling delimiters (`\left(...\right)`), and 96 symbol mappings total (Greek, math operators, arrows, relations, set/logic, blackboard bold).
> 3. **3-D round 2** — six new series: `Line3D`, `Trisurf3D` (Delaunay), `Contour3D` (marching squares), `Quiver3D` (vector field), `Voxels` (face-culled cubes), `Text3D` (3D annotations). Series count: 61 → 67.
>
> **4 028 tests green** across 11 test projects. The managed interaction layer in core (six `IInteractionModifier` implementations, `InteractionController`, `ChartLayout`) is shared between desktop controls and SignalR — one vocabulary, two transports.
>
> For earlier releases, see the [full CHANGELOG](CHANGELOG.md).

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
| **MatPlotLibNet.Notebooks** | `#r "nuget: MatPlotLibNet.Notebooks"` | Inline SVG in Polyglot / Jupyter notebooks |
| **@matplotlibnet/angular** | `npm install @matplotlibnet/angular` | Angular components + TypeScript SignalR client |
| **@matplotlibnet/react** | `npm install @matplotlibnet/react` | React hooks + components + TypeScript SignalR client |
| **@matplotlibnet/vue** | `npm install @matplotlibnet/vue` | Vue 3 composables + TypeScript SignalR client |

---

## Documentation

Full documentation is on the **[GitHub Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)**, the **[Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/)** (runnable examples with rendered images), and the **[API Reference](https://xkqg.github.io/MatPlotLibNet/api/)** (generated from XML doc comments):

- [Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/) — copy-paste code examples with rendered output for every chart type
- [API Reference](https://xkqg.github.io/MatPlotLibNet/api/) — full API documentation from source
- [Getting Started](https://github.com/xkqg/MatPlotLibNet/wiki/Getting-Started) — installation, output formats, subplots
- [Fluent Cheatsheet](https://github.com/xkqg/MatPlotLibNet/wiki/Fluent-Cheatsheet) — one-page reference for `Plt` / `FigureBuilder` / `AxesBuilder`
- [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map) — all 11 NuGet + 3 npm packages in detail
- [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types) — all 67 series with code examples
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

**67 series types** — line, scatter, bar, histogram, pie, box, violin, heatmap, contour, candlestick, OHLC, treemap, sunburst, Sankey, polar, polar heatmap, 3D surface, Bar3D, PlanarBar3D, Line3D, Trisurf3D, Contour3D, Quiver3D, Voxels, Text3D, radar, waterfall, funnel, gauge, and more.

**Native UI controls** — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Avalonia 12 and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Uno Platform render charts natively via SkiaSharp — no browser, no WebView, no SignalR required. Set `IsInteractive="True"` for local pan / zoom / reset / brush-select. Connect to a SignalR hub with `.WithServerInteraction(hubConnection)` for server-authoritative mode.

**MathText** — LaTeX-like inline math in any label or title: `$\alpha^{2}$`, `$\frac{a}{b}$`, `$\sqrt{x}$`, `$\hat{x}$`, `$\mathbf{F}$`, `$\mathbb{R}$`. 96 symbol mappings (Greek, math operators, arrows, relations, set/logic, blackboard bold), fractions, square roots, accents, font variants, spacing, and scaling delimiters.

**3-D charts** — 12 series types: Surface, Scatter3D, Bar3D, PlanarBar3D, Line3D, Trisurf3D (Delaunay), Contour3D (marching squares), Quiver3D (vector field), Voxels (face-culled cubes), Text3D (annotations). Full `Projection3D` pipeline, `DepthQueue3D` painter's algorithm, `LightingHelper` shading, and `Svg3DRotationScript` client-side rotation.

**Bidirectional SignalR** — server-authoritative interactive charts with mutation events (zoom, pan, reset, legend toggle) and notification events (brush-select, hover). Stacked-record event hierarchy, natural coalescing, per-caller hover responses.

**104 colormaps** — viridis, plasma, turbo, coolwarm, and 100 more. NumPy-style SIMD numerics (`Vec`, `Mat`, `Linalg`, `Fft`). Accessibility (ARIA, keyboard, Okabe-Ito palette, high-contrast theme). Matplotlib look-alike themes. DataFrame integration with 16 financial indicators. Broken axes. Publication-quality SVG/PNG/PDF/GIF export.

---

## License

[MIT](LICENSE) — free for any use, open-source or commercial, with no copyleft conditions.
