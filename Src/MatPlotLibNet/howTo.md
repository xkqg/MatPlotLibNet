# How to use MatPlotLibNet (v0.7.0)


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

All 40 chart types are available via `FigureBuilder` and `AxesBuilder`:

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

---

## 16. Tick locators and formatters (v0.5.1)

By default, MatPlotLibNet uses a nice-number algorithm to choose 5 aesthetically-spaced ticks.
You can override this per-axis with any `ITickLocator` implementation.

### Built-in locators

| Locator | Behaviour |
|---------|-----------|
| `AutoLocator(n)` | Nice numbers, ~n ticks (default algorithm, now a first-class object) |
| `MaxNLocator(n)` | Nice numbers, at most n ticks |
| `MultipleLocator(base)` | Ticks at exact multiples of `base` (e.g., every 0.25) |
| `FixedLocator(positions[])` | Exactly the given positions within range |
| `LogLocator` | Powers of 10 (for log-scale axes) |

```csharp
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering.TickFormatters;

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y)
        .SetXTickLocator(new MultipleLocator(25))      // ticks every 25 units
        .SetYTickLocator(new MaxNLocator(4))            // at most 4 Y ticks
        .SetYTickFormatter(new EngFormatter())          // 1000 → "1k"
        .WithMinorTicks())                              // subdivide major intervals by 5
    .Save("chart.svg");
```

### Built-in formatters

| Formatter | Example output |
|-----------|---------------|
| `EngFormatter` | 1000 → "1k", 1500 → "1.5k", 0.001 → "1m", 1e6 → "1M" |
| `PercentFormatter(max)` | `value/max*100` + "%" — e.g., `new PercentFormatter(1.0)` formats 0.5 → "50%" |
| `NumericTickFormatter` | Default: G5, scientific for large/tiny values |
| `DateTickFormatter` | OLE Automation dates, configurable format string |

Axis-level locator and formatter:

```csharp
// Direct model access
axes.XAxis.TickLocator = new FixedLocator([0, Math.PI, 2 * Math.PI]);
axes.XAxis.TickFormatter = new NumericTickFormatter();
axes.YAxis.MajorTicks = axes.YAxis.MajorTicks with { Spacing = 0.5 }; // auto MultipleLocator
axes.YAxis.MinorTicks = axes.YAxis.MinorTicks with { Visible = true };
```

---

## 17. Annotation enhancements (v0.5.1)

Annotations gained four new properties for richer labeling:

```csharp
axes.Annotate("Max value", dataX, dataY, ann =>
{
    ann.ArrowTargetX     = dataX;
    ann.ArrowTargetY     = dataY;
    ann.ArrowStyle       = ArrowStyle.FancyArrow;    // triangular arrowhead
    ann.BackgroundColor  = Color.White;              // highlight box behind text
    ann.Alignment        = TextAlignment.Center;
    ann.Rotation         = -30;                      // degrees
});
```

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| `Alignment` | `TextAlignment` | `Left` | `Left`, `Center`, `Right` |
| `Rotation` | `double` | `0` | Degrees; positive = CCW in standard math |
| `ArrowStyle` | `ArrowStyle` | `Simple` | `None`, `Simple`, `FancyArrow` |
| `BackgroundColor` | `Color?` | `null` | Fill rect behind label |

Fluent:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y)
        .Annotate("Peak", 8, 9.0, ann =>
        {
            ann.ArrowTargetX = 8; ann.ArrowTargetY = 9.0;
            ann.ArrowStyle   = ArrowStyle.FancyArrow;
            ann.BackgroundColor = Color.White;
        }))
    .Save("annotated.svg");
```

### Bar labels

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Bar(categories, values)
        .WithBarLabels("F0"))   // integer labels above each bar
    .Save("bar_labels.svg");
```

---

## 18. Performance — LTTB downsampling (v0.5.1)

For large datasets (>5 000 points), rendering every point is wasteful and visually indistinguishable.
The **Largest-Triangle-Three-Buckets** (LTTB) algorithm selects O(target) representative points that
preserve the visual shape (peaks, troughs, changes) of the original series.

```csharp
// Opt-in via MaxDisplayPoints (null = no downsampling)
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(largeX, largeY)
        .WithDownsampling(500))   // cull to viewport, then LTTB to 500 pts
    .Save("chart.svg");
```

