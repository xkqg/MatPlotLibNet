# Annotations

## Arrow annotations with background

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Annotation Enhancements")
        .Plot(x, y, line => { line.Label = "Data"; })
        .Annotate("Peak", 8, 8.9, ann =>
        {
            ann.ArrowTargetX = 8;
            ann.ArrowTargetY = 8.9;
            ann.ArrowStyle   = ArrowStyle.FancyArrow;
            ann.BackgroundColor = Colors.White;
        })
        .Annotate("Rotated label", 2, 4.5, ann =>
        {
            ann.Rotation  = -30;
            ann.Alignment = TextAlignment.Center;
        }))
    .Save("annotations.svg");
```

![Annotations](../images/annotations_enhanced.png)
