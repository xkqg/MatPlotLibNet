# MatPlotLibNet

A .NET 10 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel SVG rendering, and multi-platform output to Blazor, MAUI, ASP.NET Core, Angular, and standalone browser popups.

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

## Packages

| Package | Install | What it does |
|---------|---------|--------------|
| **MatPlotLibNet** | `dotnet add package MatPlotLibNet` | Core: models, fluent API, SVG rendering, JSON serialization, DI interfaces |
| **MatPlotLibNet.Blazor** | `dotnet add package MatPlotLibNet.Blazor` | `MplChart` + `MplLiveChart` Razor components with SignalR |
| **MatPlotLibNet.AspNetCore** | `dotnet add package MatPlotLibNet.AspNetCore` | REST endpoints, SignalR hub, `IChartPublisher` |
| **MatPlotLibNet.Maui** | `dotnet add package MatPlotLibNet.Maui` | Native `MplChartView` control via Microsoft.Maui.Graphics |
| **MatPlotLibNet.Interactive** | `dotnet add package MatPlotLibNet.Interactive` | `figure.ShowAsync()` opens default browser with live updates |
| **@matplotlibnet/angular** | `npm install @matplotlibnet/angular` | Angular components + TypeScript SignalR client |

## Quick start

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Styling;

double[] x = [1, 2, 3, 4, 5];
double[] y = [2, 4, 3, 5, 1];

// Fluent API -> SVG string
string svg = Plt.Create()
    .WithTitle("My Chart")
    .WithTheme(Theme.Seaborn)
    .Plot(x, y, line => { line.Color = Color.Blue; line.Label = "sin(x)"; })
    .Build()
    .ToSvg();

// Save to file
File.WriteAllText("chart.svg", svg);
```

## Chart types

```csharp
var fig = Plt.Create()
    .Plot(x, y)                                           // line
    .Scatter(x, y, s => s.MarkerSize = 8)                 // scatter
    .Bar(["Q1", "Q2", "Q3"], [100, 200, 150])             // bar
    .Hist(measurements, bins: 20)                          // histogram
    .Pie([40, 30, 20, 10], ["A", "B", "C", "D"])          // pie
    .Build();
```

Additional types via `AxesBuilder.AddSubPlot`: Heatmap, Box, Violin, Contour, Stem.

## Subplots

```csharp
var fig = Plt.Create()
    .WithSize(1200, 600)
    .AddSubPlot(1, 2, 1, ax => ax
        .WithTitle("Temperature")
        .SetXLabel("Time").SetYLabel("Celsius")
        .Plot(time, temp)
        .ShowGrid())
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle("Distribution")
        .Hist(samples, bins: 15))
    .Build();
```

Subplots render in **parallel** -- each gets its own SVG context, merged in order.

## Dependency injection

Rendering and serialization are interface-based:

```csharp
// ASP.NET Core -- all services registered automatically
builder.Services.AddMatPlotLibNetSignalR();

// Console apps -- static defaults via ChartServices
string svg = ChartServices.SvgRenderer.Render(figure);
string json = ChartServices.Serializer.ToJson(figure);

// Replace with custom implementations
ChartServices.Serializer = new MyCustomSerializer();
```

Interfaces: `IChartRenderer`, `ISvgRenderer`, `IChartSerializer`, `IChartPublisher`, `IChartSubscriptionClient`.

## Themes

| Theme | Style |
|-------|-------|
| `Theme.Default` | White background, classic matplotlib |
| `Theme.Dark` | Dark gray, light text |
| `Theme.Seaborn` | Light gray, statistical |
| `Theme.Ggplot` | R ggplot2 |
| `Theme.Bmh` | Bayesian Methods |
| `Theme.FiveThirtyEight` | Journalism |

Custom themes with immutable records:

```csharp
var theme = Theme.CreateFrom(Theme.Dark)
    .WithBackground(Color.FromHex("#1a1a2e"))
    .WithFont(f => f with { Family = "Consolas", Size = 14 })
    .WithGrid(g => g with { Visible = true, Alpha = 0.3 })
    .Build();
```

## Real-time charts

**Server** (ASP.NET Core):
```csharp
await publisher.PublishSvgAsync("sensor-1", figure);
```

**Blazor**:
```razor
<MplLiveChart ChartId="sensor-1" HubUrl="/charts-hub" />
```

**Angular**:
```html
<mpl-live-chart [chartId]="'sensor-1'" [hubUrl]="'/charts-hub'"></mpl-live-chart>
```

Both use `IChartSubscriptionClient` -- same SignalR protocol, different implementations (C# / TypeScript).

## Interactive browser popup

```csharp
using MatPlotLibNet.Interactive;

var handle = await figure.ShowAsync();   // opens default browser
await handle.UpdateAsync();              // pushes live updates
```

## Architecture

```
MatPlotLibNet (Core)                      zero external dependencies
    |
    +-- MatPlotLibNet.Blazor              Razor components + C# SignalR client
    +-- MatPlotLibNet.AspNetCore          REST endpoints + SignalR hub
    |       +-- MatPlotLibNet.Interactive  embedded Kestrel + browser popup
    +-- MatPlotLibNet.Maui                native GraphicsView rendering
    +-- @matplotlibnet/angular            Angular components + TS SignalR client
```

See [ARCHITECTURE.md](Src/MatPlotLibNet/ARCHITECTURE.md) for the full rendering pipeline, data flow, and design patterns.

## License

[GPL-3.0](LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
