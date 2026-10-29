---
title: "Style a C# chart: themes, fonts and colors"
description: "Style C# charts: 30 built-in themes, fonts, colors, line styles, grids and spines, plus your own theme."
---

# Styling & Themes

> **Install:** `dotnet add package MatPlotLibNet`

## 30 built-in themes

| Light themes | Dark themes | Accessibility | Operator (control room) |
|---|---|---|---|
| `Theme.Default` | `Theme.Dark` | `Theme.ColorBlindSafe` | `Theme.OpsNight` |
| `Theme.Seaborn` | `Theme.Cyberpunk` | `Theme.HighContrast` | `Theme.OpsPanel` |
| `Theme.Ggplot` | `Theme.Nord` | | `Theme.OpsWarm` |
| `Theme.FiveThirtyEight` | `Theme.Dracula` | | `Theme.OpsContrast` |
| `Theme.Bmh` | `Theme.Monokai` | | |
| `Theme.Solarize` | `Theme.Catppuccin` | | |
| `Theme.Grayscale` | `Theme.Gruvbox` | | |
| `Theme.Paper` | `Theme.OneDark` | | |
| `Theme.Presentation` | `Theme.Neon` | | |
| `Theme.Poster` | | | |
| `Theme.GitHub` | | | |
| `Theme.Minimal` | | | |
| `Theme.Retro` | | | |
| `Theme.MatplotlibClassic` | | | |
| `Theme.MatplotlibV2` | | | |

See the [Themes cookbook page](themes.md) for visual examples and custom theme building.

## PropCycler

A prop cycler cycles colors, line styles, line widths and markers across multi-series plots. The series at
place i takes the cycler's entry i, in the plot and in its legend entry; a series that sets one of these itself
keeps its own:

```csharp
var cycler = new PropCyclerBuilder()
    .WithColors(Colors.Tab10Blue, Colors.Orange, Colors.Green, Colors.Red)
    .WithLineStyles(LineStyle.Solid, LineStyle.Dashed, LineStyle.Dotted, LineStyle.DashDot)
    .Build();

var theme = Theme.CreateFrom(Theme.Default).WithPropCycler(cycler).Build();

Plt.Create()
    .WithTheme(theme)
    .AddSubPlot(1, 1, 1, ax =>
    {
        ax.Plot(x, y1, s => s.Label = "Series 1");
        ax.Plot(x, y2, s => s.Label = "Series 2");
        // colors and line styles are assigned automatically
        ax.WithLegend(LegendPosition.UpperRight);
    })
    .Save("styled.svg");
```

![PropCycler](../images/prop_cycler.png)

## Grid and spine styling

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y)
        // Grid with custom styling
        .WithGrid(g => g with
        {
            Color = Colors.LightGray,
            LineStyle = LineStyle.Dotted,
            LineWidth = 0.5,
            Alpha = 0.7
        })
        // Spine visibility
        .WithSpines(s => s with
        {
            Top = s.Top with { Visible = false },       // hide top spine
            Right = s.Right with { Visible = false },   // hide right spine
            Left = s.Left with { LineWidth = 1.2 },
            Bottom = s.Bottom with { LineWidth = 1.2 }
        })
        // Or use the shorthand:
        // .HideTopSpine()
        // .HideRightSpine()
    )
    .Save("grid_spines.svg");
```

`WithGrid` is kept as a change over the grid of whichever theme the figure is drawn under, so a theme set later keeps your
change and takes the rest from the theme, and a second `WithGrid` adds to the first. A polar chart's rings and spokes and a
3-D chart's walls follow it in colour, width and line style:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

double[] gridAngle = [.. Enumerable.Range(0, 100).Select(i => i * 2 * Math.PI / 100)];
double[] gridRadius = [.. gridAngle.Select(a => 1 + Math.Cos(3 * a))];

Plt.Create()
    .WithTitle("A polar chart on a dashed crimson grid")
    .WithSize(600, 600)
    .AddSubPlot(1, 1, 1, ax => ax
        .PolarPlot(gridRadius, gridAngle, s => { s.Color = Colors.Blue; s.LineWidth = 2; })
        .WithGrid(g => g with { Color = Colors.Crimson, LineStyle = LineStyle.Dashed }))
    .Save("polar_grid.svg");
```