The pipeline is:
1. **Viewport cull** — discard points outside the current X axis range (keep one each side for line clipping)
2. **LTTB** — if culled count > `maxPoints`, run LTTB; always preserves first and last point

Lower-level access:

```csharp
using MatPlotLibNet.Rendering.Downsampling;

// Standalone LTTB
var (outX, outY) = new LttbDownsampler().Downsample(x, y, targetPoints: 300);

// Standalone viewport cull
var (cx, cy) = ViewportCuller.Cull(x, y, xMin: 100, xMax: 500);

// Direct on model
lineSeries.MaxDisplayPoints = 1000;
```

Applies to: `LineSeries`, `AreaSeries`, `ScatterSeries`, `StepSeries`.


## §19 Vec & VectorMath

`Vec` is a LINQ-style wrapper over `double[]` with SIMD-accelerated operators and reductions backed by `System.Numerics.Tensors.TensorPrimitives`.

```csharp
using MatPlotLibNet.Numerics;

double[] closeArr = GetClosePrices();
Vec close = closeArr;   // implicit conversion

// SIMD-accelerated operators (allocate new arrays)
Vec shifted = close.Slice(1, close.Length - 1);
Vec prev    = close.Slice(0, close.Length - 1);
Vec diff    = shifted - prev;
Vec returns = diff.Zip(prev, (d, p) => p == 0 ? 0 : d / p * 100);

// SIMD reductions (zero allocation)
double avg  = returns.Mean();
double risk = returns.Std();
double best = returns.Max();

// Scalar lambdas (chain-friendly, not SIMD)
Vec gains = returns.Where(r => r > 0);
Vec log   = close.Select(v => Math.Log(v));
```

`VectorMath` is `internal` plumbing — use `Vec` from your code, use `VectorMath` only inside the library.

## §20 Chart Templates

`FigureTemplates` provides pre-built layouts for common scenarios.

```csharp
using MatPlotLibNet;

// 3-panel financial dashboard
FigureTemplates.FinancialDashboard(open, high, low, close, volume, title: "AAPL")
    .Build()
    .Save("dashboard.svg");

// Scientific paper — 150 DPI, hidden spines, tight layout
FigureTemplates.ScientificPaper(rows: 2, cols: 2, title: "Results")
    // returns FigureBuilder — add data before saving
    .AddSubPlot(2, 2, 1, ax => ax.Plot(x1, y1, s => s.Label = "Series A"))
    .AddSubPlot(2, 2, 2, ax => ax.Scatter(x2, y2))
    .ToSvg();

// Sparkline dashboard
FigureTemplates.SparklineDashboard([
    ("Revenue", revenueData),
    ("Costs",   costsData),
    ("Profit",  profitData)
]).Save("sparklines.svg");
```

Each method returns a `FigureBuilder`, so you can chain `.WithTitle()`, `.AddSubPlot()`, etc.

## §21 Contour Labels

Enable contour level labels with `ShowLabels = true`. The labels use marching-squares to find iso-line midpoints and render a centered value with a white background.

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax =>
        ax.Contour(xGrid, yGrid, zGrid, c =>
        {
            c.Levels      = 8;
            c.ShowLabels  = true;
            c.LabelFormat = "F1";   // one decimal place
            c.LabelFontSize = 9;
        })
        .WithColorMap("RdBu"))
    .Save("contour.svg");
```

## §22 Polyglot Notebooks

Add the Notebooks package in a `.dib` or `.ipynb` notebook cell:

```csharp
#r "nuget: MatPlotLibNet.Notebooks"

using MatPlotLibNet;

// Return a Figure from any cell — it renders inline as SVG
Plt.Create()
    .WithTitle("Hello Notebooks")
    .Plot([1.0, 2, 3, 4, 5], [2.0, 4, 3, 5, 1])
    .Build()
