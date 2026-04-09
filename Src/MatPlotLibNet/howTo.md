# How to use MatPlotLibNet (v0.5.0)


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

// Save directly — no Build() needed, format auto-detected from extension
Plt.Create()
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
    .Save("chart");  // no extension = SVG by default

// Or get SVG as a string
string svg = Plt.Create().Plot(x, y).ToSvg();
```

## 2. Chart types

All 39 chart types are available via `FigureBuilder` and `AxesBuilder`:

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

// Image (imshow) — display 2D data as colored pixels
ax.Image(data, img => { img.ColorMap = ColorMaps.Inferno; img.VMin = 0; img.VMax = 1; });

// 2D histogram — density plot binned from scatter data
ax.Histogram2D(xData, yData, binsX: 25, binsY: 25);

// ECDF — empirical cumulative distribution function
ax.Ecdf(measurements);

// Stacked area (stackplot)
ax.StackPlot(x, [y1, y2, y3], labels: ["A", "B", "C"]);

// Streamplot — vector field streamlines
ax.Streamplot(xGrid, yGrid, u, v);

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

// Quiver (vector field arrows)
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

## 4b. Advanced layouts

### GridSpec — unequal subplot sizes

```csharp
Plt.Create()
    .WithGridSpec(2, 2, heightRatios: [2.0, 1.0], widthRatios: [3.0, 1.0])
    .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot(x, y).WithTitle("Main"))
    .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Scatter(x, y))
    .AddSubPlot(GridPosition.Span(1, 2, 0, 2), ax => ax.Bar(cats, vals).WithTitle("Wide bar"))
    .Save("gridspec");
```

`WithGridSpec(rows, cols, heightRatios, widthRatios)` creates proportional row/column sizes.
`AddSubPlot(GridPosition.Single(row, col), configure)` places a subplot at a grid cell.
`GridPosition.Span(rowStart, rowEnd, colStart, colEnd)` spans multiple cells (exclusive end indices).

### Shared axes

Linked subplots share a common range — panning one updates all:

```csharp
Plt.Create()
    .AddSubPlot(2, 1, 1, ax => ax.ShareX("group1").Plot(x, y1))
    .AddSubPlot(2, 1, 2, ax => ax.ShareX("group1").Plot(x, y2))
    .Save("shared_x");
```

`ShareX(key)` / `ShareY(key)` take an arbitrary string key; all subplots using the same key share range.

### Spine control

```csharp
// Quick helpers
ax.HideTopSpine().HideRightSpine();

// Fine-grained via SpinesConfig
ax.WithSpines(s => s with
{
    Bottom = s.Bottom with { Position = SpinePosition.Data(0) }   // move x-axis to y=0
});
```

### Inset axes

```csharp
ax.Plot(x, y)
  .AddInset(0.6, 0.6, 0.35, 0.35, inset => inset
      .Plot(xZoom, yZoom)
      .WithTitle("Detail"));
```

`AddInset(x, y, w, h, configure)` — coordinates are fractions of the parent axes (0–1). Insets nest up to 3 levels deep.

## 5. Subplots

Use `AddSubPlot(rows, cols, index, configure)` for multi-panel figures. Subplots render in **parallel** for performance.

```csharp
Plt.Create()
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
    .Save("subplots.svg");
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

## 6. Export

Save directly from the builder -- no `.Build()` needed:

```csharp
// Auto-detect format from file extension
Plt.Create().Plot(x, y).Save("chart.svg");
Plt.Create().Plot(x, y).Save("chart.png");    // requires MatPlotLibNet.Skia
Plt.Create().Plot(x, y).Save("chart.pdf");    // requires MatPlotLibNet.Skia
Plt.Create().Plot(x, y).Save("chart.json");

// Get strings directly
string svg = Plt.Create().Plot(x, y).ToSvg();
string json = Plt.Create().Plot(x, y).ToJson();

// Register PNG/PDF once at startup
FigureExtensions.RegisterTransform(".png", new PngTransform());
FigureExtensions.RegisterTransform(".pdf", new PdfTransform());
```

For advanced usage with explicit transforms (when you need the Figure object):

```csharp
using MatPlotLibNet.Transforms;

var figure = Plt.Create().Plot(x, y).Build();

figure.Transform(new SvgTransform()).ToFile("chart.svg");
figure.Transform(new PngTransform()).ToFile("chart.png");

byte[] png = figure.Transform(new PngTransform()).ToBytes();
figure.Transform(new SvgTransform()).ToStream(stream);
```

## 7. SVG interactivity

