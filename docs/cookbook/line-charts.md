# Line Charts

> **Tips:** Try `.WithBrowserInteraction()` to make your line chart zoomable in the browser. Use `.WithSymlogYScale()` for data spanning large ranges. Switch themes with `.WithTheme(Theme.Nord)`.

## Simple line chart

```csharp
double[] x = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
double[] y = [2.1, 4.5, 3.2, 6.8, 5.1, 7.3, 6.5, 8.9, 7.2, 9.4];

Plt.Create()
    .WithTitle("Sales Trend")
    .WithTheme(Theme.Seaborn)
    .WithSize(800, 500)
    .Plot(x, y, line => { line.Color = Colors.Blue; line.Label = "Revenue"; })
    .Save("chart.svg");
```

![Line chart](../images/chart.png)

## Line styles, markers, and width

Customize every aspect of the line via the configure lambda:

```csharp
Plt.Create()
    .WithTitle("Line Styles")
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.Plot(x, y1, s =>
        {
            s.Color = Colors.Blue;
            s.LineStyle = LineStyle.Solid;
            s.LineWidth = 2.0;
            s.Label = "Solid (2px)";
        });
        ax.Plot(x, y2, s =>
        {
            s.Color = Colors.Red;
            s.LineStyle = LineStyle.Dashed;
            s.LineWidth = 1.5;
            s.Label = "Dashed";
        });
        ax.Plot(x, y3, s =>
        {
            s.Color = Colors.Green;
            s.LineStyle = LineStyle.DashDot;
            s.LineWidth = 1.0;
            s.Marker = MarkerStyle.Circle;
            s.MarkerSize = 6;
            s.MarkEvery = 2;  // marker every 2nd point
            s.Label = "DashDot + markers";
        });
        ax.WithLegend(LegendPosition.UpperLeft);
    })
    .Save("line_styles.svg");
```

## Smooth interpolation

Fritsch-Carlson cubic spline for smooth curves:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s =>
        {
            s.Smooth = true;
            s.SmoothResolution = 20;  // 20 sub-points per interval
            s.Color = Colors.Purple;
            s.Label = "Smoothed";
        })
        .Scatter(x, y, s =>
        {
            s.Color = Colors.Red;
            s.MarkerSize = 8;
            s.Label = "Data points";
        })
        .WithLegend())
    .Save("smooth_line.svg");
```

## Step functions

Control step placement with `DrawStyle`:

```csharp
Plt.Create()
    .WithSize(900, 400)
    .AddSubPlot(1, 3, 1, ax => ax
        .Plot(x, y, s => { s.DrawStyle = DrawStyle.Steps; s.Label = "Steps"; })
        .WithTitle("Steps").WithLegend())
    .AddSubPlot(1, 3, 2, ax => ax
        .Plot(x, y, s => { s.DrawStyle = DrawStyle.StepPre; s.Label = "StepPre"; })
        .WithTitle("StepPre").WithLegend())
    .AddSubPlot(1, 3, 3, ax => ax
        .Plot(x, y, s => { s.DrawStyle = DrawStyle.StepMid; s.Label = "StepMid"; })
        .WithTitle("StepMid").WithLegend())
    .TightLayout()
    .Save("step_styles.svg");
```

## Marker customization

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s =>
        {
            s.Marker = MarkerStyle.Diamond;
            s.MarkerSize = 10;
            s.MarkerFaceColor = Colors.Gold;
            s.MarkerEdgeColor = Colors.DarkRed;
            s.MarkerEdgeWidth = 1.5;
            s.Color = Colors.DarkRed;
            s.Label = "Diamond markers";
        })
        .WithLegend())
    .Save("marker_custom.svg");
```

### All marker shapes (v1.7.2 Phase M)

`MarkerStyle` exposes 13 shapes, every one drawn by the shared `MarkerRenderer`:

| Shape | SVG primitive | Notes |
|---|---|---|
| `Circle` | `<circle>` | Default. Radius = `MarkerSize / 2`. |
| `Square` | `<rect>` | Edge = `MarkerSize`. |
| `Triangle` / `TriangleDown` / `TriangleLeft` / `TriangleRight` | `<polygon>` (3 vertices) | Equilateral; orientation as named. |
| `Diamond` | `<polygon>` (4 vertices) | Square rotated 45°. |
| `Pentagon` / `Hexagon` | `<polygon>` (5 / 6 vertices) | Regular N-gon, flat-top-up variant. |
| `Star` | `<polygon>` (10 vertices) | 5-point star, inner radius ≈ 0.38 × outer (matplotlib parity). |
| `Cross` / `Plus` | `<line>` × 2 | Outline-only (no fill). Stroke thickness falls back to `MarkerSize / 8` when `MarkerEdgeWidth` is zero. |
| `None` | — | No marker drawn. |

Prior to v1.7.2 Phase M, line charts drew every marker as a circle and scatter plots honoured only `Square`; all other shapes silently collapsed to circles. The shared `MarkerRenderer` dispatches all shapes uniformly across both series types.