```

## §23 Phase F Indicators (v0.6.0)

Four new technical indicators, each with an `AxesBuilder` shortcut:

```csharp
Plt.Create()
    .WithGridSpec(2, 2)
    // Williams %R — momentum oscillator, range [-100, 0]
    .AddSubPlot(new GridPosition(0, 1, 0, 1), ax =>
        ax.WilliamsR(high, low, close, period: 14))
    // On-Balance Volume — cumulative volume indicator
    .AddSubPlot(new GridPosition(0, 1, 1, 2), ax =>
        ax.Obv(close, volume))
    // Commodity Channel Index — mean-deviation oscillator, reference at +/-100
    .AddSubPlot(new GridPosition(1, 2, 0, 1), ax =>
        ax.Cci(high, low, close, period: 20))
    // Parabolic SAR — trend dots above/below price
    .AddSubPlot(new GridPosition(1, 2, 1, 2), ax =>
    {
        ax.Plot(Enumerable.Range(0, close.Length).Select(i => (double)i).ToArray(), close);
        ax.ParabolicSar(high, low, step: 0.02, max: 0.2);
    })
    .Save("indicators.svg");
```

All four use the SIMD `VectorMath` kernel internally. `WilliamsR` and `CCI` use O(n) monotone-deque rolling min/max. `ParabolicSar` renders as two scatter series (long = green, short = red) — customize via `LongColor`/`ShortColor`.

---

## §24 KDE, Regression, Hexbin, JointPlot (v0.7.0)

### KdeSeries — kernel density estimation

Plots a smoothed density curve (and optional fill) for a 1D sample.

```csharp
using MatPlotLibNet.Numerics; // for LeastSquares

double[] samples = GetSamples(); // raw data

// Auto Silverman bandwidth, filled
Plt.Create()
    .Kde(samples, k =>
    {
        k.Fill  = true;
        k.Alpha = 0.25;
        k.Color = Color.Tab10Blue;
    })
    .Save("kde.svg");

// Side-by-side distributions in one subplot
Plt.Create()
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.Kde(groupA, k => { k.Label = "Group A"; k.Color = Color.Tab10Blue; });
        ax.Kde(groupB, k => { k.Label = "Group B"; k.Color = Color.Tab10Orange; });
        ax.WithLegend();
    })
    .Save("comparison.svg");

// Explicit bandwidth
ax.Kde(data, k => k.Bandwidth = 0.5);
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Data` | `double[]` | required | Raw sample values |
| `Bandwidth` | `double?` | `null` | Gaussian bandwidth; null = Silverman's rule (1.06σn⁻⁰²) |
| `Fill` | `bool` | `true` | Fill area under the density curve |
| `Alpha` | `double` | `0.3` | Fill opacity |
| `LineWidth` | `double` | `1.5` | Density curve stroke width |
| `Color` | `Color?` | auto | Line and fill color |
| `LineStyle` | `LineStyle` | `Solid` | Stroke style |

### RegressionSeries — polynomial regression with confidence bands

```csharp
// Linear regression (degree 1, default)
ax.Regression(xData, yData);

// Quadratic with 95% confidence band
ax.Regression(xData, yData, r =>
{
    r.Degree = 2;
    r.ShowConfidence = true;
    r.ConfidenceLevel = 0.95;
    r.BandAlpha = 0.2;
    r.Color = Color.Red;
});
```

Lower-level access via `LeastSquares`:

```csharp
using MatPlotLibNet.Numerics;

double[] coeff = LeastSquares.PolyFit(x, y, degree: 2);
double[] yFit  = LeastSquares.PolyEval(coeff, evalX);
var (upper, lower) = LeastSquares.ConfidenceBand(x, y, coeff, evalX, level: 0.95);
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Degree` | `int` | `1` | Polynomial degree (0–10) |
| `ShowConfidence` | `bool` | `false` | Draw confidence band polygon |
| `ConfidenceLevel` | `double` | `0.95` | CI level (e.g. 0.95 for 95%) |
| `BandAlpha` | `double` | `0.2` | Band fill opacity |
| `BandColor` | `Color?` | auto | Band fill color (defaults to line color) |
| `LineWidth` | `double` | `2.0` | Regression line stroke width |

### HexbinSeries — 2D hexagonal density

```csharp
ax.Hexbin(x, y, h =>
{
    h.GridSize = 25;   // number of hex columns
    h.MinCount = 2;    // hide bins with fewer than N points
})
.WithColorMap("YlOrRd")
.WithColorBar(cb => cb with { Label = "Count" });
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `GridSize` | `int` | `20` | Number of columns in the hex grid |
| `MinCount` | `int` | `1` | Bins with fewer points are hidden |
| `ColorMap` | `IColorMap?` | `null` | Colormap for count → color (falls back to Viridis) |
| `Normalizer` | `INormalizer?` | `null` | Value normalizer |

