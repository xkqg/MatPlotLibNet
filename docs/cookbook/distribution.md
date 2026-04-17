# Distribution Charts

## Histogram

```csharp
var rng = new Random(42);
double[] data = Enumerable.Range(0, 1000)
    .Select(_ => rng.NextDouble() * 6 + rng.NextDouble() * 6)
    .ToArray();

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Hist(data, 30, s => { s.Color = Colors.Teal; s.Label = "Distribution"; })
        .WithLegend())
    .Save("histogram.svg");
```

## Box plot

```csharp
double[][] groups = [
    Enumerable.Range(0, 50).Select(_ => rng.NextDouble() * 10).ToArray(),
    Enumerable.Range(0, 50).Select(_ => rng.NextDouble() * 8 + 2).ToArray(),
    Enumerable.Range(0, 50).Select(_ => rng.NextDouble() * 12 - 1).ToArray(),
];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Box(groups, ["Group A", "Group B", "Group C"]))
    .Save("boxplot.svg");
```

## Violin plot

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Violin(groups, ["A", "B", "C"]))
    .Save("violin.svg");
```

## KDE (Kernel Density Estimation)

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Kde(data, s => { s.Label = "Density"; s.Color = Colors.Purple; })
        .WithLegend())
    .Save("kde.svg");
```
