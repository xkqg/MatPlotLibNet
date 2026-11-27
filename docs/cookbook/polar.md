---
title: "Radar and polar charts in C#"
description: "Draw polar charts in C#: radar plots, rose diagrams, polar scatter and polar line charts with angular ticks."
---

# Polar Charts

> **Install:** `dotnet add package MatPlotLibNet`

## Polar line

```csharp
double[] theta = Enumerable.Range(0, 100)
    .Select(i => i * 2 * Math.PI / 100).ToArray();
double[] r = theta.Select(t => 1 + Math.Cos(3 * t)).ToArray();

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .PolarPlot(r, theta, s =>
        {
            s.Color = Colors.Blue;
            s.LineWidth = 2.0;
            s.LineStyle = LineStyle.Solid;
            s.Label = "r = 1 + cos(3θ)";
        })
        .WithLegend())
    .Save("polar_line.svg");
```

![Polar line](../images/polar_line.png)

## Multi-series polar

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .PolarPlot(r1, theta, s => { s.Color = Colors.Blue; s.Label = "cos(3θ)"; })
        .PolarPlot(r2, theta, s => { s.Color = Colors.Red; s.LineStyle = LineStyle.Dashed; s.Label = "sin(2θ)"; })
        .WithLegend(LegendPosition.UpperRight))
    .Save("polar_multi.svg");
```

## Radar chart

```csharp
string[] categories = ["Speed", "Power", "Defense", "Range", "Accuracy"];
double[] values = [85, 70, 90, 60, 95];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Radar(categories, values, s =>
        {
            s.Color = Colors.Blue;
            s.Alpha = 0.3;   // fill transparency
        }))
    .Save("radar.svg");
```

The web is a polygon with the first spoke at the top and the categories running clockwise, and the largest value reaches its outer
ring (set `MaxValue` to put the outer ring elsewhere). Under the library's own themes there are five rings at fifths of the axis.
Under the matplotlib themes, the default among them, the rings stand where matplotlib's polar axes would put them, at the ticks of its
default locator between 0 and the outer value, each labelled, drawn in the theme's grid, and the outer polygon is outlined as a frame.

## Radar comparison (two players)

```csharp
double[] player1 = [85, 70, 90, 60, 95];
double[] player2 = [70, 88, 60, 80, 75];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Radar(categories, player1, s => { s.Color = Colors.Blue; s.Alpha = 0.2; s.Label = "Player 1"; })
        .Radar(categories, player2, s => { s.Color = Colors.Red;  s.Alpha = 0.2; s.Label = "Player 2"; })
        .WithLegend())
    .Save("radar_comparison.svg");
```

![Radar comparison](../images/radar_comparison.png)

Two radars on one axes share one web under the matplotlib themes, as matplotlib's polar axes has one radial axis: the outer ring is
the largest outer value of the two (here Player 1's 95, so Player 2's 88 stops short of it), the rings are labelled once and the
frame is drawn once, over both. Under the library's own themes each radar keeps its own largest value at the outer ring.

## Polar bar

```csharp
double[] barTheta = Enumerable.Range(0, 8).Select(i => i * Math.PI / 4).ToArray();
double[] barR = [3, 5, 7, 4, 6, 8, 2, 5];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .PolarBar(barR, barTheta, s =>
        {
            s.Color = Colors.Teal;
            s.Alpha = 0.7;
            s.BarWidth = 0.35;
        }))
    .Save("polar_bar.svg");
```

## Polar scatter

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .PolarScatter(r, theta, s =>
        {
            s.Color = Css4Colors.Purple;
            s.MarkerSize = 4;
            s.Label = "Measurements";
        })
        .WithLegend())
    .Save("polar_scatter.svg");
```

## Polar heatmap

```csharp
var data = new double[12, 8]; // 12 angular bins × 8 radial bins
for (int t = 0; t < 12; t++)
    for (int r = 0; r < 8; r++)
        data[t, r] = Math.Sin(t * 0.5) * Math.Cos(r * 0.4);

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .PolarHeatmap(data, thetaBins: 12, rBins: 8)
        .WithColorMap("viridis")
        .WithColorBar())
    .Save("polar_heatmap.svg");
```

## Wind rose

How often the wind blows from each of eight directions, as one bar per direction. A polar bar takes its angle in
radians, counted counter-clockwise from the right (east), as matplotlib's polar axes do, and the angle labels count
the same way, so north stands at 90°. A compass bearing counts clockwise from north, so it is turned first:

