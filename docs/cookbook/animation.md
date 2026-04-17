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
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot(x, y, s => s.Color = Colors.Blue)
                .SetYLim(-1.5, 1.5))
            .Build();
    },
    delayMs: 50);

anim.Save("wave.gif");
```

## Save individual frames as PNG

```csharp
anim.SaveFrames("frames/", prefix: "wave");
// Creates: frames/wave_0001.png, frames/wave_0002.png, ...
```

## Multi-page PDF report

```csharp
var figures = new[]
{
    Plt.Create().WithTitle("Page 1").Plot(x, y1).Build(),
    Plt.Create().WithTitle("Page 2").Bar(categories, values).Build(),
    Plt.Create().WithTitle("Page 3").Heatmap(matrix).Build(),
};

new PdfTransform().TransformMultiPage(figures, "report.pdf");
```
