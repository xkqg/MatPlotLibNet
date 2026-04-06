# MatPlotLibNet

A .NET 10 / .NET Standard 2.1 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel SVG rendering, polymorphic export (SVG/PNG/PDF), and multi-platform output to Blazor, MAUI, ASP.NET Core, Angular, React, Vue, and standalone browser popups.

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

## Packages

| Package | Install | What it does |
|---------|---------|--------------|
| **MatPlotLibNet** | `dotnet add package MatPlotLibNet` | Core: models, fluent API, SVG rendering, JSON serialization, transforms |
| **MatPlotLibNet.Skia** | `dotnet add package MatPlotLibNet.Skia` | PNG and PDF export via SkiaSharp |
| **MatPlotLibNet.Blazor** | `dotnet add package MatPlotLibNet.Blazor` | `MplChart` + `MplLiveChart` Razor components with SignalR |
| **MatPlotLibNet.AspNetCore** | `dotnet add package MatPlotLibNet.AspNetCore` | REST endpoints, SignalR hub, `IChartPublisher` |
| **MatPlotLibNet.Maui** | `dotnet add package MatPlotLibNet.Maui` | Native `MplChartView` control via Microsoft.Maui.Graphics |
| **MatPlotLibNet.Interactive** | `dotnet add package MatPlotLibNet.Interactive` | `figure.ShowAsync()` opens default browser with live updates |
| **MatPlotLibNet.GraphQL** | `dotnet add package MatPlotLibNet.GraphQL` | GraphQL queries + subscriptions via HotChocolate |
| **@matplotlibnet/angular** | `npm install @matplotlibnet/angular` | Angular components + TypeScript SignalR client |
| **@matplotlibnet/react** | `npm install @matplotlibnet/react` | React hooks + components + TypeScript SignalR client |
| **@matplotlibnet/vue** | `npm install @matplotlibnet/vue` | Vue 3 composables + components + TypeScript SignalR client |

## Quick start

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Styling;

double[] x = [1, 2, 3, 4, 5];
double[] y = [2, 4, 3, 5, 1];

// Fluent API -> SVG string (no Build() needed)
string svg = Plt.Create()
    .WithTitle("My Chart")
    .WithTheme(Theme.Seaborn)
    .Plot(x, y, line => { line.Color = Color.Blue; line.Label = "sin(x)"; })
    .ToSvg();

// Polymorphic export via transforms
using MatPlotLibNet.Transforms;

Plt.Create().Plot(x, y).Transform(new SvgTransform()).Save("chart.svg");
Plt.Create().Plot(x, y).Transform(new PngTransform()).Save("chart.png");   // requires MatPlotLibNet.Skia
Plt.Create().Plot(x, y).Transform(new PdfTransform()).Save("chart.pdf");   // requires MatPlotLibNet.Skia
```

## Chart types

**31 series types** with fluent builder API:

```csharp
var fig = Plt.Create()
    .Plot(x, y)                                           // line
    .Scatter(x, y, s => s.MarkerSize = 8)                 // scatter
    .Bar(["Q1", "Q2", "Q3"], [100, 200, 150])             // bar
    .Hist(measurements, bins: 20)                          // histogram
    .Pie([40, 30, 20, 10], ["A", "B", "C", "D"])          // pie
    .Step(x, y, s => s.StepPosition = StepPosition.Post)  // step function
    .FillBetween(x, y)                                    // area / fill between
    .ErrorBar(x, y, errLow, errHigh)                      // error bars
    .Build();
```

Additional types via `AxesBuilder.AddSubPlot`:
Heatmap, Box, Violin, Contour, Stem, Candlestick, OhlcBar, Quiver, Radar, Donut, Bubble, Waterfall, Funnel, Gantt, Gauge, ProgressBar, Sparkline, Treemap, Sunburst, Sankey, PolarLine, PolarScatter, PolarBar.

### Stacked bars

```csharp
.AddSubPlot(1, 1, 1, ax => ax
    .SetBarMode(BarMode.Stacked)
    .Bar(["A", "B"], [10.0, 20.0])
    .Bar(["A", "B"], [5.0, 10.0]))
```

## Annotations and decorations

```csharp
.AddSubPlot(1, 1, 1, ax => ax
    .Plot(x, y)
    .Annotate("peak", 2.0, 4.0, a => { a.ArrowTargetX = 1.5; a.ArrowTargetY = 3.5; })
    .AxHLine(3.5, l => l.Color = Color.Red)           // horizontal reference line
    .AxVLine(2.0)                                       // vertical reference line
    .AxHSpan(3.0, 4.0, s => s.Alpha = 0.1)            // shaded horizontal region
    .AxVSpan(1.5, 2.5))                                // shaded vertical region
```

## Secondary Y-axis (TwinX)

```csharp
.AddSubPlot(1, 1, 1, ax => ax
    .Plot(time, temperature)
    .SetYLabel("Temperature (C)")
    .WithSecondaryYAxis(sec => sec
        .SetYLabel("Humidity (%)")
        .Plot(time, humidity, s => s.Color = Color.Orange)))
