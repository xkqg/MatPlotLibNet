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
        .ErrorBar(x, y, yerrLow, yerrHigh, s =>
        {
            s.Color = Colors.DarkRed;
            s.CapSize = 6;        // width of the error bar caps
            s.LineWidth = 1.5;
        }))
    .Save("asymmetric_errors.svg");
```

![Error bars](../images/error_bars.png)

## Error bars with styling

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .ErrorBar(x, y, yerr, s =>
        {
            s.Color = Colors.Navy;
            s.CapSize = 8;
            s.LineWidth = 2.0;
            s.Alpha = 0.8;
            s.Label = "Experiment A";
        })
        .WithLegend())
    .Save("error_styled.svg");
```

## Combined: scatter + error bars

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Scatter(x, y, s =>
        {
            s.Color = Colors.Red;
            s.MarkerSize = 10;
            s.Marker = MarkerStyle.Diamond;
            s.Label = "Data";
        })
        .ErrorBar(x, y, yerrLow, yerrHigh, s =>
        {
            s.Color = Colors.Gray;
            s.CapSize = 5;
            s.LineWidth = 1.0;
        })
        .WithLegend())
    .Save("scatter_errors.svg");
```

## Error bars with fill-between confidence band

```csharp
double[] yUpper = y.Zip(yerrHigh, (v, e) => v + e).ToArray();
double[] yLower = y.Zip(yerrLow, (v, e) => v - e).ToArray();

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => { s.Color = Colors.Blue; s.Label = "Mean"; })
        .FillBetween(x, yLower, yUpper, s =>
        {
            s.Color = Colors.Blue;
            s.Alpha = 0.2;
            s.Label = "95% CI";
        })
        .WithLegend())
    .Save("error_confidence.svg");
```

## Fluent API reference — ErrorBarSeries

| Property | Type | Default | Description |
|---|---|---|---|
| `Color` | `Color` | auto | Line and cap color |
| `CapSize` | `double` | `3` | Width of error bar caps in pixels |
| `LineWidth` | `double` | `1.0` | Error bar line width |
| `Alpha` | `double` | `1.0` | Transparency (0–1) |
| `Label` | `string` | none | Legend label |
