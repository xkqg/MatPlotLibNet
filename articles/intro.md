# MatPlotLibNet Documentation

**matplotlib for .NET.** A .NET 10 / .NET 8 charting library that tracks matplotlib's API shape: 74 series types, 104 colormaps, 26 themes, 13 map projections with embedded Natural Earth data, MathText with operator limits + matrices, parallel SVG rendering, and polymorphic export (SVG / PNG / PDF / GIF). Native UI controls for Blazor / Avalonia / Uno / WPF / MAUI. 13 NuGet packages + 3 npm bindings.

v1.9.0 is a pure indicator-expansion release. 12 new indicators extend the quant-finance library from 40 to **52 production-grade indicators**:

- **Tier 3a Volume / Money Flow** (4) — `KlingerVolumeOscillator`, `TwiggsMoneyFlow`, `EaseOfMovement`, `VwapZScore`
- **Tier 3b Trend / Transform** (4) — `Supertrend`, `CgOscillator`, `InverseFisherTransform`, `YangZhangVolRatio`
- **Tier 3c Advanced / Cross-asset** (4) — `EhlersITrend`, `Decycler`, `EhlersSuperSmoother` (public), `TransferEntropy`

All 622 classes remain at ≥90/90 line/branch coverage under the strict gate. No framework refactors this release — rendering paths are unchanged, SVG output byte-identical to shipped v1.8.0. See [CHANGELOG](https://github.com/xkqg/MatPlotLibNet/blob/main/CHANGELOG.md) for per-indicator details.

## Documentation

Full documentation is on the **[GitHub Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)**:

| Wiki page | Covers |
|---|---|
| [Getting Started](https://github.com/xkqg/MatPlotLibNet/wiki/Getting-Started) | Installation, quick start, output formats |
| [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types) | All 74 series types with code examples |
| [Matplotlib Themes](https://github.com/xkqg/MatPlotLibNet/wiki/MatplotlibThemes) | 26 theme presets + custom theme authoring |
| [Fluent Cheatsheet](https://github.com/xkqg/MatPlotLibNet/wiki/Fluent-Cheatsheet) | One-page reference of the fluent builder API |
| [Keyboard Shortcuts](https://github.com/xkqg/MatPlotLibNet/wiki/Keyboard-Shortcuts) | Browser-interactive pan / zoom / rotate / toggle |
| [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling) | Themes, colormaps, PropCycler |
| [Interactive Controls](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) | SVG pan/zoom/tooltips, 3D rotation, treemap expand/collapse, Sankey hover |
| [Bidirectional SignalR](https://github.com/xkqg/MatPlotLibNet/wiki/Bidirectional-SignalR) | Server-side interaction with live figure re-render |
| [Accessibility](https://github.com/xkqg/MatPlotLibNet/wiki/Accessibility) | ARIA, colorblind-safe palettes, high-contrast theme |
| [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced) | Layouts, date axes, math text, animations, real-time |
| [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map) | All 13 NuGet packages + 3 npm bindings in detail |
| [Notebooks](https://github.com/xkqg/MatPlotLibNet/wiki/Notebooks) | Polyglot Notebooks + Jupyter inline rendering |
| [Benchmarks](https://github.com/xkqg/MatPlotLibNet/wiki/Benchmarks) | SVG rendering, SIMD transforms, indicators, PNG/PDF |
| [Roadmap](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap) | Version history and planned phases |
| [Contributing](https://github.com/xkqg/MatPlotLibNet/wiki/Contributing) | Build, test, coding conventions |

## Samples

Runnable sample projects are in the [Samples/](../Samples/) directory:

- **[Playground](../Samples/MatPlotLibNet.Playground/)** — Blazor WASM interactive explorer. Switch between 16 example charts, flip themes, toggle series styling, copy the generated C# — hosted live at [xkqg.github.io/MatPlotLibNet/playground](https://xkqg.github.io/MatPlotLibNet/playground/)
- **[Console](../Samples/MatPlotLibNet.Samples.Console/)** — export charts to SVG, PNG, PDF; subplots; themes; JSON round-trip; generates every image in the wiki / cookbook
- **[Blazor](../Samples/MatPlotLibNet.Samples.Blazor/)** — static and real-time charts with SignalR
- **[WPF](../Samples/MatPlotLibNet.Samples.Wpf/)** — native WPF window with `MplChartControl`, 4 chart types + live interactive toggle
- **[Avalonia](../Samples/MatPlotLibNet.Samples.Avalonia/)** — native Avalonia control (Windows / macOS / Linux)
- **[Uno](../Samples/MatPlotLibNet.Samples.Uno/)** — Uno Platform control (Windows / macOS / Linux / WebAssembly / iOS / Android)
- **[AspNetCore](../Samples/MatPlotLibNet.Samples.AspNetCore/)** — server-side figure registry + SignalR hub for round-trip interactivity
- **[WebApi](../Samples/MatPlotLibNet.Samples.WebApi/)** — REST endpoints + SignalR hub for Angular / React / Vue frontends
- **[GraphQL](../Samples/MatPlotLibNet.Samples.GraphQL/)** — HotChocolate playground with queries and subscriptions

See [Samples/README.md](../Samples/README.md) for `dotnet run` instructions.

## Architecture

See [ARCHITECTURE.md](../Src/MatPlotLibNet/ARCHITECTURE.md) for the full rendering pipeline, data flow, and design patterns.

## API Reference

Browse the [API Reference](../api/index.md) for detailed class and method documentation generated from XML doc comments.