### JointPlot — scatter with marginal histograms

```csharp
FigureTemplates.JointPlot(x, y, title: "Height vs Weight", bins: 25)
    .Save("joint.svg");
```

Layout: 2×2 GridSpec with `heightRatios=[1,4]`, `widthRatios=[4,1]`.
- Top-left (spanning top row): X marginal histogram
- Center: scatter plot
- Right (spanning right column): Y marginal histogram

---

## §25 Interactive SVG (v0.7.0)

Five `FigureBuilder` methods inject embedded JavaScript into the SVG for browser interactivity. All scripts are self-contained and operate on `data-series-index` / `data-legend-index` DOM attributes.

### Legend toggle

Click a legend entry to show or hide the corresponding series:

```csharp
Plt.Create()
    .WithLegendToggle()
    .Plot(x, train, s => s.Label = "Train")
    .Plot(x, test,  s => s.Label = "Test")
    .Save("toggle.svg");
```

### Highlight on hover

Hovering over a series dims all siblings to 30% opacity:

```csharp
Plt.Create()
    .WithHighlight()
    .Plot(x, y1, s => s.Label = "A")
    .Plot(x, y2, s => s.Label = "B")
    .Save("highlight.svg");
```

### Rich tooltips

Replaces the native browser `<title>` tooltip with a styled floating `div`:

```csharp
Plt.Create()
    .WithRichTooltips()
    .AddSubPlot(1, 1, 1, ax => ax.WithTooltips().Scatter(x, y))
    .Save("tooltips.svg");
```

### Selection

Shift+drag draws a selection rectangle. On mouseup, a `CustomEvent('mpl:selection', { detail: { x1, y1, x2, y2 } })` fires on the SVG element — listen for it in JavaScript to react to user-defined data regions:

```csharp
Plt.Create()
    .WithSelection()
    .Plot(x, y)
    .Save("select.svg");
```

```javascript
// In your HTML/JS host
document.querySelector('svg').addEventListener('mpl:selection', e => {
    const { x1, y1, x2, y2 } = e.detail;
    console.log('Selected SVG coords:', x1, y1, x2, y2);
});
```

### Combining features

All four interactivity flags compose freely:

```csharp
Plt.Create()
    .WithLegendToggle()
    .WithHighlight()
    .WithRichTooltips()
    .WithSelection()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTooltips()
        .Plot(x, y1, s => s.Label = "Series A")
        .Plot(x, y2, s => s.Label = "Series B")
        .WithLegend())
    .Save("full_interactive.svg");
```

| Method | Script | Requires |
|--------|--------|---------|
| `WithLegendToggle()` | `SvgLegendToggleScript` | `data-legend-index` on legend entries |
| `WithHighlight()` | `SvgHighlightScript` | `data-series-index` on series groups |
| `WithRichTooltips()` | `SvgCustomTooltipScript` | `<title>` elements (via `WithTooltips()`) |
| `WithSelection()` | `SvgSelectionScript` | none — always active when enabled |
| `WithZoomPan()` | `SvgInteractivityScript` | none — standalone |

---

## §27 Style Sheets / rcParams (v0.7.0)

MatPlotLibNet now supports matplotlib-style global configuration via `RcParams` and scoped overrides via `StyleContext`.

### Global defaults

`RcParams.Default` holds the global parameter dictionary. Modify it directly or use `Plt.Style.Use()`:

```csharp
using MatPlotLibNet.Styling;

// Apply a named style globally (matches matplotlib's plt.style.use())
Plt.Style.Use("seaborn");

// Or apply a custom StyleSheet
Plt.Style.Use(new StyleSheet("my-style", new Dictionary<string, object>
{
    [RcParamKeys.FontSize] = 14.0,
    [RcParamKeys.LinesLineWidth] = 2.0,
    [RcParamKeys.AxesGrid] = true,
    [RcParamKeys.GridAlpha] = 0.3,
    [RcParamKeys.FigureFaceColor] = Color.FromHex("#F5F5F5"),
}));
```

