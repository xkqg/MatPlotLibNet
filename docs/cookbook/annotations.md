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

## Horizontal reference line

Mark thresholds, targets, or boundaries:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => s.Label = "Data")
        .AxHLine(5.0, l =>
        {
            l.Color = Colors.Red;
            l.LineStyle = LineStyle.Dashed;
            l.LineWidth = 1.5;
            l.Label = "Threshold";
        })
        .WithLegend())
    .Save("hline.svg");
```

## Vertical reference line

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y)
        .AxVLine(5.0, l =>
        {
            l.Color = Colors.Green;
            l.LineStyle = LineStyle.DashDot;
            l.Label = "Event";
        })
        .WithLegend())
    .Save("vline.svg");
```

## Shaded span regions

Highlight a range on the axis:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => s.Label = "Signal")
        // Vertical span: highlight an X range
        .AxVSpan(3.0, 6.0, s =>
        {
            s.Color = Colors.Yellow;
            s.Alpha = 0.3;
            s.Label = "Active period";
        })
        // Horizontal span: highlight a Y range
        .AxHSpan(4.0, 7.0, s =>
        {
            s.Color = Colors.LightBlue;
            s.Alpha = 0.2;
            s.Label = "Target range";
        })
        .WithLegend())
    .Save("spans.svg");
```

## Multiple reference lines

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y, s => s.Label = "Price")
        .AxHLine(20, l => { l.Color = Colors.Green; l.LineStyle = LineStyle.Dashed; l.Label = "Support"; })
        .AxHLine(70, l => { l.Color = Colors.Red;   l.LineStyle = LineStyle.Dashed; l.Label = "Resistance"; })
        .AxVLine(5,  l => { l.Color = Colors.Gray;  l.LineStyle = LineStyle.Dotted; l.Label = "Earnings date"; })
        .WithLegend(LegendPosition.UpperLeft))
    .Save("multi_ref.svg");
```

## Combining annotations with math text

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y)
        .Annotate(@"$R^{2} = 0.97$", 3, 7, ann =>
        {
            ann.BackgroundColor = Colors.White;
            ann.FontSize = 14;
        })
        .Annotate(@"$\frac{\partial f}{\partial x} = 0$", 8, 3, ann =>
        {
            ann.ArrowTargetX = 8;
            ann.ArrowTargetY = y[8];
            ann.ArrowStyle = ArrowStyle.FancyArrow;
        }))
    .Save("math_annotations.svg");
```

## Fluent API reference

| Method | Parameters | Description |
|---|---|---|
| `.Annotate(text, x, y, cfg?)` | text, position | Text annotation at (x, y) |
| `.Annotate(text, x, y, arrowX, arrowY, cfg?)` | + arrow target | Text with arrow pointing to target |
| `.AxHLine(y, cfg?)` | y position | Horizontal reference line |
| `.AxVLine(x, cfg?)` | x position | Vertical reference line |
| `.AxHSpan(yMin, yMax, cfg?)` | y range | Horizontal shaded band |
| `.AxVSpan(xMin, xMax, cfg?)` | x range | Vertical shaded band |

### Annotation properties

| Property | Type | Description |
|---|---|---|
| `ArrowTargetX/Y` | `double` | Arrow endpoint |
| `ArrowStyle` | `ArrowStyle` | FancyArrow, Simple, ... |
| `BackgroundColor` | `Color` | Text background |
| `FontSize` | `double` | Font size |
| `Rotation` | `double` | Text rotation in degrees |
| `Alignment` | `TextAlignment` | Left, Center, Right |

### ReferenceLine properties

| Property | Type | Description |
|---|---|---|
| `Color` | `Color` | Line color |
| `LineStyle` | `LineStyle` | Solid, Dashed, Dotted, DashDot |
| `LineWidth` | `double` | Line width |
| `Label` | `string` | Legend label |

### SpanRegion properties

| Property | Type | Description |
|---|---|---|
| `Color` | `Color` | Fill color |
| `Alpha` | `double` | Transparency (0–1) |
| `Label` | `string` | Legend label |
