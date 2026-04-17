# Symlog Axis

Symmetric logarithmic scale — linear near zero, logarithmic for large values. Perfect for data spanning both positive and negative ranges across orders of magnitude.

## Basic symlog Y-axis

```csharp
double[] x = Enumerable.Range(-50, 101).Select(i => (double)i).ToArray();
double[] y = x.Select(v => v * v * v).ToArray(); // cubic: ranges from -125000 to +125000

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => s.Label = "x³")
        .WithSymlogYScale(linthresh: 100)  // linear within [-100, 100]
        .WithLegend())
    .Save("symlog.svg");
```

## Financial P&L

```csharp
// Profit/loss data spanning -$50K to +$200K
double[] months = Enumerable.Range(1, 12).Select(i => (double)i).ToArray();
double[] pnl = [-5000, -2000, 500, 3000, -800, 15000, 45000, -3000, 80000, 120000, -10000, 200000];

Plt.Create()
    .WithTitle("Monthly P&L")
    .AddSubPlot(1, 1, 1, ax => ax
        .Bar(months.Select(m => $"M{m}").ToArray(), pnl)
        .WithSymlogYScale(linthresh: 1000)
        .SetYLabel("Profit/Loss ($)"))
    .Save("pnl_symlog.svg");
```

## How symlog works

```
 |x| <= linthresh  →  linear (identity)
 |x| >  linthresh  →  sign(x) × linthresh × (1 + log₁₀(|x|/linthresh))
```

The `linthresh` parameter controls where the transition from linear to logarithmic occurs. Smaller values = more of the axis is logarithmic.
