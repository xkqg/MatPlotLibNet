# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel SVG rendering, polymorphic export (SVG / PNG / PDF / GIF), and multi-platform output to Blazor, MAUI, Avalonia, Uno Platform, ASP.NET Core, Angular, React, and Vue.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/MatPlotLibNet)](https://www.nuget.org/packages/MatPlotLibNet)
[![Version](https://img.shields.io/badge/version-1.3.0-blue)](CHANGELOG.md)

> **v1.3.0 — Cross-platform native UI controls + MathText completion + 3-D round 2.** Three headline features:
>
> 1. **Native controls** — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) (Avalonia 12) and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) (Uno Platform) render charts natively via SkiaSharp — no browser, no WebView, no SignalR. `IsInteractive="True"` enables local pan / zoom / reset / brush-select with rubber-band overlay, legend-toggle hit-testing, and hover tooltips via `NearestPointFinder`. `.WithServerInteraction(hubConnection)` bridges native controls to a SignalR hub. Two new NuGet packages: [`MatPlotLibNet.Avalonia`](https://www.nuget.org/packages/MatPlotLibNet.Avalonia) and [`MatPlotLibNet.Uno`](https://www.nuget.org/packages/MatPlotLibNet.Uno).
> 2. **MathText completion** — `\frac{a}{b}`, `\sqrt{x}`, `\sqrt[n]{x}`, accents (`\hat`, `\bar`, `\vec`, `\tilde`, `\dot`), font variants (`\mathrm`, `\mathbf`, `\mathit`, `\mathcal`, `\mathbb`), `\text{}`, spacing (`\,`, `\:`, `\;`, `\quad`), scaling delimiters (`\left(...\right)`), and 45+ new symbol mappings (blackboard bold, arrows, relations, set operators).
> 3. **3-D round 2** — six new series: `Line3D`, `Trisurf3D` (Delaunay), `Contour3D` (marching squares), `Quiver3D` (vector field), `Voxels` (face-culled cubes), `Text3D` (3D annotations). Series count: 61 → 67.
>
> **4 028 tests green** across 11 test projects. The managed interaction layer in core (six `IInteractionModifier` implementations, `InteractionController`, `ChartLayout`) is shared between desktop controls and SignalR — one vocabulary, two transports.
>
> **v1.2.2 — Brush-select + hover round-trip (deferred v1.2.0 items).** v1.2.0 shipped four mutation events (Zoom, Pan, Reset, LegendToggle) that rewrite the authoritative `Figure` and broadcast the updated SVG. v1.2.2 introduces the first two **notification events** — `BrushSelectEvent` and `HoverEvent` — that observe the user's gesture, route it to a per-chart handler in .NET code, and (for hover) return a caller-only response. The architectural move: a new tier-2 abstract record `FigureNotificationEvent` with `sealed override ApplyTo` makes "a notification event accidentally mutates the figure" structurally impossible. Brush-select is fire-and-forget (`ChartSessionOptions.OnBrushSelect`); hover is request-response returning HTML, delivered to the **originating client only** via `IChartHubClient.ReceiveTooltipContent` — the first per-caller mechanism in the library (v1.2.0 only had group broadcast). Backward compatible: v1.2.0/v1.2.1 code paths untouched. **3 519 passing** across 7 test projects (+23 core, +10 AspNetCore including 4 real-SignalR round-trip tests with two connected clients verifying caller-only responses).
>
> **v1.2.1 — Font-factory subsystem fix + zero-warning CI sweep.** The outside-legend clipping bug that v1.1.4's changelog claimed to have fixed turned out to have a deeper root cause: eight duplicate themed-font factories across `AxesRenderer`, `ChartRenderer`, `ConstrainedLayoutEngine`, and `LegendMeasurer` had silently drifted (engine used `DefaultFont.Size − 2`, renderer used `DefaultFont.Size`). v1.2.1 consolidates them into one `ThemedFontProvider` — a single source of truth that makes the drift bug class structurally impossible. Seven new drift-regression tests cover every measurer/renderer pair. Every non-MAUI library now builds at **0 warnings / 0 errors** (21 warnings cleared, 5 stale `Color.Blue/Orange` references fixed in WebApi + GraphQL samples, 16 xUnit1051 warnings refactored to `TestContext.Current.CancellationToken`). **3 641 tests green** across 7 test projects.
>
> **v1.2.0 — Bidirectional SignalR interactive charts.** Browser wheel-zoom, drag-pan, reset, and legend-toggle round-trip through `ChartHub` to a server-authoritative `Figure` that is mutated on a per-chart channel-drained background task and re-published through the existing SignalR fan-out. Pure .NET, no JavaScript charting library, server stays the source of truth. See [MatPlotLibNet.AspNetCore README](Src/MatPlotLibNet.AspNetCore/README.md#bidirectional-signalr-v120) or the `Samples/MatPlotLibNet.Samples.AspNetCore` + `Interactive.razor` demos.

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

**67 series types** — line, scatter, bar, histogram, pie, box, violin, heatmap, contour, candlestick, OHLC, treemap, sunburst, Sankey, polar, polar heatmap, 3D surface, Bar3D, PlanarBar3D, Line3D, Trisurf3D, Contour3D, Quiver3D, Voxels, Text3D, radar, waterfall, funnel, gauge, and more.

**Native UI controls** (v1.3.0) — [`MplChartControl`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Avalonia 12 and [`MplChartElement`](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) for Uno Platform render charts natively via SkiaSharp — no browser, no WebView, no SignalR required. Set `IsInteractive="True"` for local pan / zoom / reset / brush-select using the managed interaction layer (`InteractionController`, six `IInteractionModifier` implementations, `ChartLayout` coordinate transform). Connect to a SignalR hub with `.WithServerInteraction(hubConnection)` for server-authoritative mode.

**MathText** — LaTeX-like inline math in any label or title: `$\alpha^{2}$`, `$\frac{a}{b}$`, `$\sqrt{x}$`, `$\hat{x}$`, `$\mathbf{F}$`, `$\mathbb{R}$`. Supports Greek letters, super/subscript, fractions, square roots (with optional index), accents (`\hat`, `\bar`, `\vec`, `\tilde`, `\dot`), font variants (`\mathrm`, `\mathbf`, `\mathit`, `\mathcal`, `\mathbb`), `\text{}` for roman text inside math, spacing (`\,`, `\:`, `\;`, `\quad`), scaling delimiters (`\left(...\right)`), and 90+ symbol mappings.

**3-D charts** — six series types added in v1.3.0: `Line3D` (projected polyline), `Trisurf3D` (Delaunay triangulated surface), `Contour3D` (marching-squares contour lines projected onto 3D planes), `Quiver3D` (3D vector field with arrow mesh), `Voxels` (volumetric cube rendering with face culling), `Text3D` (3D annotations). All share the existing `Projection3D` pipeline, `DepthQueue3D` painter's algorithm, and `Svg3DRotationScript` client-side rotation.

**Bidirectional SignalR** (v1.2.0, extended in v1.2.2) — `FigureBuilder.WithServerInteraction("chart-id", i => i.All())` opts a figure into server-authoritative interaction. The embedded dispatcher script invokes `ChartHub.OnZoom` / `OnPan` / `OnReset` / `OnLegendToggle` (mutation events, v1.2.0) and `OnBrushSelect` / `OnHover` (notification events, v1.2.2). Mutation events flow through a stacked-record hierarchy (`ZoomEvent : AxisRangeEvent : FigureInteractionEvent`) to a `FigureRegistry`-owned background reader task that rewrites the figure and re-publishes the SVG; notification events stack under `FigureNotificationEvent` (tier-2, `sealed override ApplyTo` no-op) and route to per-chart handlers via `ChartSessionOptions.OnBrushSelect` / `OnHover`. Hover returns HTML to the **originating client only** via `IChartHubClient.ReceiveTooltipContent`. Natural coalescing — bursts of wheel or mousemove events produce exactly one re-render / one response per batch. First-class Blazor + raw-HTML samples ship in `Samples/`.

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
- [Fluent Cheatsheet](https://github.com/xkqg/MatPlotLibNet/wiki/Fluent-Cheatsheet) — one-page reference for `Plt` / `FigureBuilder` / `AxesBuilder`
- [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map) — all 12 NuGet + 3 npm packages in detail
- [DataFrame](https://github.com/xkqg/MatPlotLibNet/wiki/DataFrame) — charting, 16 indicators, and polynomial regression from `Microsoft.Data.Analysis.DataFrame`
- [Notebooks](https://github.com/xkqg/MatPlotLibNet/wiki/Notebooks) — Polyglot Notebooks + Jupyter inline rendering
- [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types) — all 67 series with code examples
- [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling) — themes, colormaps, PropCycler
- [Matplotlib Themes](https://github.com/xkqg/MatPlotLibNet/wiki/MatplotlibThemes) — `Theme.MatplotlibClassic` and `Theme.MatplotlibV2` look-alikes
- [Accessibility](https://github.com/xkqg/MatPlotLibNet/wiki/Accessibility) — SVG semantics, keyboard navigation, color-blind palette, high-contrast theme
- [Interactive Controls](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) — Avalonia + Uno native controls, managed interaction layer, server mode adapter
- [Bidirectional SignalR](https://github.com/xkqg/MatPlotLibNet/wiki/Bidirectional-SignalR) — server-authoritative interactive charts, event hierarchy, `FigureRegistry`, hub wiring
- [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced) — date axes, math text, animations, GIF, real-time
- [Benchmarks](https://github.com/xkqg/MatPlotLibNet/wiki/Benchmarks) — SVG rendering, SIMD transforms, indicators
- [Roadmap](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap) — version history and planned phases
- [Contributing](https://github.com/xkqg/MatPlotLibNet/wiki/Contributing) — build, test, coding conventions

---

## License

[MIT](LICENSE) — free for any use, open-source or commercial, with no copyleft conditions.
