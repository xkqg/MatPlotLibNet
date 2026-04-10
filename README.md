# MatPlotLibNet

A .NET 10 / .NET 8 charting library inspired by [matplotlib](https://matplotlib.org/). Fluent API, dependency injection, parallel SVG rendering, polymorphic export (SVG/PNG/PDF), and multi-platform output to Blazor, MAUI, ASP.NET Core, Angular, React, Vue, and standalone browser popups.

[![License: LGPL v3](https://img.shields.io/badge/License-LGPLv3-blue.svg)](LICENSE)

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
| **MatPlotLibNet.Notebooks** | `#r "nuget: MatPlotLibNet.Notebooks"` | Inline SVG rendering in Polyglot / Jupyter notebooks |
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

**60 series types** with fluent builder API:

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
Heatmap, Image (imshow), Histogram2D, Box, Violin, Contour, Contourf, Stem, Candlestick, OhlcBar, Quiver, Radar, Donut, Bubble, Waterfall, Funnel, Gantt, Gauge, ProgressBar, Sparkline, Ecdf, StackedArea, Streamplot, Treemap, Sunburst, Sankey, PolarLine, PolarScatter, PolarBar, Surface, Wireframe, Scatter3D, Kde, Regression, Hexbin, Rugplot, Stripplot, Eventplot, BrokenBarH, Countplot, Pcolormesh, Residplot, Pointplot, Swarmplot, Spectrogram, Table, Tricontour, Tripcolor, QuiverKey, Barbs, Stem3D, Bar3D.

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

## Filled contours (contourf)

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Contourf(xGrid, yGrid, zGrid, c =>
        {
            c.Levels = 12;
            c.Alpha = 0.9;
            c.ShowLines = true;      // overlay iso-lines
        })
        .WithColorMap("RdBu")
        .WithColorBar())
    .Save("contourf");
```

Renders colored bands between consecutive iso-levels using a painter's algorithm. Supports all colormaps, normalizers, and color bars.

## Image interpolation

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Image(data, img =>
        {
            img.Interpolation = "bilinear";   // "nearest", "bilinear", "bicubic"
            img.Alpha = 0.8;
            img.BlendMode = BlendMode.Normal;
        })
        .WithColorMap("plasma"))
    .Save("smooth_image");
```

Interpolation engines: **nearest** (default, one color per cell), **bilinear** (smooth 2x2), **bicubic** (Catmull-Rom 4x4). Blend modes: Normal, Multiply, Screen, Overlay.

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

## Statistical series

```csharp
// KDE — kernel density estimation with auto Silverman bandwidth
Plt.Create().Kde(samples, k => { k.Fill = true; k.Alpha = 0.3; }).Save("kde");

// Regression line with 95% confidence band
ax.Regression(xData, yData, r =>
{
    r.Degree = 2;
    r.ShowConfidence = true;
    r.ConfidenceLevel = 0.95;
});

// Hexbin — 2D density via hexagonal binning
ax.Hexbin(x, y, h =>
{
    h.GridSize = 25;
    h.MinCount = 2;
}).WithColorMap("YlOrRd").WithColorBar();
```

## Statistical distribution plots (v0.8.0)

```csharp
// Rug plot — tick marks showing individual data values
ax.Rugplot(samples, r => { r.Height = 0.05; r.Alpha = 0.5; });

// Strip plot — jittered dots per category
ax.Stripplot(datasets, s => s.Jitter = 0.2);

// Swarm plot — beeswarm algorithm (non-overlapping)
ax.Swarmplot(datasets, s => s.MarkerSize = 5);

// Point plot — mean + confidence interval per category
ax.Pointplot(datasets, p => { p.CapSize = 0.2; p.ConfidenceLevel = 0.95; });

// Count plot — bar chart from raw category labels
ax.Countplot(new[] { "A", "B", "A", "C", "B", "A" });

// Residual plot — scatter of model residuals
ax.Residplot(xData, yData, r => { r.Degree = 2; r.ShowZeroLine = true; });
```

## Triangular mesh plots (v0.8.0)

```csharp
// Tricontour — iso-lines on unstructured points
ax.Tricontour(x, y, z, tc => { tc.Levels = 10; tc.ColorMap = ColorMaps.Viridis; });

// Tripcolor — pseudocolor fill on triangular mesh
ax.Tripcolor(x, y, z, tc => tc.ColorMap = ColorMaps.Plasma);

// Wind barbs — meteorological speed/direction field
ax.Barbs(x, y, speed, direction, b => b.BarbLength = 15);

// Quiver key — reference arrow for quiver field
ax.QuiverKey(0.85, 0.95, 10.0, "10 m/s");
```

## 3D stem and bar (v0.8.0)

```csharp
// 3D stem — vertical lines from XY-plane to data points
Plt.Create().Stem3D(x, y, z).Save("stem3d");

// 3D bar chart — rectangular prisms rising from XY-plane
Plt.Create().Bar3D(x, y, heights, b => b.BarWidth = 0.4).Save("bar3d");
```

## Spectrogram (v0.8.0)

```csharp
// Spectrogram — short-time Fourier transform heatmap
ax.Spectrogram(signal, sampleRate: 44100, s =>
{
    s.WindowSize = 256;
    s.Overlap = 128;
    s.ColorMap = ColorMaps.Inferno;
});
```

## Table (v0.8.0)

```csharp
// Table — render tabular data inside axes
ax.Table(
    new[] { new[] { "Row 1", "100", "A" }, new[] { "Row 2", "200", "B" } },
    t =>
    {
        t.ColumnHeaders = new[] { "Name", "Value", "Grade" };
        t.FontSize = 11;
    });
```

## Pair plot, facet grid, clustermap (v0.8.0)

```csharp
// Pair plot — N×N scatter matrix with diagonal histograms
FigureTemplates.PairPlot(columns, columnNames: new[] { "X1", "X2", "X3" })
    .Save("pairplot");

// Facet grid — one subplot per category
FigureTemplates.FacetGrid(x, y, category,
    (ax, fx, fy) => ax.Scatter(fx, fy),
    cols: 3)
    .Save("facet");

// Clustermap — hierarchically clustered heatmap with dendrograms
FigureTemplates.Clustermap(data, rowLabels, colLabels).Save("clustermap");
```

## Joint plot (scatter + marginals)

```csharp
FigureTemplates.JointPlot(x, y, title: "Correlation", bins: 30)
    .Save("joint.svg");
```

Produces a 2×2 GridSpec with the scatter in the center, X histogram on top, and Y histogram on the right.

## SVG interactivity

```csharp
// Native browser tooltips on hover
.AddSubPlot(1, 1, 1, ax => ax.WithTooltips().Scatter(x, y))

// Zoom (mouse wheel) and pan (click-drag)
Plt.Create().WithZoomPan().Plot(x, y).Save("zoomable")

// Click legend entries to show/hide series
Plt.Create().WithLegendToggle()
    .Plot(x, y1, s => s.Label = "Train")
    .Plot(x, y2, s => s.Label = "Test")
    .Save("toggle.svg");

// Hover to highlight a series and dim the rest
Plt.Create().WithHighlight()
    .Plot(x, y1, s => s.Label = "A")
    .Plot(x, y2, s => s.Label = "B")
    .Save("highlight.svg");

// Styled HTML tooltips (replaces native browser tooltip)
Plt.Create().WithRichTooltips()
    .AddSubPlot(1, 1, 1, ax => ax.WithTooltips().Scatter(x, y))
    .Save("tooltips.svg");

// Shift+drag to select a data region — fires mpl:selection CustomEvent
Plt.Create().WithSelection().Plot(x, y).Save("select.svg");

// Combine features freely
Plt.Create()
    .WithLegendToggle()
    .WithHighlight()
    .WithRichTooltips()
    .Plot(x, y, s => s.Label = "Series")
    .Save("interactive.svg");
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

## Style sheets / rcParams

Global configuration with scoped overrides — like matplotlib's `rcParams` + `plt.style.use()`:

```csharp
using MatPlotLibNet.Styling;

// Apply a named style globally
Plt.Style.Use("seaborn");

// Scoped override (auto-reverts on Dispose)
using (Plt.Style.Context("dark"))
{
    Plt.Create().Plot(x, y).Save("dark_chart");
}
// Back to previous style here

// Override individual parameters
using (Plt.Style.Context(new Dictionary<string, object>
{
    ["font.size"] = 16.0,
    ["lines.linewidth"] = 2.5,
    ["axes.grid"] = true
}))
{
    Plt.Create().Plot(x, y).Save("custom_params");
}
```

Precedence: explicit property > Theme > `RcParams.Current` > defaults.

Built-in style sheets: `default`, `dark`, `seaborn`, `ggplot`, `bmh`, `fivethirtyeight`.

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

## PropCycler

Cycle Color, LineStyle, MarkerStyle, and LineWidth simultaneously — like matplotlib's `prop_cycle`:

```csharp
using MatPlotLibNet.Styling;

var cycler = new PropCyclerBuilder()
    .WithColors(Color.Blue, Color.Orange, Color.Green)
    .WithLineStyles(LineStyle.Solid, LineStyle.Dashed)
    .WithMarkerStyles(MarkerStyle.Circle, MarkerStyle.Square)
    .Build();

// Apply to a single figure
Plt.Create()
    .WithPropCycler(cycler)
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.Plot(x, y1, s => s.Label = "A");
        ax.Plot(x, y2, s => s.Label = "B");
        ax.Plot(x, y3, s => s.Label = "C");
    })
    .Save("cycler.svg");

