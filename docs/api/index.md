# API Reference

Full API documentation generated from XML doc comments in the MatPlotLibNet source code.

Browse namespaces in the sidebar, or use the search bar to find specific types, methods, or properties.

## Key entry points

| Type | Description |
|---|---|
| `Plt` | Static entry point — `Plt.Create()` starts the fluent builder |
| `FigureBuilder` | Top-level builder: title, theme, size, subplots, export |
| `AxesBuilder` | Per-subplot builder: series, labels, legends, ticks, camera |
| `Figure` | Immutable chart model — the output of `.Build()` |
| `ChartSeries` | Abstract base for all 74 series types |
| `Theme` | Theme presets and custom theme builder |
| `ColorMaps` | 104 colormap presets |

## Package namespaces

| Namespace | Package | Notes |
|---|---|---|
| `MatPlotLibNet` | Core | `Plt`, `FigureBuilder`, `AxesBuilder`, all 74 series types |
| `MatPlotLibNet.Models` | Core | Chart model types (`Figure`, `Axes`, etc.) |
| `MatPlotLibNet.Rendering` | Core | SVG render pipeline |
| `MatPlotLibNet.Styling` | Core | Themes, colors, colormaps |
| `MatPlotLibNet.Numerics` | Core | `Vec`, `Mat`, SIMD math |
| `MatPlotLibNet.Interaction` | Core | Managed interaction modifiers + events |
| `MatPlotLibNet.Animation` | Core | `IAnimation<TState>`, `AnimationController` |
| `MatPlotLibNet.Skia` | `MatPlotLibNet.Skia` | PNG/PDF/JPEG export via SkiaSharp |
| `MatPlotLibNet.Geo` | `MatPlotLibNet.Geo` | 11 geographic projections + GeoJSON loaders |
| `MatPlotLibNet.DataFrame` | `MatPlotLibNet.DataFrame` | `Microsoft.Data.Analysis` extensions |
| `MatPlotLibNet.AspNetCore` | `MatPlotLibNet.AspNetCore` | SignalR hub + REST endpoints |
| `MatPlotLibNet.Interactive` | `MatPlotLibNet.Interactive` | Embedded Kestrel server + `Show()` extension |
| `MatPlotLibNet.GraphQL` | `MatPlotLibNet.GraphQL` | Hot Chocolate subscription type |
| `MatPlotLibNet.Blazor` | `MatPlotLibNet.Blazor` | `MplChart`, `MplLiveChart`, `MplStreamingChart` Razor components |
| `MatPlotLibNet.Wpf` | `MatPlotLibNet.Wpf` | `MplChartControl` for WPF (Skia-backed) |
| `MatPlotLibNet.Avalonia` | `MatPlotLibNet.Avalonia` | `MplChartControl` for Avalonia |
| `MatPlotLibNet.Maui` | `MatPlotLibNet.Maui` | `MplChartView` for .NET MAUI (Android/iOS/Mac/Windows) |
| `MatPlotLibNet.Uno` | `MatPlotLibNet.Uno` | `MplChartElement` for Uno Platform |
| `MatPlotLibNet.Notebooks` | `MatPlotLibNet.Notebooks` | .NET Interactive (Polyglot Notebooks) extension |
