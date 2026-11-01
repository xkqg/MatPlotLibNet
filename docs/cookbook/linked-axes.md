---
title: "A second axis in another unit in C#: secondary_xaxis and secondary_yaxis"
description: "Show the same values in another unit on a second axis in C# and .NET, as matplotlib's secondary_xaxis and secondary_yaxis do: Celsius beside Fahrenheit, a frequency above its period, through a linear, reciprocal, logarithm or power transform."
---

# A second axis in another unit

> **Install:** `dotnet add package MatPlotLibNet`

A linked axis shows the values of the primary axis in another unit: degrees Fahrenheit beside degrees Celsius, the period
of a wave above its frequency, kilometres beside miles. It is what matplotlib calls `secondary_xaxis` and
`secondary_yaxis`. `Link(transform)` makes one of the top X axis (on the builder `WithSecondaryXAxis` hands you) or of a
Y axis added with `WithAddRightYAxis` or `WithAddLeftYAxis`.

A linked axis has no range and no series of its own. It shows the range and the scale of the primary axis, each value
written through the transform, and each of its ticks stands where the primary axis has the value the tick maps back to,
so the ticks of a Fahrenheit axis fall on round Fahrenheit numbers. Its label, its colour (`SetYAxisColor`,
`SetXAxisColor`) and its tick settings are set as on any other axis.

## The transforms

A transform is an `AxisTransform`, in the namespace `MatPlotLibNet.Models`, made with one of its factories. `Forward(value)`
maps a value of the primary axis to the value shown, and `Inverse(value)` maps it back.

| Factory | Shows | Where it places a range | For example |
|---|---|---|---|
| `AxisTransform.Linear(scale, offset = 0)` | `scale × value + offset` | everywhere | Fahrenheit from Celsius: `Linear(1.8, 32)`; radians from degrees: `Linear(Math.PI / 180)` |
| `AxisTransform.Reciprocal(numerator = 1)` | `numerator / value` | on one side of 0, never across it | a period from a frequency: `Reciprocal()`; a wavelength in metres from a frequency in hertz: `Reciprocal(299_792_458)` |
| `AxisTransform.Logarithm(base = 10)` | the logarithm of the value | above 0 | |
| `AxisTransform.Power(exponent)` | the value raised to the exponent | on 0 and above (above 0 for a negative exponent) | an area from a length: `Power(2)` |
| `AxisTransform.Of(forward, inverse)` | your own function | where it runs one way | anything the four do not cover |

Each factory refuses, as the transform is made, a number it cannot map with, by an `ArgumentOutOfRangeException` that
names the parameter: a scale, a numerator or an exponent of 0 or one that is not finite, a base that is not above 0, is 1
or is not finite. A linear transform is exact both ways, and a logarithm in base 10 of a power of ten is exact.

## Fahrenheit beside Celsius

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models;

double[] hour = [0, 3, 6, 9, 12, 15, 18, 21, 24];
double[] celsius = [3, 2, 1, 7, 14, 19, 17, 10, 6];

Plt.Create()
    .WithSize(760, 420)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("A day's temperature in two units")
        .Plot(hour, celsius)
        .SetXLabel("Hour")
        .SetYLabel("Temperature (°C)")
        .WithAddRightYAxis(right => right
            .SetYLabel("Temperature (°F)")
            .Link(AxisTransform.Linear(1.8, 32))))
    .Save("linked_fahrenheit.svg");
```

![A day's temperature over the hours with a left axis in degrees Celsius from 0 to 20 and a right axis in degrees Fahrenheit from 35 to 65](../images/linked_fahrenheit.png)

The left axis reads 0 to 20 °C, and the right axis reads the same heights in °F, with a tick at every 5 °F: the tick at
65 °F stands at the height of 18.3 °C. The Celsius line is the only series; the right axis holds none.

## The period above a log frequency axis

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models;

double[] frequency = [1, 2, 5, 10, 20, 50, 100, 200, 500, 1000];
double[] gain = [1.00, 1.00, 0.99, 0.98, 0.93, 0.71, 0.45, 0.24, 0.10, 0.05];

Plt.Create()
    .WithSize(760, 440)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("A low-pass filter, by frequency and by period")
        .Plot(frequency, gain)
        .SetXScale(AxisScale.Log)
        .SetXLabel("Frequency (Hz)")
        .SetYLabel("Gain")
        .WithSecondaryXAxis(top => top
            .SetXLabel("Period (s)")
            .Link(AxisTransform.Reciprocal())))
    .Save("linked_period.svg");
```