```csharp
double[] bearings  = [0, 45, 90, 135, 180, 225, 270, 315];          // N, NE, E, SE, S, SW, W, NW
double[] frequency = [9.5, 7.8, 12.8, 6.3, 9.4, 14.1, 10.1, 7.1];   // % of the hours, all speeds
double[] theta = [.. bearings.Select(b => (90 - b) * Math.PI / 180)];

Plt.Create()
    .WithTitle("Wind Rose — share of the hours by direction")
    .WithSize(600, 600)
    .AddSubPlot(1, 1, 1, ax => ax
        .PolarBar(frequency, theta, s =>
        {
            s.Color = Color.FromHex("#2E86C1");
            s.Alpha = 0.85;
            s.BarWidth = 0.9 * Math.PI / 4;   // radians: nine tenths of the 45° between two directions
        }))
    .Save("wind_rose.svg");
```

![Wind rose](../images/wind_rose.png)

## Wind rose stacked by speed

A wind rose often splits each direction by wind speed, the slow winds near the centre and the fast ones stacked outside
them. Set the bar mode of the axes to stacked and its `PolarBar` series stand on one another, wedge by wedge: each starts
where the one below it ends, and the rings reach the top of the highest stack.

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

double[] bearings = [0, 45, 90, 135, 180, 225, 270, 315];
double[] theta = [.. bearings.Select(b => (90 - b) * Math.PI / 180)];
double[] calm = [3.1, 2.4, 4.0, 2.1, 3.0, 4.4, 3.3, 2.2];       // % of the hours, under 5 m/s
double[] breeze = [4.2, 3.6, 5.5, 2.9, 4.1, 6.0, 4.5, 3.2];     // 5 to 10 m/s
double[] strong = [2.2, 1.8, 3.3, 1.3, 2.3, 3.7, 2.3, 1.7];     // over 10 m/s

Plt.Create()
    .WithTitle("Wind Rose — share of the hours by direction and speed")
    .WithSize(640, 640)
    .AddSubPlot(1, 1, 1, ax => ax
        .PolarBar(calm, theta, s => { s.Color = Color.FromHex("#9ECAE1"); s.BarWidth = 0.9 * Math.PI / 4; s.Label = "under 5 m/s"; })
        .PolarBar(breeze, theta, s => { s.Color = Color.FromHex("#3182BD"); s.BarWidth = 0.9 * Math.PI / 4; s.Label = "5 to 10 m/s"; })
        .PolarBar(strong, theta, s => { s.Color = Color.FromHex("#08306B"); s.BarWidth = 0.9 * Math.PI / 4; s.Label = "over 10 m/s"; })
        .SetBarMode(BarMode.Stacked)
        .WithLegend())
    .Save("wind_rose_stacked.svg");
```

![A wind rose of eight wedges, each of three stacked layers, the palest nearest the centre](../images/wind_rose_stacked.png)

The wedges of one direction are the same index of every series. One series alone is not a stack, a radius that is not drawn
(missing, infinite or below zero) adds nothing, and the stacking is worked out when the chart is drawn and written into no
series.

## One radial scale

The grid and every polar series (lines, points, bars and heatmaps) read one radial scale. The rings stand at round numbers,
labelled in one number format (or by the axis' own formatter) at 22.5° so the labels stay clear of the bars, and a ring
labelled 10 stands exactly where a radius of 10 is drawn, whichever kind of series draws it, so two series on one axes can be
compared. Under the matplotlib themes the scale is ranged as matplotlib's polar axes ranges it: the default style pads the span
of the radii by 5 % at each end, the classic style rounds it out to the rings, and the centre is 0 unless a radius falls below it.
Under the library's own themes it reaches 5 % beyond the farthest radius. A limit you give the axes (`SetYLim`) is that end
exactly.

A radius below zero is drawn, as matplotlib draws it: the scale reaches down to it and its low end stands at the centre. Radii
from -20 to -3 give a scale from -20.85 to -2.15 under the default style. `SetROrigin` puts another radius at the centre
(matplotlib's `set_rorigin`); one below the low end leaves a hole there, bounded by an inner spine:

```csharp
Plt.Create()
    .AddSubPlot(1, 2, 1, ax => ax
        .PolarPlot([-20.0, -10, -3], [0.0, 2, 4]))
    .AddSubPlot(1, 2, 2, ax => ax
        .PolarPlot([1.0, 5, 9], [0.0, 2, 4])
        .SetROrigin(-3))
    .Save("polar_below_zero_and_origin.svg");
```

A locator you give the radial axis places the rings, as matplotlib's polar axes rings itself at the ticks of its radial axis'
locator. Rings every 3 out to 10:

```csharp
Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .PolarPlot([10.0, 10, 10], [0.0, 1, 2])
        .SetYTickLocator(new MultipleLocator(3)))
    .Save("polar_rings_every_3.svg");
```
