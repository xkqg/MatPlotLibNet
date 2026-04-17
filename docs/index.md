# MatPlotLibNet

**matplotlib for .NET** — 74 series types, 104 colormaps, 13 map projections, 26 themes, native Avalonia + Uno + WPF + MAUI controls, SignalR interactivity, streaming, and publication-quality SVG/PNG/PDF output. 13 NuGet packages.

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

## Explore

| Section | Description |
|---|---|
| [Playground](https://xkqg.github.io/MatPlotLibNet/playground/) | Try charts live in the browser — pick an example, tweak, see the SVG update |
| [Cookbook](https://xkqg.github.io/MatPlotLibNet/cookbook/) | Code examples with rendered images — copy, paste, chart |
| [API Reference](https://xkqg.github.io/MatPlotLibNet/api/) | Full API documentation generated from XML doc comments |
| [Wiki](https://github.com/xkqg/MatPlotLibNet/wiki) | Guides, tutorials, and architecture documentation |
| [NuGet](https://www.nuget.org/packages/MatPlotLibNet) | Install via `dotnet add package MatPlotLibNet` |

## Packages

| Package | Purpose |
|---|---|
| `MatPlotLibNet` | Core: models, fluent API, SVG rendering |
| `MatPlotLibNet.Skia` | PNG, PDF, GIF export via SkiaSharp |
| `MatPlotLibNet.Blazor` | Razor components with SignalR |
| `MatPlotLibNet.AspNetCore` | REST + SignalR hub + `IChartPublisher` |
| `MatPlotLibNet.Interactive` | Browser popup — no server needed |
| `MatPlotLibNet.GraphQL` | HotChocolate queries + subscriptions |
| `MatPlotLibNet.Maui` | Native MAUI control |
| `MatPlotLibNet.Avalonia` | Native Avalonia 12 control |
| `MatPlotLibNet.Uno` | Native Uno Platform control |
| `MatPlotLibNet.DataFrame` | DataFrame indicators + regression |
| `MatPlotLibNet.Wpf` | Native WPF chart control via SkiaSharp |
| `MatPlotLibNet.Geo` | 13 map projections, GeoJSON, Natural Earth 110m data |
| `MatPlotLibNet.Notebooks` | Polyglot / Jupyter inline SVG |
