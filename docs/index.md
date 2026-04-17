# MatPlotLibNet

**matplotlib for .NET** — 74 series types, 104 colormaps, 12 3D chart types, native Avalonia + Uno controls, SignalR interactivity, and publication-quality SVG/PNG/PDF output.

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
| [Cookbook](cookbook/) | Code examples with rendered images — copy, paste, chart |
| [API Reference](api/) | Full API documentation generated from XML doc comments |
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
| `MatPlotLibNet.Notebooks` | Polyglot / Jupyter inline SVG |
