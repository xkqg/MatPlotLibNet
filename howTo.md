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
FigureBuilder.RegisterGlobalTransform(".png", new PngTransform());
FigureBuilder.RegisterGlobalTransform(".pdf", new PdfTransform());

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
