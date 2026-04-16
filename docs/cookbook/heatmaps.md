# Heatmaps & Colormaps

## Heatmap with colorbar

```csharp
double[,] matrix = new double[10, 10];
for (int r = 0; r < 10; r++)
    for (int c = 0; c < 10; c++)
        matrix[r, c] = Math.Sin(r * 0.5) * Math.Cos(c * 0.5);

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Heatmap — Plasma")
        .Heatmap(matrix)
        .WithColorMap("plasma")
        .WithColorBar(cb => cb with { Label = "Intensity" }))
    .TightLayout()
    .Save("heatmap.svg");
```

![Heatmap](../images/heatmap_colormap.png)

## Colormap comparison

MatPlotLibNet ships 104 colormaps. Here are four popular ones side by side:

```csharp
string[] maps = ["viridis", "turbo", "coolwarm", "greys"];
var builder = Plt.Create()
    .WithTitle("Colormap Comparison")
    .WithSize(1200, 800);

for (int i = 0; i < maps.Length; i++)
{
    var mapName = maps[i];
    builder.AddSubPlot(2, 2, i + 1, ax => ax
        .WithTitle(mapName)
        .Heatmap(matrix)
        .WithColorMap(mapName)
        .WithColorBar());
}

builder.TightLayout().Save("colormap_comparison.svg");
```

![Colormap comparison](../images/colormap_comparison.png)