```csharp
// Native browser tooltips -- data values shown on hover
ax.WithTooltips().Scatter(x, y);

// Zoom (mouse wheel) and pan (click-drag) -- embedded JavaScript
Plt.Create().WithZoomPan().Plot(x, y).Save("zoomable")

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

### Python/matplotlib-style imperative API

If you come from Python's matplotlib, the `new Figure()` + `AddSubPlot()` pattern maps directly to how `fig, ax = plt.subplots()` works — you get explicit handles to both the figure and each axes, and mutate them in place:

```python
# Python matplotlib
fig, (ax1, ax2) = plt.subplots(1, 2, sharex=True)
ax1.plot(x, y1)
ax2.bar(cats, vals)
plt.savefig("chart.svg")
```

```csharp
// C# equivalent
var fig = new Figure { Width = 1200, Height = 600 };
var ax1 = fig.AddSubPlot(1, 2, 1);
var ax2 = fig.AddSubPlot(1, 2, 2, sharex: ax1);   // shared X range
ax1.Plot(x, y1);
ax2.Bar(cats, vals);
fig.ToSvg();
```

This style is particularly useful when building figures programmatically — dynamic panel counts, updating axes after construction, or passing axes to separate methods:

```csharp
var fig = new Figure();
foreach (var (dataset, i) in datasets.Select((d, i) => (d, i)))
{
    var ax = fig.AddSubPlot(datasets.Count, 1, i + 1);
    ax.Title = dataset.Name;
    ax.Plot(dataset.X, dataset.Y);
}
string svg = fig.ToSvg();
```

`Plt.Create()` (fluent builder) and `new Figure()` (direct model) produce identical output — choose whichever fits the context.

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

Plt.Create()
    .WithTheme(myTheme)
    .Plot(x, y)
    .Save("themed_chart");
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

52 built-in colormaps across 6 categories (104 total including reversed `_r` variants):

| Category | Count | Maps | When to use |
|----------|-------|------|-------------|
| Perceptually-uniform | 6 | Viridis (default), Plasma, Inferno, Magma, Turbo, Cividis | Continuous numerical data |
| Sequential | 21 | Blues, Reds, Greens, Oranges, Purples, Greys, Hot, Copper, Bone, BuPu, GnBu, PuRd, RdPu, YlGnBu, PuBuGn, YlOrBr, YlOrRd, OrRd, PuBu, YlGn, BuGn, Cubehelix | Light→dark single-hue ramp |
| Diverging | 10 | Coolwarm, RdBu, RdYlGn, RdYlBu, BrBG, PiYG, Spectral, PuOr, Seismic, Bwr | Data with meaningful center (e.g. diverging from zero) |
| Cyclic | 3 | Twilight, TwilightShifted, Hsv | Phase angles, time-of-day |
| Qualitative | 10 | Tab10, Tab20, Set1, Set2, Set3, Pastel1, Pastel2, Dark2, Accent, Paired | Categorical, unordered |
| Legacy | 2 | Jet, Jet_r | Rainbow (prefer Turbo instead) |

### Usage patterns

```csharp
using MatPlotLibNet.Styling.ColorMaps;

// 1. Fluent API — recommended
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Heatmap(data)
        .WithColorMap("plasma")
        .WithNormalizer(new LogNormalizer())
        .WithColorBar(cb => cb with { Label = "Intensity" }))
    .Save("heatmap");

// 2. Direct property assignment
var hm = axes.Heatmap(data);
hm.ColorMap = ColorMaps.Plasma;
hm.Normalizer = new TwoSlopeNormalizer(center: 0);

// 3. Registry lookup — case-insensitive, _r for reversed
var map = ColorMapRegistry.Get("rdylgn");
var reversed = ColorMapRegistry.Get("rdylgn_r");
```

### Reversed variants

Every colormap automatically registers a `_r` reversed variant — 52 → 104 names:

```csharp
ColorMapRegistry.Get("viridis_r")   // flips dark→light
ColorMapRegistry.Get("coolwarm_r")  // flips red→blue
```

### Normalizers

Normalizers map data values to [0, 1] before color lookup:

| Normalizer | When to use |
|------------|-------------|
| `LinearNormalizer.Instance` | default, evenly spaced data |
| `new LogNormalizer()` | power-law / wide-range data |
| `new TwoSlopeNormalizer(center)` | diverging data with asymmetric range |
| `new BoundaryNormalizer(double[])` | discrete bins, step-function mapping |

### Colormappable series

Seven series implement `IColormappable` and accept `.WithColorMap()` / `.WithNormalizer()`:
`HeatmapSeries`, `ImageSeries`, `Histogram2DSeries`, `ContourSeries`, `SurfaceSeries`, `ScatterSeries`, `HierarchicalSeries` (Treemap/Sunburst).

### Custom colormap

```csharp
public class MyMap : IColorMap
{
    public string Name => "my_gradient";
    public Color GetColor(double value) =>
        new((byte)(value * 255), 0, (byte)((1 - value) * 255));
}

ColorMapRegistry.Register("my_gradient", new MyMap());
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
