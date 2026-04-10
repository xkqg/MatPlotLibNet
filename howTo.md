# MatPlotLibNet How-To Guide

## Getting started

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Styling;

double[] x = [1, 2, 3, 4, 5];
double[] y = [2, 4, 3, 5, 1];

// Create a chart and save — no Build() needed
Plt.Create()
    .WithTitle("My First Chart")
    .Plot(x, y, line => { line.Color = Color.Blue; line.Label = "Data"; })
    .Save("chart.svg");
```

## Saving to different formats

```csharp
// Auto-detect from extension
Plt.Create().Plot(x, y).Save("chart.svg");    // SVG (built-in)
Plt.Create().Plot(x, y).Save("chart.json");   // JSON
Plt.Create().Plot(x, y).Save("chart.png");    // PNG (needs Skia)
Plt.Create().Plot(x, y).Save("chart.pdf");    // PDF (needs Skia)

// Or get strings directly
string svg = Plt.Create().Plot(x, y).ToSvg();
string json = Plt.Create().Plot(x, y).ToJson();
```

## Setting up PNG/PDF export

```csharp
using MatPlotLibNet.Skia;

// Register once at startup
FigureExtensions.RegisterTransform(".png", new PngTransform());
FigureExtensions.RegisterTransform(".pdf", new PdfTransform());

// Then use ToFile everywhere
Plt.Create().Plot(x, y).Save("chart.png");
```

## Chart types

### Line, scatter, bar

```csharp
Plt.Create().Plot(x, y).Save("line.svg");
Plt.Create().Scatter(x, y).Save("scatter.svg");
Plt.Create().Bar(["Q1", "Q2", "Q3"], [100, 200, 150]).Save("bar.svg");
```

### Subplots

```csharp
Plt.Create()
    .WithSize(1200, 600)
    .AddSubPlot(1, 2, 1, ax => ax.WithTitle("Line").Plot(x, y))
    .AddSubPlot(1, 2, 2, ax => ax.WithTitle("Bar").Bar(["A", "B"], [10, 20]))
    .Save("subplots.svg");
```

### Legend

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, temp, s => s.Label = "Temperature")
        .Plot(x, humidity, s => s.Label = "Humidity")
        .WithLegend(LegendPosition.UpperRight))
    .Save("legend.svg");
```

### Hierarchical charts

```csharp
var tree = new TreeNode
{
    Label = "Revenue",
    Children = [
        new TreeNode { Label = "Products", Value = 400 },
        new TreeNode { Label = "Services", Value = 300 },
        new TreeNode { Label = "Licensing", Value = 200 }
    ]
};

Plt.Create().Treemap(tree).Save("treemap.svg");
Plt.Create().Sunburst(tree).Save("sunburst.svg");
```

### Sankey diagrams

```csharp
SankeyNode[] nodes = [new("Coal"), new("Gas"), new("Electricity"), new("Heat")];
SankeyLink[] links = [new(0, 2, 50), new(1, 2, 30), new(1, 3, 20)];

Plt.Create().Sankey(nodes, links).Save("sankey.svg");
```

### Polar plots

```csharp
double[] r = [1, 2, 3, 4, 5];
double[] theta = [0, 0.5, 1.0, 1.5, 2.0];

Plt.Create().PolarPlot(r, theta).Save("polar_line.svg");
Plt.Create().PolarScatter(r, theta).Save("polar_scatter.svg");

// Wind rose
double[] speeds = [5, 10, 8, 3, 7, 12, 6, 9];
double[] dirs = Enumerable.Range(0, 8).Select(i => i * Math.PI / 4).ToArray();
Plt.Create().PolarBar(speeds, dirs, b => b.BarWidth = 0.7).Save("windrose.svg");
```

### Financial charts

```csharp
double[] open = [10, 12, 11], high = [15, 14, 13], low = [8, 10, 9], close = [13, 11, 12];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Candlestick(open, high, low, close)
        .Sma(2)
        .WithLegend())
    .Save("candlestick.svg");
```

## Styling

### Themes

```csharp
Plt.Create()
    .WithTheme(Theme.Dark)
    .Plot(x, y)
    .Save("dark.svg");

// Custom theme
var theme = Theme.CreateFrom(Theme.Dark)
    .WithBackground(Color.FromHex("#1a1a2e"))
    .WithFont(f => f with { Family = "Consolas", Size = 14 })
    .Build();

Plt.Create().WithTheme(theme).Plot(x, y).Save("custom.svg");
```

### Axis formatting

```csharp
// Date axis
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .SetXDateFormat("MMM yyyy")
        .Plot(dates.Select(d => d.ToOADate()).ToArray(), values))
    .Save("dates.svg");

// Log scale
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .SetYScale(AxisScale.Log)
        .SetYTickFormatter(new LogTickFormatter())
        .Plot(x, y))
    .Save("logscale.svg");
```

## Color maps

