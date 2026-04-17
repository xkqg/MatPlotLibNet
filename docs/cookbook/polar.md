# Polar Charts

## Polar line

```csharp
double[] theta = Enumerable.Range(0, 100)
    .Select(i => i * 2 * Math.PI / 100).ToArray();
double[] r = theta.Select(t => 1 + Math.Cos(3 * t)).ToArray();

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .SetPolar()
        .PolarPlot(theta, r, s => { s.Color = Colors.Blue; s.Label = "r = 1 + cos(3θ)"; })
        .WithLegend())
    .Save("polar_line.svg");
```

## Radar chart

```csharp
string[] categories = ["Speed", "Power", "Defense", "Range", "Accuracy"];
double[] values = [85, 70, 90, 60, 95];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Radar(categories, values, s => { s.Color = Colors.Blue; s.Alpha = 0.3; }))
    .Save("radar.svg");
```

## Polar bar

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .SetPolar()
        .PolarBar(theta[..8], r[..8]))
    .Save("polar_bar.svg");
```
