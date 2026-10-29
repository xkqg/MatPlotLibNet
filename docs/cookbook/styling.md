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