// Or embed in a custom theme
var theme = Theme.CreateFrom(Theme.Default)
    .WithPropCycler(cycler)
    .Build();
```

Properties cycle independently using their individual lengths (LCM wrap-around). When `PropCycler` is not set, the existing `Theme.CycleColors[]` path is unchanged.

## Date axes

```csharp
DateTime[] dates = Enumerable.Range(0, 90)
    .Select(i => DateTime.Today.AddDays(i))
    .ToArray();
double[] values = dates.Select((_, i) => Math.Sin(i * 0.1)).ToArray();

Plt.Create()
    .Plot(dates, values)              // auto-sets X to AxisScale.Date
    .WithXLabel("Date")
    .Save("date_axis.svg");           // ticks show "Apr 15", "May 01", etc.
```

`AutoDateLocator` chooses the tick interval (Years/Months/Weeks/Days/Hours/Minutes/Seconds) from the data range. `AutoDateFormatter` picks the matching format string. Both are applied automatically when `AxisScale.Date` is set and no explicit locator is configured.

## Math text labels

Use mini-LaTeX syntax in any title, axis label, or annotation:

```csharp
Plt.Create()
    .WithTitle("$\\alpha$ vs $\\beta$ correlation")
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.WithTitle("R$^{2}$ = 0.97");
        ax.SetYLabel("$\\sigma$ (Pa)");
        ax.SetXLabel("$\\Delta t$ (ms)");
        ax.Plot(x, y);
    })
    .Save("math.svg");