52 built-in colormaps — see [Src/MatPlotLibNet/howTo.md §13](Src/MatPlotLibNet/howTo.md#13-color-maps) for the full taxonomy table and normalizer reference.

```csharp
using MatPlotLibNet.Styling.ColorMaps;

// Fluent API (heatmap with colormap + colorbar)
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Heatmap(data)
        .WithColorMap("plasma")
        .WithColorBar(cb => cb with { Label = "Intensity" }))
    .Save("heatmap.svg");

// Registry lookup — case-insensitive, append _r for reversed variant
var map = ColorMapRegistry.Get("rdylgn");
var reversed = ColorMapRegistry.Get("viridis_r");
```

Normalizers: `new LogNormalizer()`, `new TwoSlopeNormalizer(center)`, `new BoundaryNormalizer(double[])`.

## Advanced layouts

### GridSpec — unequal subplot sizes

```csharp
Plt.Create()
    .WithGridSpec(2, 2, heightRatios: [2.0, 1.0], widthRatios: [3.0, 1.0])
    .AddSubPlot(GridPosition.Single(0, 0), ax => ax.Plot(x, y).WithTitle("Main"))
    .AddSubPlot(GridPosition.Single(0, 1), ax => ax.Scatter(x, y))
    .AddSubPlot(GridPosition.Span(1, 2, 0, 2), ax => ax.Bar(cats, vals))
    .Save("gridspec.svg");
```

### Shared axes

```csharp
.AddSubPlot(2, 1, 1, ax => ax.ShareX("group1").Plot(x, y1))
.AddSubPlot(2, 1, 2, ax => ax.ShareX("group1").Plot(x, y2))
```

### Spines

```csharp
ax.HideTopSpine().HideRightSpine();

// Move x-axis to y=0
ax.WithSpines(s => s with { Bottom = s.Bottom with { Position = SpinePosition.Data(0) } });
```

### Inset axes

```csharp
ax.Plot(x, y)
  .AddInset(0.6, 0.6, 0.35, 0.35, inset => inset
      .Plot(xZoom, yZoom).WithTitle("Detail"));
```

## Subplot spacing

```csharp
// Tight layout (automatic minimal margins)
Plt.Create()
    .TightLayout()
    .AddSubPlot(2, 2, 1, ax => ax.Plot(x, y))
    .AddSubPlot(2, 2, 2, ax => ax.Scatter(x, y))
    .Save("tight.svg");

// Custom margins
Plt.Create()
    .WithSubPlotSpacing(s => s with { MarginLeft = 80, HorizontalGap = 20 })
    .AddSubPlot(1, 2, 1, ax => ax.Plot(x, y))
    .AddSubPlot(1, 2, 2, ax => ax.Bar(["A"], [10]))
    .Save("custom_spacing.svg");
```

## Real-time charts

### ASP.NET Core + SignalR

```csharp
// Server
builder.Services.AddMatPlotLibNetSignalR();
await publisher.PublishSvgAsync("sensor-1", figure);

// Blazor client
<MplLiveChart ChartId="sensor-1" HubUrl="/charts-hub" />

// React client
<MplLiveChart chartId="sensor-1" hubUrl="/charts-hub" />
```

### Interactive popup

```csharp
using MatPlotLibNet.Interactive;

var handle = await Plt.Create().Plot(x, y).Build().ShowAsync();
// Updates push via SignalR
await handle.UpdateAsync();
```

## Animation

```csharp
using MatPlotLibNet.Animation;

// Define frames
var animation = new AnimationBuilder(60, frame =>
    Plt.Create()
        .WithTitle($"t = {frame * 0.1:F1}")
        .Plot(x, x.Select(v => Math.Sin(v + frame * 0.1)).ToArray())
        .Build())
{
    Interval = TimeSpan.FromMilliseconds(50),
    Loop = true
};

// Play in browser
var handle = await Plt.Create().Plot(x, y).Build().ShowAsync();
await handle.AnimateAsync(animation);

// Stop with cancellation
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await handle.AnimateAsync(animation, cts.Token);
```

## When you need the Figure object

If you need to pass the figure to APIs (Blazor components, publishers, servers), use `.Build()`:

```csharp
var figure = Plt.Create().Plot(x, y).Build();

// Pass to Blazor component
<MplChart Figure="figure" />

// Pass to publisher
await publisher.PublishSvgAsync("chart-1", figure);

// Pass to interactive popup
await figure.ShowAsync();
```

## PropCycler

Cycle Color, LineStyle, MarkerStyle, and LineWidth simultaneously across series.

```csharp
using MatPlotLibNet.Styling;

// Build a cycler
var cycler = new PropCyclerBuilder()
    .WithColors(Color.Blue, Color.Orange, Color.Green, Color.Red)
    .WithLineStyles(LineStyle.Solid, LineStyle.Dashed)
    .Build();

// Apply to a figure
Plt.Create()
    .WithPropCycler(cycler)
    .AddSubPlot(1, 1, 1, ax =>
    {
        for (int i = 0; i < 4; i++)
            ax.Plot(x, y[i], s => s.Label = $"Series {i + 1}");
        ax.WithLegend();
    })
    .Save("cycler.svg");

// Embed in a theme for reuse
var theme = Theme.CreateFrom(Theme.Dark)
    .WithPropCycler(cycler)
    .Build();
```

Properties cycle at their own lengths; the series index wraps modulo the LCM. If `PropCycler` is null, the original `Theme.CycleColors[]` path is used (backward compatible).

## Date axes

Plot time-series data with automatic date tick placement.

```csharp
DateTime[] dates = Enumerable.Range(0, 365)
    .Select(i => new DateTime(2025, 1, 1).AddDays(i))
    .ToArray();
double[] values = dates.Select((d, i) => Math.Sin(i / 30.0)).ToArray();

// DateTime[] overload auto-sets X to AxisScale.Date
Plt.Create()
    .Plot(dates, values)
    .WithXLabel("Date")
    .WithYLabel("Value")
    .Save("dates.svg");   // ticks: "Jan 2025", "Feb 2025", …
```

For sub-day data:

```csharp
DateTime[] hours = Enumerable.Range(0, 48)
    .Select(i => DateTime.Today.AddHours(i))
    .ToArray();

Plt.Create()
    .Plot(hours, measurements)
    .Save("hourly.svg");   // ticks: "00:00", "06:00", "12:00", …
```

`AutoDateLocator` automatically selects the tick interval from years down to seconds. To override:

```csharp
ax.SetXTickLocator(new AutoDateLocator())
  .SetXTickFormatter(new AutoDateFormatter());
```

## Math text labels

Use mini-LaTeX syntax in any title, axis label, annotation, or legend entry. Wrap math in `$...$`.

```csharp
// Greek letters + super/subscript in chart title
Plt.Create()
    .WithTitle("$\\alpha$ vs $\\beta$ correlation")
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.WithTitle("R$^{2}$ = 0.97");
        ax.SetYLabel("$\\sigma$ (Pa)");
        ax.SetXLabel("Time $\\Delta t$ (ms)");
        ax.Plot(x, y);
    })
    .Save("math.svg");
```

**Quick reference:**

| Syntax | Renders as |
|--------|-----------|
| `$\\alpha$` | α |
| `$\\sigma^{2}$` | σ² |
| `$x_{i}$` | x subscript i |
| `$\\pm$` | ± |
| `$\\infty$` | ∞ |
| `$\\leq$` | ≤ |
| `$\\degree$C` | °C |

Text outside `$...$` is rendered as-is. SVG backends emit `<tspan baseline-shift="super/sub">`. Non-SVG backends (Skia, MAUI) get a plain-text fallback with Unicode substitution.

## Constrained layout / tight layout

Automatically compute margins from actual text extents instead of hardcoded defaults.

```csharp
// TightLayout or ConstrainedLayout — same effect
Plt.Create()
    .TightLayout()
    .AddSubPlot(2, 2, 1, ax =>
    {
        ax.SetYLabel("Population (millions)");
        ax.Plot(x, y1);
    })
    .AddSubPlot(2, 2, 2, ax =>
    {
        ax.SetYLabel("GDP ($\\times 10^{9}$)");
        ax.Plot(x, y2);
    })
    .Save("constrained.svg");
```

The engine measures Y-tick label widths, axis label sizes, and subplot title heights; computes exact `SubPlotSpacing` margins; and takes the maximum across all subplots. Margins are clamped to sensible ranges.

```csharp
// Equivalent using ConstrainedLayout()
Plt.Create()
    .ConstrainedLayout()
    .AddSubPlot(1, 1, 1, ax => ax.Plot(x, y))
    .Save("cl.svg");
```

When neither flag is set, fixed default margins (`MarginLeft=60`, `MarginBottom=50`, etc.) are used as before.

## GIF animation export

Export an `AnimationBuilder` directly to an animated GIF (requires `MatPlotLibNet.Skia`).

```csharp
using MatPlotLibNet.Animation;
using MatPlotLibNet.Skia;

var animation = new AnimationBuilder(60, frame =>
    Plt.Create()
        .WithTitle($"t = {frame * 0.1:F1} s")
        .Plot(x, x.Select(v => Math.Sin(v + frame * 0.2)).ToArray())
        .Build())
{
    Interval = TimeSpan.FromMilliseconds(80),
    Loop = true
};

// Save to file
animation.SaveGif("wave.gif");

// Or get bytes (e.g., for a Blazor download)
byte[] gif = animation.ToGif();
```

The encoder writes GIF89a with a NETSCAPE2.0 loop extension, 252-color uniform quantization (6×7×6 RGB cube), and LZW-compressed per-frame image data. No FFmpeg dependency.