### Scoped overrides

`Plt.Style.Context()` returns an `IDisposable` that temporarily overrides parameters for the current async flow:

```csharp
// Scoped override — auto-reverts on Dispose
using (Plt.Style.Context("dark"))
{
    // All figures created here use dark style
    Plt.Create().Plot(x, y).Save("dark_chart");
}
// Back to previous style

// Override individual parameters
using (Plt.Style.Context(new Dictionary<string, object>
{
    [RcParamKeys.FontSize] = 16.0,
    [RcParamKeys.LinesLineWidth] = 2.5,
}))
{
    Plt.Create().Plot(x, y).Save("custom_chart");
}

// Nesting works — inner scope overrides outer
using (Plt.Style.Context("seaborn"))
{
    using (Plt.Style.Context(new Dictionary<string, object>
    {
        [RcParamKeys.FontSize] = 18.0,  // override just font size
    }))
    {
        // seaborn style + larger font
    }
}
```

### Precedence

Parameters resolve in strict priority order:
1. **Explicit property** on the object (`series.LineWidth = 2.0`)
2. **Explicit Theme** on the Figure (`figure.Theme = Theme.Dark`)
3. **`RcParams.Current`** scoped override (from `StyleContext`)
4. **`RcParams.Default`** hard-coded defaults

This ensures 100% backward compatibility — existing code with explicit properties or themes sees zero behavior change.

### Supported parameters

| Key | Type | Default | Maps to |
|-----|------|---------|---------|
| `font.family` | `string` | `"sans-serif"` | `Font.Family` |
| `font.size` | `double` | `12` | `Font.Size` |
| `font.weight` | `FontWeight` | `Normal` | `Font.Weight` |
| `lines.linewidth` | `double` | `1.5` | `LineSeries.LineWidth` |
| `lines.linestyle` | `LineStyle` | `Solid` | `LineSeries.LineStyle` |
| `lines.markersize` | `double` | `6` | `LineSeries.MarkerSize` |
| `axes.facecolor` | `Color` | `White` | `Theme.AxesBackground` |
| `axes.grid` | `bool` | `false` | `GridStyle.Visible` |
| `grid.color` | `Color` | `#CCCCCC` | `GridStyle.Color` |
| `grid.linewidth` | `double` | `0.5` | `GridStyle.LineWidth` |
| `grid.alpha` | `double` | `0.7` | `GridStyle.Alpha` |
| `figure.figsize.width` | `double` | `800` | `Figure.Width` |
| `figure.figsize.height` | `double` | `600` | `Figure.Height` |
| `figure.dpi` | `double` | `96` | `Figure.Dpi` |
| `figure.facecolor` | `Color` | `White` | `Figure.BackgroundColor` |
| `scatter.markersize` | `double` | `36` | `ScatterSeries.MarkerSize` |
| `image.cmap` | `string` | `"viridis"` | Default colormap name |
| `text.color` | `Color` | `Black` | `Theme.ForegroundText` |

### Built-in style sheets

Six style sheets derived from the existing themes:

| Name | Source Theme |
|------|-------------|
| `"default"` | `Theme.Default` |
| `"dark"` | `Theme.Dark` |
| `"seaborn"` | `Theme.Seaborn` |
| `"ggplot"` | `Theme.Ggplot` |
| `"bmh"` | `Theme.Bmh` |
| `"fivethirtyeight"` | `Theme.FiveThirtyEight` |

### Theme bridge

Convert any `Theme` to a `StyleSheet`:

```csharp
StyleSheet sheet = Theme.Dark.ToStyleSheet();
Plt.Style.Use(sheet);

// Or register a custom theme as a style sheet
var myTheme = Theme.CreateFrom(Theme.Seaborn)
    .WithFont(f => f with { Size = 14 })
    .Build();
StyleSheetRegistry.Register("my-seaborn", myTheme.ToStyleSheet());
Plt.Style.Use("my-seaborn");
```

### Thread safety

`StyleContext` uses `AsyncLocal<T>`, which flows correctly across `async`/`await` boundaries but does not share state between unrelated threads. Each async flow gets its own scope — safe for concurrent request handling in ASP.NET Core.

---

## §28 Filled Contours — contourf (v0.7.0)

