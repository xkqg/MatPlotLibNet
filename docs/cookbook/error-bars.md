# Error Bars

## Symmetric error bars

```csharp
double[] x = [1, 2, 3, 4, 5];
double[] y = [2.1, 4.5, 3.2, 6.8, 5.1];
double[] yerr = [0.5, 0.3, 0.8, 0.4, 0.6];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .ErrorBar(x, y, yerr, s =>
        {
            s.Color = Colors.Blue;
            s.Label = "Measurement";
        })
        .WithLegend())
    .Save("error_bars.svg");
```

## Asymmetric error bars

```csharp
double[] yerrLow = [0.3, 0.2, 0.5, 0.3, 0.4];
double[] yerrHigh = [0.8, 0.5, 1.0, 0.6, 0.9];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .ErrorBar(x, y, yerrLow, yerrHigh))
    .Save("asymmetric_errors.svg");
```
