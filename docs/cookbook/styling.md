# Styling & Themes

## Built-in themes

| Theme | Description |
|---|---|
| `Theme.Default` | Clean white background, tab10 cycle |
| `Theme.Dark` | Dark background, bright colors |
| `Theme.Seaborn` | Seaborn-inspired subtle grid |
| `Theme.MatplotlibClassic` | Pre-2.0 matplotlib look (`bgrcmyk` cycle, DejaVu Sans 12pt) |
| `Theme.MatplotlibV2` | Modern matplotlib default (soft-black, `tab10`, DejaVu Sans 10pt) |
| `Theme.ColorBlindSafe` | Okabe-Ito palette for color-vision accessibility |
| `Theme.HighContrast` | WCAG AAA high-contrast |

## PropCycler

Automatically cycle colors and line styles across multi-series plots:

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

## Scientific paper template

Publication-quality defaults: 150 DPI, hidden top/right spines, tight layout:

```csharp
FigureTemplates.ScientificPaper(
    ax => ax
        .Plot(t, y, s => { s.Label = "e^{-0.15t} cos(2t)"; s.LineWidth = 1.2; })
        .SetXLabel("t (s)")
        .SetYLabel("Amplitude")
        .WithLegend(LegendPosition.UpperRight),
    title: "Damped Oscillation")
.Save("scientific.svg");
```

![Scientific paper](../images/scientific_paper.png)

## Sparkline dashboard

Compact metric panels — one row per metric, no axes chrome:

```csharp
FigureTemplates.SparklineDashboard(
    [
        ("CPU %",    cpuData),
        ("Memory %", memData),
        ("Disk I/O", diskData),
    ],
    title: "Server Metrics — Last 60s")
.Save("sparklines.svg");
```

![Sparkline dashboard](../images/sparkline_dashboard.png)