```

## Technical indicators (TradingView-style)

```csharp
// Overlay indicators — auto-detect price data from series on axes
ax.Candlestick(open, high, low, close)
  .Sma(20)                           // adds SMA overlay
  .Ema(9)                            // adds EMA overlay
  .BollingerBands(20, 2.0)           // adds BB bands + middle line

// Buy/sell signals
  .BuyAt(5, close[5])
  .SellAt(12, close[12])

// Panel indicators in subplots
.AddSubPlot(3, 1, 2, ax => ax.Rsi(close, 14).AxHLine(70).AxHLine(30))
.AddSubPlot(3, 1, 3, ax => ax.AddIndicator(new Macd(close)))
```

**13 indicators:** SMA, EMA, Bollinger Bands, VWAP, RSI, MACD, Stochastic, Volume, Fibonacci, ATR, ADX, Keltner Channels, Ichimoku Cloud.

**Trading analytics:** EquityCurve, ProfitLoss, DrawDown — for strategy backtesting panels.

## Hierarchical charts (Treemap + Sunburst)

```csharp
var tree = new TreeNode
{
    Label = "Sales",
    Children = [
        new TreeNode { Label = "Electronics", Value = 400 },
        new TreeNode { Label = "Clothing", Value = 300 },
        new TreeNode { Label = "Food", Value = 200 }
    ]
};

Plt.Create().Treemap(tree).Build();     // nested rectangles
Plt.Create().Sunburst(tree).Build();    // concentric ring segments
```

## Sankey diagrams

```csharp
SankeyNode[] nodes = [new("A"), new("B"), new("C")];
SankeyLink[] links = [new(0, 1, 30), new(0, 2, 20)];

Plt.Create().Sankey(nodes, links).Build();
```

## Legend

```csharp
.AddSubPlot(1, 1, 1, ax => ax
    .Plot(x, temp, s => s.Label = "Temperature")
    .Plot(x, humidity, s => s.Label = "Humidity")
    .WithLegend(LegendPosition.UpperRight))
```

## Subplot spacing

```csharp
Plt.Create()
    .TightLayout()
    .AddSubPlot(2, 2, 1, ax => ax.Plot(x, y))
    .AddSubPlot(2, 2, 2, ax => ax.Scatter(x, y))
    .Build();

// Or custom margins
Plt.Create()
    .WithSubPlotSpacing(s => s with { MarginLeft = 80, HorizontalGap = 20 })
    .Build();
```

## Polar plots

```csharp
double[] r = [1, 2, 3, 4, 5];
double[] theta = [0, 0.5, 1.0, 1.5, 2.0];

// Polar line
Plt.Create().PolarPlot(r, theta).ToSvg();

// Polar scatter
Plt.Create().PolarScatter(r, theta).ToSvg();

// Polar bar (windrose-style)
Plt.Create().PolarBar([5, 10, 8, 3], [0, Math.PI/2, Math.PI, 3*Math.PI/2]).ToSvg();
```

## Axis formatting

```csharp
// Date axis
.AddSubPlot(1, 1, 1, ax => ax
    .SetXDateFormat("MMM yyyy")
    .Plot(dates, values))

// Custom tick formatter
.SetXTickFormatter(new LogTickFormatter())
```

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

## Export transforms

All output formats share the `IFigureTransform` interface with a fluent `TransformResult`. No `.Build()` needed:

```csharp
using MatPlotLibNet.Transforms;

// Auto-detect format from file extension -- no Build() needed
Plt.Create().Plot(x, y).Save("chart.svg");
Plt.Create().Plot(x, y).Save("chart.png");   // requires MatPlotLibNet.Skia
Plt.Create().Plot(x, y).Save("chart.pdf");   // requires MatPlotLibNet.Skia
Plt.Create().Plot(x, y).Save("chart.json");

// Convenience methods
string svg = Plt.Create().Plot(x, y).ToSvg();
string json = Plt.Create().Plot(x, y).ToJson();

// Register PNG/PDF once at startup (when using MatPlotLibNet.Skia)
FigureBuilder.RegisterGlobalTransform(".png", new PngTransform());
FigureBuilder.RegisterGlobalTransform(".pdf", new PdfTransform());
```

## SVG interactivity

```csharp
// Native browser tooltips on hover
.AddSubPlot(1, 1, 1, ax => ax.WithTooltips().Scatter(x, y))

// Zoom (mouse wheel) and pan (click-drag) via embedded JavaScript
Plt.Create().WithZoomPan().Plot(x, y).Build()
```

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

Interfaces: `IFigureTransform`, `IChartRenderer`, `ISvgRenderer`, `IChartSerializer`, `IChartPublisher`.

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

**React**:
```tsx
<MplLiveChart chartId="sensor-1" hubUrl="/charts-hub" />
```

**Vue**:
```vue
<MplLiveChart chartId="sensor-1" hubUrl="/charts-hub" />
```

**GraphQL** (subscription):
```graphql
subscription { onChartSvgUpdated(chartId: "sensor-1") }
```

All platforms use `IChartSubscriptionClient` -- same SignalR protocol, different implementations (C# / TypeScript).

## Interactive browser popup

```csharp
using MatPlotLibNet.Interactive;