![A gain curve over frequencies from 1 to 1000 Hz on a log axis along the bottom, and along the top a log axis of periods from 1 s at the left to 0.001 s at the right](../images/linked_period.png)

Over a log primary axis the linked axis is a log axis too, with a tick at each power of ten of its own unit, as
matplotlib's `functionlog` draws it. A reciprocal turns the order around, so the period falls from 1 s at the left, where
the frequency is 1 Hz, to 0.001 s at the right.

## How the ticks are placed

The ticks of a linked axis are laid out over the range it shows (the image of the primary's range, low to high), by its
own tick locator when it has one (`Axis.TickLocator`), else by a log locator when the primary axis is logarithmic, else by
the default for the length of the plot. Each tick is labelled with its own value and drawn where the primary axis has
the value that maps back to it, so the primary's scale, direction and breaks place it. A tick the primary cannot place
(inside a break) is left out.

A limit set on a linked axis changes nothing: the axis has no range of its own to limit, and it neither clips the series
nor moves the primary axis. A pan, a zoom or Home moves it with the primary axis, in the browser and in the native
controls alike.

When the primary axis shows a range the transform cannot place (a reciprocal across 0, a logarithm reaching 0, a function
of your own that gives no number at an end or maps both ends to one value), the linked axis draws no ticks and reports it
once a render on `ChartDiagnostics`; nothing throws while drawing. An exception thrown by a function given to `Of` is
yours and travels as it is.

## What a linked axis holds

A linked axis holds no series, and the builders hold it to that whichever comes first: `Link` on an axis that holds a
series, and a series on an axis that is linked (`Plot`, `Scatter` or `StreamingPlot` of a Y axis, `PlotXSecondary` or
`ScatterXSecondary` of the top axis), are refused by an `ArgumentException` that names the parameter, before anything
changes. Called again, `Link` replaces the transform, as a second label replaces
the first.

## A top axis with a range of its own

A top X axis that is not linked keeps its own range, and follows its own settings: a tick locator, a tick spacing, a log,
symlog or date scale and minor ticks set on `SecondaryXAxis` are drawn, and a series on it stands where the primary Y
axis puts it, on a log, symlog or broken Y axis too. A top axis that sets none of them draws eight intervals across the
plot, as it always did.

## In JSON and through the MCP server

An axis writes its link as `link`, by `type` (`linear`, `reciprocal`, `logarithm`, `power`), with the numbers the
transform was made with; in a document written by hand an offset, a numerator or a base that is left out reads as 0, 1
or 10, the factories' defaults:

```json
"secondaryYAxis": {
  "label": "°F",
  "link": { "type": "linear", "scale": 1.8, "offset": 32 }
}
```

A link made with `Of` is a pair of functions, which is code a JSON document cannot carry: it is left out, and the axis
reads back unlinked. A kind the reader does not know is reported on `ChartDiagnostics` and the axis stays unlinked; a
number the factory refuses is refused when it is read. The [MCP server](mcp.md) checks the kind of a link at every axis
of a spec.

## Where it differs from matplotlib

- A linked axis is the top X axis or a Y axis past the primary one. matplotlib also puts one on the bottom, over the
  primary's ticks, or at a place of your own; neither is offered here, and a link set on a primary axis is ignored.
- `Power` is defined on 0 and above (above 0 for a negative exponent), where it runs one way.
- matplotlib draws whatever its functions return. Here a transform that cannot place the range draws no ticks and says
  why; a figure or a spec written by hand that gives a linked axis a series draws the link.

Shapes in data coordinates, which also follow a log axis, are on the [shapes](patches.md) page.
