# How to use MatPlotLibNet (v0.3.2)

## Install

```
dotnet add package MatPlotLibNet
dotnet add package MatPlotLibNet.Skia    # for PNG/PDF export
```

## 1. Create a chart with the fluent API

`Plt.Create()` returns a `FigureBuilder` for method chaining:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Styling;

double[] x = [1, 2, 3, 4, 5];
double[] y = [2, 4, 3, 5, 1];

string svg = Plt.Create()
    .WithTitle("My First Chart")
    .WithSize(1000, 600)
    .WithDpi(96)
    .WithTheme(Theme.Seaborn)
    .Plot(x, y, line =>
    {
        line.Color = Color.Blue;
        line.Label = "Series A";
        line.LineStyle = LineStyle.Dashed;
        line.LineWidth = 2.0;
    })
    .Build()
    .ToSvg();
```

## 2. Chart types

All 16 chart types are available via `FigureBuilder` and `AxesBuilder`:

```csharp
// Line
builder.Plot(x, y, line => line.Color = Color.Red);

// Scatter
builder.Scatter(x, y, s => { s.Color = Color.Green; s.MarkerSize = 8; });

// Bar
builder.Bar(["Q1", "Q2", "Q3"], [100, 200, 150], bar =>
{
    bar.Color = Color.FromHex("#2196F3");
    bar.BarWidth = 0.6;
    bar.Orientation = BarOrientation.Vertical;
});

// Histogram
builder.Hist(measurements, bins: 20, h => h.Color = Color.Orange);

// Pie
builder.Pie([40, 30, 20, 10], ["A", "B", "C", "D"]);

// Step function
builder.Step(x, y, s => s.StepPosition = StepPosition.Post);

// Area / fill between
builder.FillBetween(x, y);                        // fill to y=0
builder.FillBetween(x, yUpper, yLower);           // fill between two curves

// Error bars
builder.ErrorBar(x, y, errLow, errHigh, e => e.CapSize = 8);

// Heatmap (via AxesBuilder)
ax.Heatmap(new double[,] { { 1, 2, 3 }, { 4, 5, 6 } });

// Box plot
ax.BoxPlot([[1.0, 2.0, 3.0, 4.0], [2.0, 3.0, 5.0, 7.0]]);

// Violin
ax.Violin([[1.0, 2.0, 3.0, 4.0], [2.0, 3.0, 5.0, 7.0]]);

// Contour
ax.Contour(xGrid, yGrid, zData);

// Stem
ax.Stem(x, y);

// Candlestick (OHLC financial chart)
ax.Candlestick(open, high, low, close, ["Mon", "Tue", "Wed"]);

// Quiver (vector field)
ax.Quiver(xPos, yPos, uComponent, vComponent);

// Radar (spider chart)
ax.Radar(["Speed", "Power", "Range", "Defense"], [8, 6, 9, 5]);
```

### Stacked bars

```csharp
ax.SetBarMode(BarMode.Stacked)
  .Bar(["A", "B", "C"], [10, 20, 15])
  .Bar(["A", "B", "C"], [5, 10, 8]);
```

## 3. Annotations and decorations

```csharp
ax.Plot(x, y)
  .Annotate("peak", 2.0, 4.0, a =>
  {
      a.ArrowTargetX = 1.5;
      a.ArrowTargetY = 3.5;
      a.Color = Color.Red;
  })
  .AxHLine(3.5, l => { l.Color = Color.Red; l.LineStyle = LineStyle.Dashed; })
  .AxVLine(2.0)
  .AxHSpan(3.0, 4.0, s => s.Alpha = 0.1)
  .AxVSpan(1.5, 2.5);
```

## 4. Secondary Y-axis (TwinX)

Plot series with independent Y scales:

```csharp
ax.Plot(time, temperature)
  .SetYLabel("Temperature (C)")
  .WithSecondaryYAxis(sec => sec
      .SetYLabel("Humidity (%)")
      .Plot(time, humidity, s => s.Color = Color.Orange));
