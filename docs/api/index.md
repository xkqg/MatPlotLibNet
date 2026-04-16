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
| `ChartSeries` | Abstract base for all 67 series types |
| `Theme` | Theme presets and custom theme builder |
| `ColorMaps` | 104 colormap presets |

## Package namespaces

| Namespace | Package |
|---|---|
| `MatPlotLibNet` | Core |
| `MatPlotLibNet.Models` | Chart model types |
| `MatPlotLibNet.Rendering` | SVG render pipeline |
| `MatPlotLibNet.Styling` | Themes, colors, colormaps |
| `MatPlotLibNet.Numerics` | Vec, Mat, SIMD math |
| `MatPlotLibNet.Interaction` | Managed interaction layer |
| `MatPlotLibNet.Blazor` | Blazor Razor components |
| `MatPlotLibNet.AspNetCore` | SignalR hub, REST endpoints |
| `MatPlotLibNet.DataFrame` | DataFrame extensions |
