# Line Charts

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