```

## 5. Subplots

Use `AddSubPlot(rows, cols, index, configure)` for multi-panel figures. Subplots render in **parallel** for performance.

```csharp
var figure = Plt.Create()
    .WithSize(1200, 600)
    .WithTheme(Theme.Dark)
    .AddSubPlot(1, 2, 1, ax => ax
        .WithTitle("Temperature")
        .SetXLabel("Time")
        .SetYLabel("Celsius")
        .SetXLim(0, 24)
        .SetYLim(-10, 40)
        .Plot(time, temp, l => l.LineStyle = LineStyle.Solid)
        .ShowGrid())
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle("Distribution")
        .Hist(samples, bins: 15))
    .Build();

string svg = figure.ToSvg();
```

`AxesBuilder` methods:

| Method | Description |
|--------|-------------|
| `WithTitle(string)` | Subplot title |
| `SetXLabel(string)` / `SetYLabel(string)` | Axis labels |
| `SetXLim(min, max)` / `SetYLim(min, max)` | Axis range |
| `SetXScale(AxisScale)` / `SetYScale(AxisScale)` | Linear or Log scale |
| `ShowGrid(bool)` | Toggle grid lines |
| `SetBarMode(BarMode)` | Grouped or Stacked bars |
| `WithTooltips(bool)` | Enable native SVG hover tooltips |
| `WithSecondaryYAxis(Action)` | Add right-side Y-axis |
| `Annotate`, `AxHLine`, `AxVLine`, `AxHSpan`, `AxVSpan` | Decorations |
| `Plot`, `Scatter`, `Bar`, `Hist`, `Pie`, `Step`, `FillBetween`, `ErrorBar` | Series (FigureBuilder) |
| `Heatmap`, `BoxPlot`, `Violin`, `Contour`, `Stem`, `Candlestick`, `Quiver`, `Radar` | Series (AxesBuilder) |

## 6. Export transforms

All output formats implement `IFigureTransform` with a fluent `TransformResult`:

```csharp
using MatPlotLibNet.Transforms;

var figure = Plt.Create().Plot(x, y).Build();

// Polymorphic -- same pattern for any format
figure.Transform(new SvgTransform()).ToFile("chart.svg");
figure.Transform(new PngTransform()).ToFile("chart.png");   // requires MatPlotLibNet.Skia
figure.Transform(new PdfTransform()).ToFile("chart.pdf");   // requires MatPlotLibNet.Skia

// Get bytes or write to stream
byte[] png = figure.Transform(new PngTransform()).ToBytes();
figure.Transform(new SvgTransform()).ToStream(stream);

// Convenience shortcuts
string svg = figure.ToSvg();
byte[] pngBytes = figure.ToPng();
byte[] pdfBytes = figure.ToPdf();
```

## 7. SVG interactivity

```csharp
// Native browser tooltips -- data values shown on hover
ax.WithTooltips().Scatter(x, y);

// Zoom (mouse wheel) and pan (click-drag) -- embedded JavaScript
Plt.Create().WithZoomPan().Plot(x, y).Build()

// Double-click resets to original view. Only effective in browsers.
```

## 8. Direct model manipulation

For full control, create `Figure` and `Axes` directly:

```csharp
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

var figure = new Figure
{
    Title = "Revenue",
    Width = 1000,
    Height = 600,
    Theme = Theme.Ggplot
};

var axes = figure.AddSubPlot();
axes.Title = "Quarterly Sales";
axes.XAxis.Label = "Quarter";
axes.YAxis.Label = "Revenue ($)";
axes.Grid = axes.Grid with { Visible = true };

var bars = axes.Bar(["Q1", "Q2", "Q3", "Q4"], [100, 150, 200, 175]);
bars.Color = Color.FromHex("#2196F3");
bars.Label = "2026";

axes.AxHLine(150, line => { line.Label = "Target"; line.Color = Color.Red; });
axes.Annotate("Best quarter", 2, 200);