`ContourfSeries` renders colored bands between consecutive iso-levels — the filled counterpart to `ContourSeries` (which draws iso-lines).

### Basic usage

```csharp
double[] x = Enumerable.Range(0, 50).Select(i => i * 0.1).ToArray();
double[] y = Enumerable.Range(0, 50).Select(i => i * 0.1).ToArray();
var z = new double[50, 50];
for (int r = 0; r < 50; r++)
for (int c = 0; c < 50; c++)
    z[r, c] = Math.Sin(x[c]) * Math.Cos(y[r]);

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Contourf(x, y, z, cf =>
        {
            cf.Levels = 12;
            cf.Alpha = 0.9;
        })
        .WithColorMap("RdBu")
        .WithColorBar(cb => cb with { Label = "Amplitude" }))
    .Save("contourf.svg");
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Levels` | `int` | `10` | Number of iso-levels (bands = levels + 1) |
| `Alpha` | `double` | `1.0` | Fill opacity (0.0–1.0) |
| `ShowLines` | `bool` | `true` | Overlay iso-lines on top of filled bands |
| `LineWidth` | `double` | `0.5` | Width of overlaid iso-lines |
| `ColorMap` | `IColorMap?` | `null` | Color mapping (falls back to Viridis) |
| `Normalizer` | `INormalizer?` | `null` | Value-to-[0,1] normalizer |

### Algorithm

Uses a **painter's algorithm** internally:
1. Computes evenly-spaced levels from data min to max
2. Fills entire plot area with the lowest band color
3. For each ascending level: extracts closed contour polygons via marching squares, fills with the next band color
4. Optionally overlays iso-lines

### Contour vs. Contourf

| | `ContourSeries` | `ContourfSeries` |
|-|----------------|------------------|
| Rendering | Iso-lines (stroked paths) | Filled bands (colored polygons) |
| Labels | `ShowLabels` at midpoints | Not applicable |
| Alpha | Not supported | `Alpha` property |
| Color bar | Not built-in | `IColorBarDataProvider` |

---

## §29 Image Interpolation & Compositing (v0.7.0)

`ImageSeries` now supports bilinear and bicubic interpolation for smooth image rendering, plus alpha compositing with blend modes.

### Interpolation

```csharp
// Smooth rendering with bilinear interpolation
ax.Image(data, img =>
{
    img.Interpolation = "bilinear";   // "nearest" (default), "bilinear", "bicubic"
    img.ColorMap = ColorMaps.Plasma;
});
```

| Engine | Quality | Speed | Best for |
|--------|---------|-------|----------|
| `"nearest"` | Blocky | Fastest | Categorical data, pixel art, small grids |
| `"bilinear"` | Smooth | Fast | General-purpose continuous data |
| `"bicubic"` | Smoothest | Moderate | Publication-quality images, scientific data |

Bicubic uses a Catmull-Rom kernel with output clamping to prevent ringing artifacts.

For SVG output, the data grid is upsampled to a higher resolution (capped at min(source×4, 256)) then rendered as smaller rectangles.

### Alpha compositing

```csharp
ax.Image(data, img =>
{
    img.Alpha = 0.7;                      // overall opacity
    img.BlendMode = BlendMode.Multiply;   // blend with background
});
```

| Blend mode | Formula (per channel, [0,1]) | Use case |
|------------|------------------------------|----------|
| `Normal` | `src × a + dst × (1-a)` | Standard transparency |
| `Multiply` | `src × dst` | Darkening overlay |
| `Screen` | `1 - (1-src)(1-dst)` | Lightening overlay |
| `Overlay` | `dst<0.5 ? 2·src·dst : 1-2·(1-src)(1-dst)` | Contrast enhancement |

### Custom interpolation engine

Register a custom interpolation engine with `InterpolationRegistry`:

```csharp
using MatPlotLibNet.Rendering.Interpolation;

public class LanczosInterpolation : IInterpolationEngine
{
    public string Name => "lanczos";
    public double[,] Resample(double[,] data, int targetRows, int targetCols)
    {
        // Your implementation here
    }
}

InterpolationRegistry.Register("lanczos", new LanczosInterpolation());

// Use by name
ax.Image(data, img => img.Interpolation = "lanczos");
```
