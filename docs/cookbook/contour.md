# Contour Plots

## Contour with labels

```csharp
double[] x = Enumerable.Range(0, 20).Select(i => i * 0.5 - 5.0).ToArray();
double[] y = Enumerable.Range(0, 20).Select(i => i * 0.5 - 5.0).ToArray();
var z = new double[20, 20];
for (int r = 0; r < 20; r++)
    for (int c = 0; c < 20; c++)
        z[r, c] = Math.Sin(x[c]) * Math.Cos(y[r]);

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Contour with Labels")
        .Contour(x, y, z, s =>
        {
            s.ShowLabels = true;
            s.LabelFormat = "F2";
            s.LabelFontSize = 9;
        })
        .WithColorMap("coolwarm"))
    .Save("contour_labels.svg");
```

![Contour with labels](../images/contour_labels.png)