![A three-petalled blue curve on a polar axes whose rings and spokes are dashed crimson lines](../images/polar_grid.png)


## The neutral grey

Reference lines, price levels, Fibonacci lines, trendlines, waterfall connectors, a dumbbell's connector and the outlines of a
map wear one grey, `Theme.NeutralColor`. A theme that sets none derives it from its plot: the library's grey where that stands
at 3 : 1 or more against the plot, otherwise the nearest grey that does, so Nord, Dracula, Monokai, Gruvbox and OpsPanel get one of
their own. `ThemeBuilder.WithNeutralColor` names one by hand, and a colour set on the line itself always wins:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Styling;

double[] weekNumber = [1, 2, 3, 4, 5, 6, 7, 8];
double[] throughput = [3.1, 3.9, 3.4, 4.8, 5.2, 4.9, 6.3, 6.8];

Plt.Create()
    .WithTheme(Theme.Nord)
    .WithTitle("Reference lines under Nord")
    .WithSize(700, 450)
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(weekNumber, throughput, s => s.Label = "Throughput")
        .AxHLine(5.0, line => line.Label = "Target")
        .AxHLine(3.0, line => line.Label = "Floor")
        .SetXLabel("Week")
        .WithLegend())
    .Save("neutral_colour.svg");
```

![A line chart on Nord's dark background with two dashed grey reference lines, Target at 5 and Floor at 3](../images/neutral_colour.png)


## Tight margins

Remove all padding around the data:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y)
        .WithTightMargins()     // data touches spines
        .WithYTicksMirrored()   // Y ticks on both sides
        .WithXTicksMirrored()   // X ticks on top and bottom
    )
    .Save("tight_margins.svg");
```

## Scientific paper template

The template applies publication-quality defaults: hidden top and right spines, tight layout and 150 dpi. The
dpi sizes the PNG: the template's default 800 × 600 is drawn as a 1250 × 938 PNG, the same layout drawn finer.
An SVG and a PDF page are vector drawings and do not change with it:

```csharp
FigureTemplates.ScientificPaper(
    ax => ax
        .Plot(t, y, s => { s.Label = @"$e^{-0.15t}\cos(2t)$"; s.LineWidth = 1.2; })
        .SetXLabel("t (s)")
        .SetYLabel("Amplitude")
        .WithLegend(LegendPosition.UpperRight),
    title: "Damped Oscillation")
.Save("scientific.svg");
```

![Scientific paper](../images/scientific_paper.png)

## Sparkline dashboard

This template draws compact metric panels. Each metric is a `NamedSample`, a name and its values, and gets one row,
named on its Y axis. There is no axes chrome:

```csharp
FigureTemplates.SparklineDashboard(
    [
        new NamedSample("CPU %",    cpuData),
        new NamedSample("Memory %", memData),
        new NamedSample("Disk I/O", diskData),
    ],
    title: "Server Metrics — Last 60s")
.Save("sparklines.svg");
```

![Sparkline dashboard](../images/sparkline_dashboard.png)

## Hide all axes chrome

Use this for custom visualizations where you control everything. A series that needs no axes (a treemap, a Sankey, a pie, a stat
tile) draws none on its own, so the call is for a chart that has axes you want gone:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .HideAllAxes()  // no spines, ticks, or labels
        .Treemap(root, s => s.ShowLabels = true))
    .Save("bare_canvas.svg");
```

## Fluent API reference

| Method | Description |
|---|---|
| `.WithTheme(Theme)` | Apply a theme preset or custom theme |
| `.WithBackground(Color)` | Override figure background |
| `.WithGrid(Func<GridStyle, GridStyle>)` | Grid color, style, width, alpha |
| `.ShowGrid(bool)` | Show/hide grid |
| `.WithSpines(Func<SpinesConfig, SpinesConfig>)` | Spine visibility |
| `.HideTopSpine()` | Hide top spine |
| `.HideRightSpine()` | Hide right spine |
| `.HideAllAxes()` | Hide all chrome |
| `.WithTightMargins()` | Zero axis padding |
| `.WithYTicksMirrored()` | Y ticks on both sides |
| `.WithXTicksMirrored()` | X ticks on top and bottom |
| `.WithMinorTicks(bool)` | Show/hide minor ticks |