```

**Supported syntax:**

| Syntax | Example | Output |
|--------|---------|--------|
| Math delimiters | `$...$` | switch to math mode |
| Greek lowercase | `\alpha` … `\omega` | α … ω |
| Greek uppercase | `\Alpha` … `\Omega` | Α … Ω |
| Superscript | `^{text}` or `^x` | raised + 70% size |
| Subscript | `_{text}` or `_x` | lowered + 70% size |
| Math symbols | `\pm \times \leq \geq \neq \infty \approx \cdot \degree` | ± × ≤ ≥ ≠ ∞ ≈ ⋅ ° |

Mixed text is supported: `Temperature ($\\degree$C)` renders as plain text outside `$...$`.

## Constrained layout

Automatic margin computation from actual text extents — no more clipped axis labels:

```csharp
Plt.Create()
    .TightLayout()                   // or .ConstrainedLayout()
    .AddSubPlot(2, 2, 1, ax => { ax.SetYLabel("$\\sigma$ (very long label)"); ax.Plot(x, y); })
    .AddSubPlot(2, 2, 2, ax => { ax.SetYLabel("Value"); ax.Plot(x, y); })
    .Save("tight.svg");
```

`ConstrainedLayoutEngine` measures Y-tick label widths, axis label sizes, and title heights; computes exact margins per subplot; and takes the maximum across all subplots. Margins are clamped to sensible ranges (left ∈ [30,120], bottom ∈ [30,100]).

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

### GIF export

Save an animated GIF directly from an `AnimationBuilder` — no FFmpeg required:

```csharp
using MatPlotLibNet.Skia;  // MatPlotLibNet.Skia package required

var animation = new AnimationBuilder(30, frame =>
    Plt.Create()
        .WithTitle($"t = {frame * 0.1:F1}")
        .Plot(x, x.Select(v => Math.Sin(v + frame * 0.2)).ToArray())
        .Build())
{
    Interval = TimeSpan.FromMilliseconds(100),
    Loop = true
};

