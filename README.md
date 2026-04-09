# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel SVG rendering, polymorphic export (SVG/PNG/PDF), and multi-platform output to Blazor, MAUI, ASP.NET Core, Angular, React, Vue, and standalone browser popups.

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

// Fluent API -> save with format auto-detected from extension
Plt.Create()
    .WithTitle("My Chart")
    .WithTheme(Theme.Seaborn)
    .Plot(x, y, line => { line.Color = Color.Blue; line.Label = "sin(x)"; })
    .Save("chart");  // no extension = SVG by default

// Or get the SVG string directly
string svg = Plt.Create().Plot(x, y).ToSvg();

// Multiple formats — no Build() needed
Plt.Create().Plot(x, y).Save("chart.svg");
Plt.Create().Plot(x, y).Save("chart.png");   // requires MatPlotLibNet.Skia
Plt.Create().Plot(x, y).Save("chart.pdf");   // requires MatPlotLibNet.Skia
```

## Chart types

**39 series types** with fluent builder API:

```csharp
Plt.Create()
    .Plot(x, y)                                           // line
    .Scatter(x, y, s => s.MarkerSize = 8)                 // scatter
    .Bar(["Q1", "Q2", "Q3"], [100, 200, 150])             // bar
    .Hist(measurements, bins: 20)                          // histogram
    .Pie([40, 30, 20, 10], ["A", "B", "C", "D"])          // pie
    .Step(x, y, s => s.StepPosition = StepPosition.Post)  // step function
    .FillBetween(x, y)                                    // area / fill between
    .ErrorBar(x, y, errLow, errHigh)                      // error bars
    .Save("chart");
```

Additional types via `AxesBuilder.AddSubPlot`:
Heatmap, Image (imshow), Histogram2D, Box, Violin, Contour, Stem, Candlestick, OhlcBar, Quiver, Radar, Donut, Bubble, Waterfall, Funnel, Gantt, Gauge, ProgressBar, Sparkline, Ecdf, StackedArea, Streamplot, Treemap, Sunburst, Sankey, PolarLine, PolarScatter, PolarBar, Surface, Wireframe, Scatter3D.

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

Plt.Create().Treemap(tree).Save("treemap");     // nested rectangles
Plt.Create().Sunburst(tree).Save("sunburst");   // concentric ring segments
```

## Sankey diagrams

```csharp
SankeyNode[] nodes = [new("A"), new("B"), new("C")];
SankeyLink[] links = [new(0, 1, 30), new(0, 2, 20)];

Plt.Create().Sankey(nodes, links).Save("sankey");
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
    .Save("tight");

// Or custom margins
Plt.Create()
    .WithSubPlotSpacing(s => s with { MarginLeft = 80, HorizontalGap = 20 })
    .Save("custom_spacing");
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

## 3D plots

```csharp
double[] x = [0, 1, 2, 3];
double[] y = [0, 1, 2, 3];
double[,] z = { {0, 1, 2, 3}, {1, 2, 3, 4}, {2, 3, 4, 5}, {3, 4, 5, 6} };

// Surface plot with color mapping
Plt.Create().Surface(x, y, z).Save("surface");

// Wireframe
Plt.Create().Wireframe(x, y, z).Save("wireframe");

// 3D scatter
Plt.Create().Scatter3D([1, 2, 3], [4, 5, 6], [7, 8, 9]).Save("scatter3d");

// Custom projection angle
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithProjection(elevation: 45, azimuth: -45)
        .Surface(x, y, z))
    .Save("rotated");
```

## Color maps

52 built-in colormaps across 6 categories (104 including reversed `_r` variants):

```csharp
using MatPlotLibNet.Styling.ColorMaps;

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Heatmap(data)
        .WithColorMap("turbo")
        .WithColorBar(cb => cb with { Label = "Intensity" }))
    .Save("heatmap");
