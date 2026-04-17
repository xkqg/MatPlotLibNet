# Broken / Discontinuous Axes

Remove a range from the axis to show widely separated data without wasting space:

## Y-axis break

```csharp
double[] x = Enumerable.Range(0, 20).Select(i => (double)i).ToArray();
double[] y = x.Select(v => v < 10 ? v * 2 : v * 2 + 80).ToArray();

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => s.Label = "Data")
        .WithYBreak(25, 85)  // remove the gap between 25 and 85
        .WithLegend())
    .Save("broken_y.svg");
```

## X-axis break

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y)
        .WithXBreak(5, 15))  // skip the middle
    .Save("broken_x.svg");
```

## Break styles

```csharp
// Zigzag (default)
ax.WithYBreak(25, 85, BreakStyle.Zigzag)

// Straight diagonal lines
ax.WithYBreak(25, 85, BreakStyle.Straight)

// No visual marker
ax.WithYBreak(25, 85, BreakStyle.None)
```