animation.SaveGif("output.gif");        // saves to file
byte[] bytes = animation.ToGif();       // returns bytes
```

GIF89a format with NETSCAPE2.0 loop extension, 252-color uniform quantization, LZW-compressed frames.

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
| **0.8.1** | **Tier 3 Infrastructure.** `PropCycler` cycles Color + LineStyle + MarkerStyle + LineWidth simultaneously across series (LCM wrap-around). Date Axis: `AutoDateLocator` + `AutoDateFormatter` auto-select tick intervals from years down to seconds; `DateTime[]` overloads on builder API. Constrained Layout: `TightLayout()` and `ConstrainedLayout()` now invoke a real engine that measures text extents via `CharacterWidthTable` and computes exact margins. Math Text: mini-LaTeX in any label — `$\alpha^{2}$` renders as SVG `<tspan>` with Greek Unicode + super/subscript; 48 Greek + 40 math symbols. GIF Animation: `AnimationBuilder.SaveGif()` / `ToGif()` via custom GIF89a encoder + 252-color quantizer. SkiaSharp `SKFont` API migration. 2430 tests. |
| **0.7.0** | **Style sheets + Stats series + Interactive SVG.** `RcParams` global config registry + `Plt.Style.Use()`/`Context()`. `ContourfSeries` filled contours. `IInterpolationEngine` (nearest/bilinear/bicubic) + `BlendMode`. `KdeSeries` (Silverman KDE + fill). `RegressionSeries` (polynomial fit + confidence bands, `LeastSquares`). `HexbinSeries` (flat-top hex bins, `HexGrid`). `FigureTemplates.JointPlot()`. Interactive SVG: `WithLegendToggle()`, `WithHighlight()`, `WithRichTooltips()`, `WithSelection()` — 4 embedded JS modules keyed on DOM `data-series-index`/`data-legend-index`. Notebooks package `BuildOutputTargetFolder` fix. 43 series types. 1924 tests. |
| **0.6.0** | **SIMD Vectorization + Phase F.** `VectorMath` kernel backed by `TensorPrimitives` (SIMD-accelerated): `RollingMean`, `RollingMin/Max` (O(n) monotone deque), `RollingStdDev`, `MultiplyAdd`, `CumulativeSum`. `Vec` public `readonly record struct` with SIMD operators, reductions, and LINQ-style lambdas. `TransformBatch` rewritten as single-pass AVX SIMD interleave (3.6x faster, zero intermediate allocations). 4 new indicators: `WilliamsR`, `OBV`, `CCI`, `ParabolicSar`. `FigureTemplates`: `FinancialDashboard`, `ScientificPaper`, `SparklineDashboard`. `MarchingSquares` contour label rendering. `MatPlotLibNet.Notebooks` package (Polyglot Notebooks / Jupyter inline SVG). All 15 existing indicators refactored onto VectorMath. 1668 tests. |
| **0.5.1** | **Phase C — Annotations**: `Annotation.Alignment`, `Rotation`, `ArrowStyle` (None/Simple/FancyArrow with triangular arrowhead), `BackgroundColor`. Bar labels (`ShowLabels`/`LabelFormat` on `BarSeries`). `ContourSeries.ShowLabels` reserved. `IRenderContext.DrawText(…, rotation)` overload (SVG `transform="rotate(…)"`). **Phase D — Ticks**: `ITickLocator` interface + `AutoLocator`, `MaxNLocator`, `MultipleLocator`, `FixedLocator`, `LogLocator`. `EngFormatter` (SI prefixes) + `PercentFormatter`. `Axis.TickLocator`, minor tick rendering (5 per major interval), `TickConfig.Spacing` now wires `MultipleLocator`. `AxesBuilder.SetXTickLocator/SetYTickLocator`, `.WithMinorTicks()`. Bug fix: secondary Y-axis and polar chart formatters now use the custom `TickFormatter` instead of `FormatTick`. **Phase E — Performance**: `IDownsampler` + `LttbDownsampler` (Largest-Triangle-Three-Buckets O(n)) + `ViewportCuller` (static). `XYSeries.MaxDisplayPoints` opt-in downsampling for Line/Area/Scatter/Step. `DataTransform.DataXMin/XMax/YMin/YMax`. `AxesBuilder.WithDownsampling(maxPoints)`. 1588 tests (86 new). |
| **0.5.0** | 39 series types. Layout: `GridSpec` unequal subplots, `SpinesConfig`, shared axes (`ShareX`/`ShareY`), inset axes. 5 new series: `ImageSeries` (imshow), `Histogram2DSeries`, `StreamplotSeries`, `EcdfSeries`, `StackedAreaSeries`. OO interfaces: `IColormappable`, `INormalizable`, `ICategoryLabeled`, `IColorBarDataProvider`, `IStackable`. 20 new colormaps (52 base, 104 with `_r` variants): Turbo, Jet, Hsv, Hot, Copper, Bone, BuPu, GnBu, PuRd, RdPu, YlGnBu, PuBuGn, Cubehelix, PuOr, Seismic, Bwr, Pastel2, Dark2, Accent, Paired. `ColorMapRegistry` (case-insensitive, thread-safe). `INormalizer`: Linear, Log, TwoSlope, Boundary. `AxesBuilder.WithColorMap/WithNormalizer` collapse to single-line interface check (bug fix: previously missed 3 of 7 colormappable series). 1502 tests. |
| **0.4.0** | 34 series types (11 families), 798 tests. OO architecture: `AxesRenderer` polymorphism (`CartesianAxesRenderer`, `PolarAxesRenderer`, `ThreeDAxesRenderer`). `ISeriesSerializable` on all 34 series, `SeriesRegistry` for deserialization. Generic bases: `XYSeries`, `PolarSeries`, `GridSeries3D`, `HierarchicalSeries`. Interfaces: `IHasDataRange`, `IPolarSeries`, `I3DGridSeries`, `I3DPointSeries`, `IPriceSeries`. Thread-safe: volatile fields, `ConcurrentDictionary` for `GlobalTransforms`, `AxesRenderer` registry, `SeriesRegistry`. Animation: `IAnimation<TState>`, `AnimationController<TState>`, `LegacyAnimationAdapter`. `Save("chart")` API with format auto-detect. `FigureBuilder` SRP: Save/Transform moved to `FigureExtensions`. Color constants (`Tab10Blue`, `GridGray`, etc.). `ITickFormatter` pipeline. ColorBar, Legend, SubPlotSpacing. Polar + 3D coordinate systems. |
| **0.3.2** | Quality release: OO indicator refactor (`Indicator<TResult>` with `IIndicatorResult` constraint, named result records, no statics). 92 new tests. BenchmarkDotNet suite. CHANGELOG, BENCHMARKS.md, DocFX, 4 sample projects. JSON serialization fix for 9 series types. |
| **0.3.1** | Platform expansion: `@matplotlibnet/react` (React 19 hooks + components), `@matplotlibnet/vue` (Vue 3 composables + components), `MatPlotLibNet.GraphQL` (HotChocolate queries + subscriptions). Core library multi-targets `netstandard2.1`. |
| **0.3.0** | 25 series types (Donut, Bubble, OhlcBar, Waterfall, Funnel, Gantt, Gauge, ProgressBar, Sparkline). 13 technical indicators (SMA, EMA, BB, VWAP, RSI, MACD, Stochastic, Volume, Fibonacci, ATR, ADX, Keltner, Ichimoku). Trading analytics (EquityCurve, ProfitLoss, DrawDown). Buy/sell signal markers. Generic `SeriesRenderer<T>` + `Indicator<TResult>`. Intuitive fluent API (`.Sma(20)`, `.BuyAt()`, `.SaveSvg()`). PriceSource enum, Offset, LineStyle on all indicators. Series organized by chart family. |
| **0.2.0** | 16 series types (Area, Step, ErrorBar, Candlestick, Quiver, Radar). Stacked bars. Annotations (text, HLine/VLine, HSpan/VSpan). Secondary Y-axis (TwinX). SVG tooltips + zoom/pan. Polymorphic transforms (`IFigureTransform`, `FigureTransform`, `TransformResult`). PNG/PDF export via MatPlotLibNet.Skia. |
| **0.1.0** | Initial release. 10 series types (Line, Scatter, Bar, Histogram, Pie, Heatmap, Box, Violin, Contour, Stem). Fluent builder API. Parallel SVG rendering. JSON serialization. 6 themes. Blazor, ASP.NET Core, MAUI, Angular, Interactive packages. |

See [CHANGELOG.md](CHANGELOG.md) for detailed release notes. See [BENCHMARKS.md](BENCHMARKS.md) for performance numbers.

## License

[LGPL-3.0](LICENSE) -- Copyright (c) 2026 H.P. Gansevoort