```

Categories: **Perceptually-uniform** (Viridis, Plasma, Inferno, Magma, Turbo, Cividis), **Sequential** (Blues, Reds, Hot, Copper, Bone, BuPu, GnBu, YlGnBu, PuBuGn, and 12 more), **Diverging** (Coolwarm, RdBu, Seismic, Bwr, PuOr, and 5 more), **Cyclic** (Twilight, TwilightShifted, Hsv), **Qualitative** (Tab10, Tab20, Set1-3, Pastel1-2, Dark2, Accent, Paired).

Reversed: append `_r` to any name — `ColorMapRegistry.Get("viridis_r")`.

Normalizers: `LogNormalizer`, `TwoSlopeNormalizer(center)`, `BoundaryNormalizer(double[])`.

See [howTo.md §13](Src/MatPlotLibNet/howTo.md#13-color-maps) for the full reference.

## Color bar

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Heatmap(data)
        .WithColorBar(cb => cb with { Label = "Temperature" }))
    .Save("heatmap_colorbar");
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
Plt.Create()
    .WithSize(1200, 600)
    .AddSubPlot(1, 2, 1, ax => ax
        .WithTitle("Temperature")
        .SetXLabel("Time").SetYLabel("Celsius")
        .Plot(time, temp)
        .ShowGrid())
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle("Distribution")
        .Hist(samples, bins: 15))
    .Save("subplots");
```

Subplots render in **parallel** -- each gets its own SVG context, merged in order.

## GridSpec layouts

Unequal row/column sizes with cell spanning:

```csharp
Plt.Create()
    .WithGridSpec(2, 2, heightRatios: [2.0, 1.0], widthRatios: [3.0, 1.0])
    .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot(x, y).WithTitle("Main"))
    .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Scatter(x, y))
    .AddSubPlot(GridPosition.Span(1, 2, 0, 2), ax => ax.Bar(cats, vals))
    .Save("gridspec");
```

## Inset axes

```csharp
.AddSubPlot(1, 1, 1, ax => ax
    .Plot(x, y)
    .AddInset(0.6, 0.6, 0.35, 0.35, inset => inset
        .Plot(xZoom, yZoom)
        .WithTitle("Detail")))
```

Coordinates are fractions of the parent axes (0–1). Nests up to 3 levels deep.

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
FigureExtensions.RegisterTransform(".png", new PngTransform());
FigureExtensions.RegisterTransform(".pdf", new PdfTransform());
```

## SVG interactivity

```csharp
// Native browser tooltips on hover
.AddSubPlot(1, 1, 1, ax => ax.WithTooltips().Scatter(x, y))

// Zoom (mouse wheel) and pan (click-drag) via embedded JavaScript
Plt.Create().WithZoomPan().Plot(x, y).Save("zoomable")
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

## Animation