string svg = figure.ToSvg();
```

Note: `Grid`, `Legend`, and `TickConfig` are immutable records -- use `with` expressions to modify them.

## 9. Dependency injection

All rendering and serialization goes through interfaces:

```csharp
// In ASP.NET Core -- services registered automatically
builder.Services.AddMatPlotLibNetSignalR();
// Registers: IChartSerializer, IChartRenderer, ISvgRenderer, IChartPublisher

// In console apps -- use ChartServices static defaults
string svg = ChartServices.SvgRenderer.Render(figure);
string json = ChartServices.Serializer.ToJson(figure);

// Replace with your own implementation
ChartServices.Serializer = new MyCustomSerializer();
```

The extension methods `figure.ToSvg()` and `figure.ToJson()` delegate to `ChartServices` under the hood.

## 10. Themes

Six built-in themes:

```csharp
Theme.Default          // white background, classic matplotlib
Theme.Dark             // dark gray background, light text
Theme.Seaborn          // light gray, statistical style
Theme.Ggplot           // R ggplot2 style
Theme.Bmh              // Bayesian Methods style
Theme.FiveThirtyEight  // journalism style
```

Custom themes with `ThemeBuilder` and immutable records:

```csharp
var myTheme = Theme.CreateFrom(Theme.Dark)
    .WithBackground(Color.FromHex("#1a1a2e"))
    .WithForegroundText(Color.White)
    .WithAxesBackground(Color.FromHex("#16213e"))
    .WithCycleColors(Color.Cyan, Color.Magenta, Color.Yellow)
    .WithFont(f => f with { Family = "Consolas", Size = 14 })
    .WithGrid(g => g with { Visible = true, Alpha = 0.3 })
    .Build();

var svg = Plt.Create()
    .WithTheme(myTheme)
    .Plot(x, y)
    .Build()
    .ToSvg();
```

## 11. JSON serialization

Figures round-trip to JSON for storage or API transport:

```csharp
// Serialize
string json = figure.ToJson(indented: true);

// Deserialize
Figure restored = ChartServices.Serializer.FromJson(json);

// Re-render
string svg = restored.ToSvg();
```

## 12. Colors

```csharp
// Named colors
Color.Red, Color.Blue, Color.Green, Color.Orange, Color.White, Color.Black

// From hex
Color.FromHex("#FF5722");

// From normalized RGBA (0.0 - 1.0)
Color.FromRgba(1.0, 0.34, 0.13, 1.0);

// With alpha
var transparent = Color.Blue.WithAlpha(128);
```

`Color` is a `readonly record struct` -- value equality, immutable.

## 13. Color maps

Built-in color maps for heatmaps and contour plots:

```csharp
using MatPlotLibNet.Styling.ColorMaps;

ColorMaps.Viridis     // perceptually uniform, default
ColorMaps.Plasma
ColorMaps.Inferno
ColorMaps.Magma
ColorMaps.Coolwarm    // diverging blue-red
ColorMaps.Blues
ColorMaps.Reds
```

Use on a heatmap:

```csharp
var heatmap = axes.Heatmap(data);
heatmap.ColorMap = ColorMaps.Plasma;
```

## 14. Real-time subscription client

`IChartSubscriptionClient` is the shared contract for receiving live chart updates via SignalR. Implemented in C# (Blazor) and TypeScript (Angular):

```csharp
// Register in DI or create directly
var client = new MatPlotLibNet.Blazor.ChartSubscriptionClient();

client.OnSvgUpdated(async (chartId, svg) =>
{
    // handle SVG update
});

await client.ConnectAsync("/charts-hub");
await client.SubscribeAsync("sensor-1");

// Later
await client.DisposeAsync();
```

## 15. Display modes

`DisplayMode` enum controls how chart components present the chart:

```csharp
DisplayMode.Inline      // SVG rendered directly in the component (default)
DisplayMode.Expandable  // inline with a fullscreen overlay toggle
DisplayMode.Popup       // thumbnail with "open in new tab" link
```

Used by Blazor's `MplChart` component and applicable to Angular components.