var handle = await figure.ShowAsync();   // opens default browser
await handle.UpdateAsync();              // pushes live updates
```

## Performance: server-side SVG + SignalR

Charts render **server-side as SVG** and push to clients via SignalR — no client-side chart library, no JavaScript rendering, no canvas redraws.

- **Less traffic** — a chart SVG is 5-15 KB vs. shipping raw datasets (100 KB+) plus a JS chart library (200-500 KB). Only changed charts are pushed.
- **Zero client CPU** — the browser swaps innerHTML. A Raspberry Pi displays the same dashboard as a workstation.
- **Inline SVG in the DOM** — CSS-stylable, printable, accessible to screen readers. Works in hidden tabs and below the fold.
- **Inline, expandable, or popup** — view charts in-page, expand in-place, or pop out into a separate window. `figure.ShowAsync()` opens a standalone browser with live updates.
- **Same output everywhere** — identical SVG whether inline in Blazor, pushed to React via SignalR, saved as a file, exported to PNG/PDF, or rendered in MAUI.

A simple line chart: **52 us**. A 3x3 subplot grid: **224 us**. All 13 indicators on 100K points: **< 8 ms**. See [BENCHMARKS.md](BENCHMARKS.md).

## Architecture

```
MatPlotLibNet (Core)                      net10.0 + netstandard2.1
    |
    +-- MatPlotLibNet.Skia                PNG + PDF export via SkiaSharp
    +-- MatPlotLibNet.Blazor              Razor components + C# SignalR client
    +-- MatPlotLibNet.AspNetCore          REST endpoints + SignalR hub
    |       +-- MatPlotLibNet.Interactive  embedded Kestrel + browser popup
    |       +-- MatPlotLibNet.GraphQL      GraphQL queries + subscriptions (HotChocolate)
    +-- MatPlotLibNet.Maui                native GraphicsView rendering
    +-- @matplotlibnet/angular            Angular components + TS SignalR client
    +-- @matplotlibnet/react              React hooks + components + TS SignalR client
    +-- @matplotlibnet/vue                Vue 3 composables + components + TS SignalR client
```

See [ARCHITECTURE.md](Src/MatPlotLibNet/ARCHITECTURE.md) for the full rendering pipeline, data flow, and design patterns.

## Version history

| Version | Highlights |
|---------|-----------|
| **0.3.3** | 31 series types: Treemap, Sunburst, Sankey, PolarLine, PolarScatter, PolarBar. Polar coordinate system with `PolarTransform` and circular grid rendering. `HierarchicalSeries` generic base. Legend rendering. Configurable subplot spacing. `ITickFormatter` pipeline (date, log, numeric). `FigureBuilder` output methods (`ToSvg()`, `SaveSvg()`, `Transform()`) — no `.Build()` needed. GitHub Actions v5. |
| **0.3.2** | Quality release: OO indicator refactor (`Indicator<TResult>` with `IIndicatorResult` constraint, named result records, no statics). 92 new tests. BenchmarkDotNet suite. CHANGELOG, BENCHMARKS.md, DocFX, 4 sample projects. JSON serialization fix for 9 series types. |
| **0.3.1** | Platform expansion: `@matplotlibnet/react` (React 19 hooks + components), `@matplotlibnet/vue` (Vue 3 composables + components), `MatPlotLibNet.GraphQL` (HotChocolate queries + subscriptions). Core library multi-targets `netstandard2.1`. |
| **0.3.0** | 25 series types (Donut, Bubble, OhlcBar, Waterfall, Funnel, Gantt, Gauge, ProgressBar, Sparkline). 13 technical indicators (SMA, EMA, BB, VWAP, RSI, MACD, Stochastic, Volume, Fibonacci, ATR, ADX, Keltner, Ichimoku). Trading analytics (EquityCurve, ProfitLoss, DrawDown). Buy/sell signal markers. Generic `SeriesRenderer<T>` + `Indicator<TResult>`. Intuitive fluent API (`.Sma(20)`, `.BuyAt()`, `.SaveSvg()`). PriceSource enum, Offset, LineStyle on all indicators. Series organized by chart family. |
| **0.2.0** | 16 series types (Area, Step, ErrorBar, Candlestick, Quiver, Radar). Stacked bars. Annotations (text, HLine/VLine, HSpan/VSpan). Secondary Y-axis (TwinX). SVG tooltips + zoom/pan. Polymorphic transforms (`IFigureTransform`, `FigureTransform`, `TransformResult`). PNG/PDF export via MatPlotLibNet.Skia. |
| **0.1.0** | Initial release. 10 series types (Line, Scatter, Bar, Histogram, Pie, Heatmap, Box, Violin, Contour, Stem). Fluent builder API. Parallel SVG rendering. JSON serialization. 6 themes. Blazor, ASP.NET Core, MAUI, Angular, Interactive packages. |

See [CHANGELOG.md](CHANGELOG.md) for detailed release notes. See [BENCHMARKS.md](BENCHMARKS.md) for performance numbers.

## License

[GPL-3.0](LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
