# API Reference

Full API documentation generated from XML doc comments in the MatPlotLibNet source code.

Browse namespaces in the sidebar, or use the search bar to find specific types, methods, or properties.

## Key entry points

| Type | Description |
|---|---|
| `Plt` | Static entry point — `Plt.Create()` starts the fluent builder |
| `FigureBuilder` | Top-level builder: title, theme, size, subplots, export |
| `AxesBuilder` | Per-subplot builder: series, labels, legends, ticks, camera |
| `CategoricalAxesExtensions`, `DistributionAxesExtensions`, `XYAxesExtensions`, `GridAxesExtensions`, `PriceAxesExtensions`, `SignalAxesExtensions` | 38 chart verbs on `AxesBuilder` that draw a whole chart from your data in one call, such as `Pareto`, `Ridgeline`, `CalendarHeatmap`, `KaplanMeier`, `BumpChart`, `Stairs` and `Psd`. Each is built from series the library already has, so it adds no series type and goes through JSON as those series |
| `Figure` | Immutable chart model — the output of `.Build()` |
| `ChartSeries` | Abstract base for all 91 series types |
| `ChartDataTable` | The figure's data as a table — `figure.ToDataTables()`, then `ToHtml()` / `ToMarkdown()` / `ToCsv()` |
| `ChartDataColumn` | One column: its header, its `DataColumnKind` (Number/Date/Text) and the `DataAxis` its values are positions on |
| `Theme` | Theme presets and custom theme builder |
| `ColorMaps` | Nine named colormaps, and `Get(name)` for any of the 148 colormaps registered by name |

## Package namespaces

| Namespace | Package | Notes |
|---|---|---|
| `MatPlotLibNet` | Core | `Plt`, `FigureBuilder`, `AxesBuilder`, all 91 series types, the 38 chart verbs and the types they take (`BarGroup`, `NamedSample`, `SurvivalGroup`, `ControlLimits`, `ControlChartData`, `QQReference`, `SpectralOptions`, `SpectrumOptions`, `CorrelationOptions`, `MagnitudeScale`) |
| `MatPlotLibNet.Models` | Core | Chart model types (`Figure`, `Axes`, etc.), and what a figure or an inset draws itself: `FigureText`, `FigureImage`, `ZoomIndicator` |
| `MatPlotLibNet.Rendering` | Core | SVG render pipeline |
| `MatPlotLibNet.Styling` | Core | Themes, colors, colormaps |
| `MatPlotLibNet.Numerics` | Core | `Vec`, `Mat`, SIMD math, `Fft`, and the window and detrend of the signal verbs (`SpectralWindow`, `SpectralDetrend`, `SpectralScaling`) |
| `MatPlotLibNet.Interaction` | Core | Managed interaction modifiers + events |
| `MatPlotLibNet.Animation` | Core | `IAnimation<TState>`, `AnimationController` |
| `MatPlotLibNet.Skia` | `MatPlotLibNet.Skia` | PNG, PDF and GIF export via SkiaSharp |
| `MatPlotLibNet.Geo` | `MatPlotLibNet.Geo` | 13 map projections + GeoJSON loaders |
| `MatPlotLibNet.DataFrame` | `MatPlotLibNet.DataFrame` | `Microsoft.Data.Analysis` extensions |
| `MatPlotLibNet.AspNetCore` | `MatPlotLibNet.AspNetCore` | SignalR hub + REST endpoints |
| `MatPlotLibNet.Interactive` | `MatPlotLibNet.Interactive` | Embedded Kestrel server + `Show()` extension |
| `MatPlotLibNet.GraphQL` | `MatPlotLibNet.GraphQL` | Hot Chocolate subscription type |
| `MatPlotLibNet.Blazor` | `MatPlotLibNet.Blazor` | `MplChart`, `MplLiveChart`, `MplStreamingChart` Razor components |
| `MatPlotLibNet.Wpf` | `MatPlotLibNet.Wpf` | `MplChartControl` and `MplStreamingChartControl` for WPF (Skia-backed) |
| `MatPlotLibNet.Avalonia` | `MatPlotLibNet.Avalonia` | `MplChartControl` for Avalonia |
| `MatPlotLibNet.Maui` | `MatPlotLibNet.Maui` | `MplChartView` for .NET MAUI (Android/iOS/Mac/Windows) |
| `MatPlotLibNet.Uno` | `MatPlotLibNet.Uno` | `MplChartElement` for Uno Platform |
| `MatPlotLibNet.Mcp` | `MatPlotLibNet.Mcp` | MCP server — an AI agent renders charts over stdio |
| `MatPlotLibNet.Notebooks` | `MatPlotLibNet.Notebooks` | .NET Interactive (Polyglot Notebooks) extension |
