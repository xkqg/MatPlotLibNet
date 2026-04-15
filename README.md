# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel SVG rendering, polymorphic export (SVG / PNG / PDF / GIF), and multi-platform output to Blazor, MAUI, ASP.NET Core, Angular, React, and Vue.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![Version](https://img.shields.io/badge/version-1.2.0-blue)](CHANGELOG.md)

> **v1.2.0 — Bidirectional SignalR interactive charts.** Browser wheel-zoom, drag-pan, reset, and legend-toggle now round-trip through `ChartHub` to a server-authoritative `Figure` that is mutated on a per-chart channel-drained background task and re-published through the existing SignalR fan-out. Pure .NET, no JavaScript charting library, server stays the source of truth. See [MatPlotLibNet.AspNetCore README](Src/MatPlotLibNet.AspNetCore/README.md#bidirectional-signalr-v120) or the `Samples/MatPlotLibNet.Samples.AspNetCore` + `Interactive.razor` demos.

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

**61 series types** — line, scatter, bar, histogram, pie, box, violin, heatmap, contour, candlestick, OHLC, treemap, sunburst, Sankey, polar, polar heatmap, 3D surface, Bar3D, PlanarBar3D, radar, waterfall, funnel, gauge, and more.

**Bidirectional SignalR** (v1.2.0) — `FigureBuilder.WithServerInteraction("chart-id", i => i.All())` opts a figure into server-authoritative interaction. The embedded dispatcher script invokes `ChartHub.OnZoom` / `OnPan` / `OnReset` / `OnLegendToggle`, a `FigureRegistry`-owned background reader task mutates the figure through a stacked-record event hierarchy (`ZoomEvent : AxisRangeEvent : FigureInteractionEvent`), and the updated SVG streams back through the existing publish fan-out. Natural coalescing — bursts of wheel events produce exactly one re-render per batch. Works with any `@microsoft/signalr` host page; first-class Blazor sample ships in `Samples/MatPlotLibNet.Samples.Blazor`.

**Accessibility** — SVG exports carry `role="img"`, `<title>`/`<desc>`, and ARIA labels on all structural groups; all 5 interactive JS features are keyboard-navigable; Okabe-Ito color-blind safe palette (`Theme.ColorBlindSafe`); WCAG AAA high-contrast theme (`Theme.HighContrast`).

**Matplotlib look-alike themes** — `Theme.MatplotlibClassic` mimics matplotlib's pre-2.0 default (white background, the iconic `bgrcmyk` cycle, DejaVu Sans 12pt). `Theme.MatplotlibV2` mimics the modern matplotlib default since 2017 (white background, soft-black text, the `tab10` 10-color cycle, DejaVu Sans 10pt). Drop-in matplotlib look in pure .NET — no Python runtime required.

**Series capability interfaces** — `IHasColor`, `IHasAlpha`, `IHasEdgeColor`, `ILabelable` allow polymorphic access to common series properties without casting; enables generic theming and rendering utilities.

**NumPy-style numerics** — `Mat` matrix type with SIMD operators, `Linalg` (Solve/Inv/Det/Eigh/Svd), `NpStats` (Diff/Median/Histogram/Argsort/Unique/Cov/Corrcoef), `NpRandom` (Normal/Uniform/Lognormal/Integers), and `Fft.Inverse`/`Frequencies`/`Shift` — all zero new dependencies, pure C# + TensorPrimitives.

**Broken / discontinuous axis** — `AxisBreak` + `BreakStyle` (`Zigzag`, `Straight`, `None`); `WithXBreak` / `WithYBreak` on the fluent builder; visual markers drawn at break boundaries.

**DataFrame indicator + regression bridges** — `MatPlotLibNet.DataFrame` now includes `DataFrameIndicatorExtensions` (16 methods: SMA, EMA, RSI, Bollinger, OBV, MACD, DrawDown, ADX, ATR, CCI, WilliamsR, Stochastic, ParabolicSar, KeltnerChannels, VWAP) and `DataFrameNumericsExtensions` (PolyFit, PolyEval, ConfidenceBand) — compute indicators and polynomial regressions directly from named DataFrame columns.

---

## Documentation

Full documentation is on the **[GitHub Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)**:

- [Getting Started](https://github.com/xkqg/MatPlotLibNet/wiki/Getting-Started) — installation, output formats, subplots
- [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map) — all packages in detail
- [DataFrame](https://github.com/xkqg/MatPlotLibNet/wiki/DataFrame) — `MatPlotLibNet.DataFrame`: charting, indicators, and regression from `Microsoft.Data.Analysis.DataFrame`
- [Notebooks](https://github.com/xkqg/MatPlotLibNet/wiki/Notebooks) — Polyglot Notebooks + Jupyter inline rendering
- [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types) — all 61 series with examples
- [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling) — themes, colormaps, PropCycler
- [Matplotlib Themes](https://github.com/xkqg/MatPlotLibNet/wiki/MatplotlibThemes) — `Theme.MatplotlibClassic` and `Theme.MatplotlibV2` look-alikes
- [Accessibility](https://github.com/xkqg/MatPlotLibNet/wiki/Accessibility) — SVG semantics, keyboard navigation, color-blind palette, high-contrast theme
- [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced) — date axes, math text, animations, GIF, real-time
- [Benchmarks](https://github.com/xkqg/MatPlotLibNet/wiki/Benchmarks) — SVG rendering, SIMD transforms, indicators
- [Roadmap](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap) — version history and planned phases
- [Contributing](https://github.com/xkqg/MatPlotLibNet/wiki/Contributing) — build, test, coding conventions

---

## License

[MIT](LICENSE) — free for any use, open-source or commercial, with no copyleft conditions.