## Grid and spine control

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => s.Label = "Data")
        // Grid styling
        .WithGrid(g => g with
        {
            Color = Colors.LightGray,
            LineStyle = LineStyle.Dotted,
            LineWidth = 0.5,
            Alpha = 0.8
        })
        // Hide top and right spines (matplotlib style)
        .HideTopSpine()
        .HideRightSpine()
        // Axis ranges
        .SetXLim(0, 12)
        .SetYLim(0, 12)
        .SetXLabel("Time (s)")
        .SetYLabel("Amplitude")
        .WithLegend())
    .Save("grid_spines.svg");
```

## Secondary Y-axis

Plot two datasets with different scales on the same axes:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, temperature, s => { s.Color = Colors.Red; s.Label = "Temperature (°C)"; })
        .SetYLabel("Temperature (°C)")
        .WithSecondaryYAxis(sec =>
        {
            sec.Plot(x, pressure, s => { s.Color = Colors.Blue; s.Label = "Pressure (hPa)"; });
            sec.SetYLabel("Pressure (hPa)");
        })
        .WithLegend())
    .Save("dual_axis.svg");
```

## PropCycler — automatic color + line style cycling

When plotting multiple series, `PropCycler` automatically assigns distinct colors and line styles:

```csharp
var cycler = new PropCyclerBuilder()
    .WithColors(Colors.Tab10Blue, Colors.Orange, Colors.Green, Colors.Red)
    .WithLineStyles(LineStyle.Solid, LineStyle.Dashed, LineStyle.Dotted, LineStyle.DashDot)
    .Build();

double[] x = Enumerable.Range(0, 60).Select(i => i * 0.2).ToArray();

Plt.Create()
    .WithTitle("PropCycler — four series, cycling color + line style")
    .WithTheme(Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build())
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.Plot(x, x.Select(v => Math.Sin(v)).ToArray(),       s => s.Label = "sin(x)");
        ax.Plot(x, x.Select(v => Math.Sin(v + 1.0)).ToArray(), s => s.Label = "sin(x+1)");
        ax.Plot(x, x.Select(v => Math.Sin(v + 2.0)).ToArray(), s => s.Label = "sin(x+2)");
        ax.Plot(x, x.Select(v => Math.Sin(v + 3.0)).ToArray(), s => s.Label = "sin(x+3)");
        ax.WithLegend(LegendPosition.UpperRight);
    })
    .TightLayout()
    .Save("prop_cycler.svg");
```

![PropCycler](../images/prop_cycler.png)

## LTTB downsampling for large datasets

Display 10,000 points as 500 using the Largest-Triangle-Three-Buckets algorithm:

```csharp
double[] x = Enumerable.Range(0, 10_000).Select(i => (double)i).ToArray();
double[] y = x.Select(v => Math.Sin(v * 0.05) * Math.Exp(-v * 0.0003)).ToArray();

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("10 000-point signal (LTTB → 500 display points)")
        .Plot(x, y, line => { line.Label = "Signal"; })
        .WithDownsampling(500))
    .Save("lttb.svg");
```

![LTTB downsampling](../images/lttb_downsampling.png)

## Outside legend

Place the legend outside the plot area — the constrained-layout engine reserves margin space automatically:

```csharp
Plt.Create()
    .WithSize(900, 500)
    .TightLayout()
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.Plot(x, x.Select(v => Math.Sin(v)).ToArray(), s => s.Label = "sin(x)");
        ax.Plot(x, x.Select(v => Math.Cos(v)).ToArray(), s => s.Label = "cos(x)");
        ax.WithLegend(l => l with { Position = LegendPosition.OutsideRight, Title = "Series" });
    })
    .Save("legend_outside.svg");
```

![Outside legend](../images/legend_outside.png)

## Legend customization

```csharp
ax.WithLegend(l => l with
{
    Position = LegendPosition.UpperRight,
    NCols = 2,               // two-column layout
    FontSize = 10,
    Title = "Metrics",
    TitleFontSize = 12,
    FrameOn = true,
    FancyBox = true,          // rounded corners
    Shadow = true,
    FaceColor = Colors.AliceBlue,
    EdgeColor = Colors.SteelBlue,
    FrameAlpha = 0.9,
    MarkerScale = 1.5,
    LabelSpacing = 0.4,
    ColumnSpacing = 1.5,
});
```

## Fluent API reference — LineSeries

| Property | Type | Default | Description |
|---|---|---|---|
| `Color` | `Color` | auto | Line color |
| `LineStyle` | `LineStyle` | `Solid` | Solid, Dashed, Dotted, DashDot |
| `LineWidth` | `double` | `1.5` | Line width in pixels |
| `Marker` | `MarkerStyle` | none | Circle, Square, Triangle, Diamond, Star, Cross, Plus, ... |
| `MarkerSize` | `double` | `6` | Marker diameter |
| `MarkerFaceColor` | `Color` | auto | Marker fill color |
| `MarkerEdgeColor` | `Color` | auto | Marker edge color |
| `MarkerEdgeWidth` | `double` | `1` | Marker edge width |
| `MarkEvery` | `int` | `1` | Show marker every N points |
| `DrawStyle` | `DrawStyle` | `Default` | Default, Steps, StepPre, StepMid, StepPost |
| `Smooth` | `bool` | `false` | Fritsch-Carlson cubic interpolation |
| `SmoothResolution` | `int` | `10` | Sub-points per interval when smoothing |
| `Label` | `string` | none | Legend label |
| `Visible` | `bool` | `true` | Show/hide series |
| `ZOrder` | `int` | `0` | Render order (higher = on top) |
