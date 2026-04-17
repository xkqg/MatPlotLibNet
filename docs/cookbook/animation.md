# Animation

## FuncAnimation — declarative animated GIF

Define a frame generator function, produce an animated GIF:

```csharp
double[] x = Enumerable.Range(0, 100).Select(i => i * 0.1).ToArray();

var anim = new FuncAnimation(
    frameCount: 60,
    frameGenerator: i =>
    {
        double t = i / 60.0 * 2 * Math.PI;
        double[] y = x.Select(v => Math.Sin(v + t)).ToArray();
        return Plt.Create()
            .WithTitle($"Frame {i + 1}/60")
            .WithTheme(Theme.Dark)
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x, y, s => { s.Color = Colors.Cyan; s.LineWidth = 2; })
                .SetYLim(-1.5, 1.5))
            .Build();
    },
    delayMs: 50);  // 50ms between frames = 20 FPS

anim.Save("wave.gif");
```

## Animation with theme and styling

```csharp
var anim = new FuncAnimation(
    frameCount: 30,
    frameGenerator: i =>
    {
        double angle = i * 12;  // rotate 12° per frame
        return Plt.Create()
            .WithTheme(Theme.Cyberpunk)
            .WithSize(600, 600)
            .AddSubPlot(1, 1, 1, ax => ax
                .Surface(x, y, z, s => { s.ColorMap = ColorMaps.Plasma; s.Alpha = 0.9; })
                .WithCamera(elevation: 30, azimuth: -60 + angle))
            .Build();
    },
    delayMs: 80);  // 80ms = ~12.5 FPS

anim.Save("rotating_surface.gif");
```

## Save individual frames as PNG

Export each frame as a numbered PNG file:

```csharp
anim.SaveFrames("frames/", prefix: "wave");
// Creates: frames/wave_0001.png, frames/wave_0002.png, ...
```

## Multi-page PDF report

Combine multiple charts into a single PDF document:

```csharp
var figures = new[]
{
    Plt.Create()
        .WithTitle("Page 1 — Line Chart")
        .Plot(x, y1, s => s.Label = "Revenue")
        .WithLegend()
        .Build(),

    Plt.Create()
        .WithTitle("Page 2 — Distribution")
        .AddSubPlot(1, 1, 1, ax => ax
            .Hist(data, 30, s => s.Color = Colors.Teal))
        .Build(),

    Plt.Create()
        .WithTitle("Page 3 — Heatmap")
        .AddSubPlot(1, 1, 1, ax => ax
            .Heatmap(matrix)
            .WithColorMap("viridis")
            .WithColorBar())
        .Build(),
};

new PdfTransform().TransformMultiPage(figures, "report.pdf");
```

## Practical example: growing bar chart

```csharp
string[] categories = ["A", "B", "C", "D", "E"];
double[] finalValues = [42, 28, 65, 31, 53];

var anim = new FuncAnimation(
    frameCount: 40,
    frameGenerator: i =>
    {
        double progress = (i + 1.0) / 40;
        double[] current = finalValues.Select(v => v * progress).ToArray();
        return Plt.Create()
            .WithTitle($"Growth ({progress:P0})")
            .AddSubPlot(1, 1, 1, ax => ax
                .Bar(categories, current, s => s.Color = Colors.Tab10Blue)
                .SetYLim(0, 70)
                .WithBarLabels("F0"))
            .Build();
    },
    delayMs: 60);

anim.Save("growing_bars.gif");
```

## Fluent API reference

| Class / Method | Parameters | Description |
|---|---|---|
| `new FuncAnimation(frameCount, frameGenerator, delayMs)` | count, `Func<int, Figure>`, ms | Create animation |
| `anim.Save(path)` | `string` (.gif) | Save as animated GIF |
| `anim.SaveFrames(dir, prefix?)` | directory, file prefix | Export individual PNG frames |
| `new PdfTransform().TransformMultiPage(figures, path)` | `Figure[]`, path | Multi-page PDF |

> **Note:** GIF and PNG export requires `MatPlotLibNet.Skia`. PDF export also requires `MatPlotLibNet.Skia`.