```csharp
using MatPlotLibNet.Animation;
using MatPlotLibNet.Interactive;

// Legacy: AnimationBuilder for frame-based animation
var animation = new AnimationBuilder(60, frame =>
    Plt.Create()
        .WithTitle($"Frame {frame}")
        .Plot(x, x.Select(v => Math.Sin(v + frame * 0.1)).ToArray())
        .Build());

// Play in browser with 50ms between frames
var handle = await Plt.Create().Plot(x, y).Build().ShowAsync();
await handle.AnimateAsync(animation);

// New: IAnimation<TState> + AnimationController<TState> for typed animation pipelines
// LegacyAnimationAdapter bridges AnimationBuilder to the new IAnimation<TState> contract
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

A simple line chart: **52 us**. A treemap: **26 us**. A 3D surface: **72 us**. A 3x3 subplot grid: **422 us**. All 13 indicators on 100K points: **< 8 ms**. See [BENCHMARKS.md](BENCHMARKS.md).

## Architecture

```
MatPlotLibNet (Core)                      net10.0 + net8.0
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
| **0.5.0** | 39 series types. Layout: `GridSpec` unequal subplots, `SpinesConfig`, shared axes (`ShareX`/`ShareY`), inset axes. 5 new series: `ImageSeries` (imshow), `Histogram2DSeries`, `StreamplotSeries`, `EcdfSeries`, `StackedAreaSeries`. OO interfaces: `IColormappable`, `INormalizable`, `ICategoryLabeled`, `IColorBarDataProvider`, `IStackable`. 20 new colormaps (52 base, 104 with `_r` variants): Turbo, Jet, Hsv, Hot, Copper, Bone, BuPu, GnBu, PuRd, RdPu, YlGnBu, PuBuGn, Cubehelix, PuOr, Seismic, Bwr, Pastel2, Dark2, Accent, Paired. `ColorMapRegistry` (case-insensitive, thread-safe). `INormalizer`: Linear, Log, TwoSlope, Boundary. `AxesBuilder.WithColorMap/WithNormalizer` collapse to single-line interface check (bug fix: previously missed 3 of 7 colormappable series). 1502 tests. |
| **0.4.0** | 34 series types (11 families), 798 tests. OO architecture: `AxesRenderer` polymorphism (`CartesianAxesRenderer`, `PolarAxesRenderer`, `ThreeDAxesRenderer`). `ISeriesSerializable` on all 34 series, `SeriesRegistry` for deserialization. Generic bases: `XYSeries`, `PolarSeries`, `GridSeries3D`, `HierarchicalSeries`. Interfaces: `IHasDataRange`, `IPolarSeries`, `I3DGridSeries`, `I3DPointSeries`, `IPriceSeries`. Thread-safe: volatile fields, `ConcurrentDictionary` for `GlobalTransforms`, `AxesRenderer` registry, `SeriesRegistry`. Animation: `IAnimation<TState>`, `AnimationController<TState>`, `LegacyAnimationAdapter`. `Save("chart")` API with format auto-detect. `FigureBuilder` SRP: Save/Transform moved to `FigureExtensions`. Color constants (`Tab10Blue`, `GridGray`, etc.). `ITickFormatter` pipeline. ColorBar, Legend, SubPlotSpacing. Polar + 3D coordinate systems. |
| **0.3.2** | Quality release: OO indicator refactor (`Indicator<TResult>` with `IIndicatorResult` constraint, named result records, no statics). 92 new tests. BenchmarkDotNet suite. CHANGELOG, BENCHMARKS.md, DocFX, 4 sample projects. JSON serialization fix for 9 series types. |
| **0.3.1** | Platform expansion: `@matplotlibnet/react` (React 19 hooks + components), `@matplotlibnet/vue` (Vue 3 composables + components), `MatPlotLibNet.GraphQL` (HotChocolate queries + subscriptions). Core library multi-targets `netstandard2.1`. |
| **0.3.0** | 25 series types (Donut, Bubble, OhlcBar, Waterfall, Funnel, Gantt, Gauge, ProgressBar, Sparkline). 13 technical indicators (SMA, EMA, BB, VWAP, RSI, MACD, Stochastic, Volume, Fibonacci, ATR, ADX, Keltner, Ichimoku). Trading analytics (EquityCurve, ProfitLoss, DrawDown). Buy/sell signal markers. Generic `SeriesRenderer<T>` + `Indicator<TResult>`. Intuitive fluent API (`.Sma(20)`, `.BuyAt()`, `.SaveSvg()`). PriceSource enum, Offset, LineStyle on all indicators. Series organized by chart family. |
| **0.2.0** | 16 series types (Area, Step, ErrorBar, Candlestick, Quiver, Radar). Stacked bars. Annotations (text, HLine/VLine, HSpan/VSpan). Secondary Y-axis (TwinX). SVG tooltips + zoom/pan. Polymorphic transforms (`IFigureTransform`, `FigureTransform`, `TransformResult`). PNG/PDF export via MatPlotLibNet.Skia. |
| **0.1.0** | Initial release. 10 series types (Line, Scatter, Bar, Histogram, Pie, Heatmap, Box, Violin, Contour, Stem). Fluent builder API. Parallel SVG rendering. JSON serialization. 6 themes. Blazor, ASP.NET Core, MAUI, Angular, Interactive packages. |

See [CHANGELOG.md](CHANGELOG.md) for detailed release notes. See [BENCHMARKS.md](BENCHMARKS.md) for performance numbers.

## License

[GPL-3.0](LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
