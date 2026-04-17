# Bar Charts

## Bar chart with labels

```csharp
string[] products = ["Alpha", "Beta", "Gamma", "Delta"];
double[] sales    = [12_500, 34_800, 8_200, 27_600];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("Sales by Product")
        .SetYLabel("Revenue ($)")
        .Bar(products, sales, bar => { bar.Color = Colors.Tab10Blue; bar.Label = "Q1 Sales"; })
        .WithBarLabels("F0")
        .SetYTickFormatter(new EngFormatter()))
    .Save("bar_labels.svg");
```

![Bar labels](../images/bar_labels.png)

## Horizontal bars

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Bar(products, sales, s =>
        {
            s.Orientation = BarOrientation.Horizontal;
            s.Color = Colors.Teal;
        })
        .WithBarLabels())
    .Save("bar_horizontal.svg");
```

## Stacked bars

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .SetBarMode(BarMode.Stacked)
        .Bar(products, q1, s => { s.Color = Colors.Tab10Blue; s.Label = "Q1"; })
        .Bar(products, q2, s => { s.Color = Colors.Orange;    s.Label = "Q2"; })
        .Bar(products, q3, s => { s.Color = Colors.Green;     s.Label = "Q3"; })
        .WithLegend()
        .WithBarLabels())
    .Save("bar_stacked.svg");
```

## Grouped bars

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .SetBarMode(BarMode.Grouped)
        .Bar(products, q1, s => { s.Color = Colors.CornflowerBlue; s.Label = "Q1"; })
        .Bar(products, q2, s => { s.Color = Colors.Salmon;         s.Label = "Q2"; })
        .WithLegend(LegendPosition.UpperLeft))
    .Save("bar_grouped.svg");
```

## Bar width, edge color, and alpha

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Bar(products, sales, s =>
        {
            s.Color = Colors.CornflowerBlue;
            s.EdgeColor = Colors.Navy;
            s.LineWidth = 1.5;
            s.BarWidth = 0.5;   // narrower bars (default 0.8)
            s.Alpha = 0.7;
            s.Align = BarAlignment.Center;
        }))
    .Save("bar_styled.svg");
```

## Hatching patterns

Add texture to bars for print-friendly charts:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Bar(products, q1, s =>
        {
            s.Color = Colors.LightBlue;
            s.Hatch = HatchPattern.Slash;
            s.HatchColor = Colors.DarkBlue;
            s.Label = "Q1";
        })
        .Bar(products, q2, s =>
        {
            s.Color = Colors.LightCoral;
            s.Hatch = HatchPattern.BackslashDouble;
            s.HatchColor = Colors.DarkRed;
            s.Label = "Q2";
        })
        .WithLegend())
    .Save("bar_hatched.svg");
```

## Bar labels with custom format

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Bar(products, sales, s =>
        {
            s.ShowLabels = true;
            s.LabelFormat = "C0";  // currency format
        }))
    .Save("bar_custom_labels.svg");
```

## Fluent API reference — BarSeries

| Property | Type | Default | Description |
|---|---|---|---|
| `Color` | `Color` | auto | Bar fill color |
| `EdgeColor` | `Color` | auto | Bar outline color |
| `LineWidth` | `double` | `0.5` | Outline width |
| `BarWidth` | `double` | `0.8` | Width as fraction |
| `Orientation` | `BarOrientation` | `Vertical` | Vertical or Horizontal |
| `Align` | `BarAlignment` | `Center` | Center or Edge |
| `Alpha` | `double` | `1.0` | Transparency (0–1) |
| `Hatch` | `HatchPattern` | `None` | Slash, Backslash, Dash, Pipe, CrossHash, ... |
| `HatchColor` | `Color` | auto | Hatch pattern color |
| `ShowLabels` | `bool` | `false` | Display value labels |
| `LabelFormat` | `string` | `"G4"` | .NET format string for labels |
| `Label` | `string` | none | Legend label |

| Axes method | Description |
|---|---|
| `.SetBarMode(BarMode.Stacked)` | Stack multiple bar series |
| `.SetBarMode(BarMode.Grouped)` | Side-by-side grouped bars |
| `.WithBarLabels(format?)` | Show value labels on last bar |
