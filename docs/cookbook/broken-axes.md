---
title: "Broken axis charts in C#"
description: "Draw a broken axis in C#. Cut an empty range out of the axis so widely separated values share one chart without wasted space."
---

# Broken / Discontinuous Axes

> **Install:** `dotnet add package MatPlotLibNet`

Remove a range from the axis to show widely separated data without wasting space.

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

![Broken Y-axis](../images/broken_y.png)

## Under the matplotlib themes

matplotlib has no broken axis of its own; its gallery draws one as two subplots stacked one over the other, each showing one
part of the range. Under the matplotlib themes a broken axis is drawn that way. The parts stand apart by the gap matplotlib's
subplots keep between them (`figure.subplot.hspace`, a fifth of the mean part, `wspace` for a break of the X axis), and each
part keeps the length its range takes, so one unit of the data is as long on either side of the break. Each part has its own
ticks and its own spines, which stop where the part stops, and the gap between the parts shows the figure's background. A line
that crosses the break keeps the slope of the data in each part: it ends where it leaves one part and starts again where it
comes into the next. The marker stands at the corners of each part where the spines stop.

Under the library's own themes the break is cut out of a single axis, with one row of ticks and the marker across the middle of
the plot.

## X-axis break

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(x, y)
        .WithXBreak(5, 15))  // skip the middle
    .Save("broken_x.svg");
```

## Break styles

The break marker has three visual styles. Under the library's own themes it is drawn across the middle of the plot over a thin
band of the plot's background; under the matplotlib themes it is drawn at each corner of the cut, where the spines stop, a share of
the part's size each way. `Straight` there is the diagonal of matplotlib's gallery recipe:

```csharp
var builder = Plt.Create()
    .WithSize(1200, 400)
    .WithTitle("Break Styles");

builder.AddSubPlot(1, 3, 1, ax => ax
    .Plot(x, y).WithYBreak(25, 85, BreakStyle.Zigzag).WithTitle("Zigzag (default)"));

builder.AddSubPlot(1, 3, 2, ax => ax
    .Plot(x, y).WithYBreak(25, 85, BreakStyle.Straight).WithTitle("Straight"));

builder.AddSubPlot(1, 3, 3, ax => ax
    .Plot(x, y).WithYBreak(25, 85, BreakStyle.None).WithTitle("None (invisible)"));

builder.TightLayout().Save("break_styles.svg");
```

## Combined X + Y break

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Scatter(x, y, s => { s.Color = Colors.Red; s.MarkerSize = 8; })
        .WithXBreak(5, 15, BreakStyle.Zigzag)
        .WithYBreak(25, 85, BreakStyle.Straight)
        .WithTitle("Both axes broken"))
    .Save("broken_both.svg");
```

## Practical example: outlier handling

```csharp
// Sales data with one extreme outlier
string[] months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun"];
double[] sales = [120, 135, 8500, 142, 128, 155]; // March is a one-off bulk order

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Bar(months, sales, s => { s.Color = Colors.Teal; s.Label = "Sales ($K)"; })
        .WithYBreak(200, 8000)  // compress the gap
        .WithBarLabels("F0")
        .WithLegend())
    .Save("broken_outlier.svg");
```

## On a log or symlog axis

A break on a log or symlog axis takes its bounds in data units, as on a linear one, and is cut out of the axis as it is drawn:
`SetXScale(AxisScale.Log).WithXBreak(100, 1000)` hides the values between 100 and 1000 and removes that decade, so every
other decade keeps the same width on either side of the mark.

## On an interactive chart

A pointer over an axis with breaks reads the value drawn under it, past a break as well as before it: the native controls,
the browser chart talking to the server, the data cursor, hover, brush select, zoom and Home all read the axis by the
same rule that cut the break out. The one place where a break is cut, where both its ends are drawn, reads as the value
where the break begins. Under the matplotlib themes the gap between the parts is a share of the parts, so it grows and shrinks
with them: a wheel zoom is taken in the parts themselves, and the value under the pointer stays under it while the break stays in
view.

## Fluent API reference

| Method | Parameters | Description |
|---|---|---|
| `.WithXBreak(from, to)` | `double, double` | Remove X range |
| `.WithXBreak(from, to, style)` | `+ BreakStyle` | With visual style |
| `.WithYBreak(from, to)` | `double, double` | Remove Y range |
| `.WithYBreak(from, to, style)` | `+ BreakStyle` | With visual style |

| BreakStyle | Description |
|---|---|
| `Zigzag` | A zigzag across the gap (default); at each corner of the cut under the matplotlib themes |
| `Straight` | Two parallel slanted lines across the gap; the gallery recipe's diagonal at each corner under the matplotlib themes |
| `None` | No marker; the gap is still cut out |
