# MatPlotLibNet Documentation

**matplotlib for .NET.** A .NET 10 / .NET 8 charting library that tracks matplotlib's API shape: 91 series types, 148 colormaps, 30 themes, 13 map projections with embedded Natural Earth data, MathText with operator limits + matrices, parallel SVG rendering, and polymorphic export (SVG / PNG / PDF / GIF). Native UI controls for Blazor / Avalonia / Uno / WPF / MAUI. 15 NuGet packages + 3 npm bindings.

Beyond the 91 chart types, 26 chart verbs draw whole charts from your data in one call: a Pareto chart, a ridgeline, a calendar heatmap, a control chart, a Kaplan-Meier curve, a Q-Q plot and fourteen more. A verb adds no chart type of its own. It is built from series the library already has, so the figure still goes through JSON, the MCP server and every native control as those series.

For operations screens, `Plt.OpsDashboard()` composes KPI tiles, state timelines and a trend panel on one time window that you supply.

Every class is held at 90% line and 90% branch coverage by a gate that fails the build otherwise; the latest figures are in [COVERAGE.md](https://github.com/xkqg/MatPlotLibNet/blob/main/docs/COVERAGE.md). See the [CHANGELOG](https://github.com/xkqg/MatPlotLibNet/blob/main/CHANGELOG.md) for every release's per-feature details.

## Documentation

Full documentation is on the **[GitHub Wiki](https://github.com/xkqg/MatPlotLibNet/wiki)**:

| Wiki page | Covers |
|---|---|
| [Getting Started](https://github.com/xkqg/MatPlotLibNet/wiki/Getting-Started) | Installation, quick start, output formats |
| [Chart Types](https://github.com/xkqg/MatPlotLibNet/wiki/Chart-Types) | All 91 series types with code examples |
| [Matplotlib Themes](https://github.com/xkqg/MatPlotLibNet/wiki/MatplotlibThemes) | 30 theme presets + custom theme authoring |
| [Fluent Cheatsheet](https://github.com/xkqg/MatPlotLibNet/wiki/Fluent-Cheatsheet) | One-page reference of the fluent builder API |
| [Keyboard Shortcuts](https://github.com/xkqg/MatPlotLibNet/wiki/Keyboard-Shortcuts) | Browser-interactive pan / zoom / rotate / toggle |
| [Styling](https://github.com/xkqg/MatPlotLibNet/wiki/Styling) | Themes, colormaps, PropCycler |
| [Interactive Controls](https://github.com/xkqg/MatPlotLibNet/wiki/Interactive-Controls) | SVG pan/zoom/tooltips, 3D rotation, treemap expand/collapse, Sankey hover |
| [Bidirectional SignalR](https://github.com/xkqg/MatPlotLibNet/wiki/Bidirectional-SignalR) | Server-side interaction with live figure re-render |
| [Accessibility](https://github.com/xkqg/MatPlotLibNet/wiki/Accessibility) | ARIA, colorblind-safe palettes, high-contrast theme |
| [Advanced](https://github.com/xkqg/MatPlotLibNet/wiki/Advanced) | Layouts, date axes, math text, animations, real-time |
| [Package Map](https://github.com/xkqg/MatPlotLibNet/wiki/Package-Map) | All 15 NuGet packages + 3 npm bindings in detail |
| [Notebooks](https://github.com/xkqg/MatPlotLibNet/wiki/Notebooks) | Polyglot Notebooks + Jupyter inline rendering. The runtime underneath has been ended by Microsoft; new work belongs in MatPlotLibNet.Verso, the extension for the notebook built to replace it |
| [Benchmarks](https://github.com/xkqg/MatPlotLibNet/wiki/Benchmarks) | SVG rendering, SIMD transforms, indicators, PNG/PDF |
| [Roadmap](https://github.com/xkqg/MatPlotLibNet/wiki/Roadmap) | Version history and planned phases |
| [Contributing](https://github.com/xkqg/MatPlotLibNet/wiki/Contributing) | Build, test, coding conventions |

## Samples

Runnable sample projects are in the [Samples/](../Samples/) directory:

- **[Playground](../Samples/MatPlotLibNet.Playground/)** — Blazor WASM interactive explorer. Switch between 22 example charts, flip themes, toggle series styling, copy the generated C# — hosted live at [xkqg.github.io/MatPlotLibNet/playground](https://xkqg.github.io/MatPlotLibNet/playground/)
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
