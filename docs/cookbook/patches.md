---
title: "Rectangles, circles, polygons and arrows on a chart in C#"
description: "Draw matplotlib's patches in C# and .NET: a rectangle, polygon, arrow, circle, ellipse, wedge or arc in data coordinates, styled, listed in the legend, bent by a log axis and kept in JSON."
---

# Shapes in data coordinates

> **Install:** `dotnet add package MatPlotLibNet`

A patch is a shape you make yourself, style through its own properties and add to an axes with `AddPatch`, as
matplotlib's `add_patch` adds one. Its corners, vertices and centres are points in the units of the data, written
`new(x, y)`, so the shape stands where the data stands, stretches with the axes and counts in the automatic range. There
are seven kinds, in the namespace `MatPlotLibNet.Models.Patches`:

| Kind | matplotlib | Made with | Settings of its own |
|---|---|---|---|
| `Rectangle` | `Rectangle` | `new Rectangle(anchor, width, height)` | `Angle`, degrees counter-clockwise about the anchor; a negative width or height extends it left of or below the anchor |
| `Polygon` | `Polygon` | `new Polygon(vertices)` | `Closed` (true); fewer than three vertices draw nothing |
| `Arrow` | `FancyArrow` (`ax.arrow`) | `new Arrow(start, dx, dy)` | `Width` (0.001), `HeadWidth` (three times the width), `HeadLength` (one and a half times the head width), `LengthIncludesHead` (false), `Shape` (`ArrowHeadShape.Full`, `Left` or `Right`), `Overhang` (0) |
| `Circle` | `Circle` | `new Circle(center, radius)` | |
| `Ellipse` | `Ellipse` | `new Ellipse(center, width, height)` | `Angle`; the width and the height are diameters |
| `Wedge` | `Wedge` | `new Wedge(center, radius, theta1, theta2)` | `Width`, which makes it the slice of a ring |
| `Arc` | `Arc` | `new Arc(center, width, height)` | `Angle`, `Theta1` (0), `Theta2` (360); an arc has no face |

Every patch is styled the same way, and rests at matplotlib's patch style:

| Property | Default | What it does |
|---|---|---|
| `FaceColor` | `Colors.Tab10Blue`, matplotlib's `C0` | The fill. Every patch starts with the same colour: a patch never moves the colour cycle on, as a series does. An `Arc` has no face. |
| `EdgeColor` | none on a filled shape, black on an `Arrow` and an `Arc` | The outline. A colour of none is `Colors.Transparent`. |
| `EdgeWidth` | 1 | The width of the outline in pixels; 0 draws none, and a negative width is refused. |
| `EdgeLineStyle` | `LineStyle.Solid` | The dash of the outline; `LineStyle.None` draws none. |
| `Alpha` | 1 | The opacity of the whole patch, multiplied into the face, the edge and the hatch; a value outside 0 to 1 is refused. |
| `Hatch` | `HatchPattern.None` | A pattern over the face. |
| `HatchColor` | the edge colour, black when there is no edge | The colour of the hatch lines. |
| `Label` | none | A row in the legend. |

## Every kind on one axes

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

Plt.Create()
    .WithTitle("Every kind of patch")
    .WithSize(900, 560)
    .AddSubPlot(1, 1, 1, ax => ax
        .SetAspect(1)
        .AddPatch(new Rectangle(new(0.8, 0.8), 3, 1.6) { Angle = 15, FaceColor = Colors.Tab10Blue, Label = "Rectangle" })
        .AddPatch(new Polygon([new(5, 0.8), new(8, 0.8), new(8.4, 2.6), new(6.6, 3.6), new(4.8, 2.6)]) { FaceColor = Colors.Tab10Orange, Label = "Polygon" })
        .AddPatch(new Arrow(new(9.2, 0.9), 1.6, 1.2) { Width = 0.3, HeadWidth = 0.9, FaceColor = Colors.Tab10Green, Label = "Arrow" })
        .AddPatch(new Circle(new(2, 6.2), 1.2) { FaceColor = Color.FromHex("#D62728"), Label = "Circle" })
        .AddPatch(new Ellipse(new(5.8, 6.2), 3.6, 1.8) { Angle = 30, FaceColor = Color.FromHex("#9467BD"), Label = "Ellipse" })
        .AddPatch(new Wedge(new(9.6, 6.2), 1.4, 30, 150) { FaceColor = Color.FromHex("#8C564B"), Label = "Wedge" })
        .AddPatch(new Arc(new(13.6, 2.2), 2.4, 2.4) { Theta1 = 20, Theta2 = 250, EdgeWidth = 3, Label = "Arc" })
        .WithLegend())
    .Save("patch_kinds.svg");
```

![A turned blue rectangle, an orange pentagon, a green arrow, a red circle, a purple turned ellipse, a brown wedge and a black open arc, with a legend that lists all seven](../images/patch_kinds.png)

A patch is sized in data units, so a circle is round on the page only where a unit of x is as long as a unit of y:
`SetAspect(1)` makes it so. On axes of different scales a circle is an ellipse on the page, as it is in matplotlib.

The arrow is matplotlib's `FancyArrow`, the shape `ax.arrow` draws: its shaft runs from the start along `(dx, dy)`, every
size is in data units, and its tip stands a head length past the end point unless `LengthIncludesHead` is set. The shaft
is 0.001 wide by default, a hairline on any axes much wider than one unit, so set `Width` to the scale of the data. An
arrow that points at something from a text, with its head in pixels, is an annotation (see [Annotations](annotations.md)).

## A patch over a line

The axes holds the very patch it was given, so a patch can be made first, added, and changed afterwards. A patch is drawn
after the spans and before the reference lines and the series, so a line runs over it:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

double[] hour = [0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24];
double[] load = [31, 28, 26, 30, 52, 71, 74, 70, 66, 69, 58, 41, 33];

var window = new Rectangle(new(8, 20), 10, 60)
{
    FaceColor = Colors.Tab10Orange,
    Alpha = 0.25,
    EdgeColor = Colors.Tab10Orange,
    EdgeLineStyle = LineStyle.Dashed,
    Hatch = HatchPattern.ForwardDiagonal,
    Label = "Service window",
};

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(hour, load, line => line.Label = "Load (%)")
        .AddPatch(window)
        .AddPatch(new Polygon([new(4, 85), new(8, 85), new(6, 92)]) { FaceColor = Colors.Crimson, EdgeColor = Colors.Black })
        .SetXLabel("Hour")
        .WithLegend())
    .Save("service_window.svg");
```

The rectangle is a translucent orange band from 8 to 18 o'clock, its edge dashed and its face hatched, with the load line
drawn over it, and the triangle marks the start of the rise. `Alpha` multiplies the opacity of the face, the edge and the
hatch alike, so a colour that is already translucent becomes more so and a colour of none stays none; matplotlib's
`alpha` replaces each colour's opacity instead, which draws the same for opaque colours.

## On a log axis

A patch is drawn through the transform of the axes it stands on, and a curve is made of matplotlib's cubic Bézier curves
in data units: every control point goes through the log, so a circle bends as it does in matplotlib, and a straight edge
stays straight between its corners.

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;

Plt.Create()
    .WithSize(860, 400)
    .TightLayout()
    .AddSubPlot(1, 2, 1, ax => ax
        .WithTitle("Linear X axis")
        .AddPatch(new Circle(new(4, 6), 2.5))
        .AddPatch(new Rectangle(new(7, 1), 4, 2))
        .AddPatch(new Polygon([new(1, 1), new(5, 1), new(1, 4)])))
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle("Log X axis")
        .SetXScale(AxisScale.Log)
        .AddPatch(new Circle(new(4, 6), 2.5))
        .AddPatch(new Rectangle(new(7, 1), 4, 2))
        .AddPatch(new Polygon([new(1, 1), new(5, 1), new(1, 4)])))
    .Save("patches_log_axis.svg");
```

![The same circle, rectangle and triangle on a linear and on a log X axis: on the log axis the circle is stretched on its left and narrow on its right, while the rectangle and the triangle keep their straight edges](../images/patches_log_axis.png)

A coordinate a log axis cannot place, 0 or below, is pinned to the start of the axis (matplotlib pushes the point far
outside it, where the axes clip it at the same edge). A point inside an axis break leaves the outline open, and it
resumes after the gap. Every vertex of every kind is held to the one matplotlib computes for the same patch, to 1e-12.

## The range, the clip and the legend

- **The automatic range holds every patch**, over its true extent: its vertices, and the points where a curve turns, not
  its control points. A patch adds no sticky edge, axes that share a scale share its range, and an axes that holds patches
  and nothing else frames them (matplotlib's `add_patch` leaves such an axes at 0 to 1).
- **Under a limit you set** (`SetXLim`, `SetYLim`), the patches are clipped to the plot, as the series are.
- **A labelled patch has a row in the legend**, after the rows of the series, drawn as a rectangle in the patch's own
  face, edge, dash, hatch and opacity whatever its shape (matplotlib's patch handle). A click on the legend does not hide
  a patch. A label that is empty or starts with an underscore (`"_hidden"`) gives no row, and a figure legend
  (`FigLegend`) lists a patch drawn the same in two subplots once.
- **Where patches are drawn:** on a Cartesian axes and an inset. A polar, 3-D or ternary axes draws and lists none.

## In JSON and through the MCP server

A subplot writes its patches as `patches`, in the order they were added, each told apart by `type` and carrying its
geometry and every style that differs from a new patch of its kind; a point is `{"dataX": …, "dataY": …}`:

```json
"patches": [
  { "type": "rectangle", "anchor": { "dataX": 0, "dataY": 0 }, "width": 2, "height": 1, "angle": 15, "label": "zone" },
  { "type": "circle", "center": { "dataX": 3, "dataY": 1 }, "radius": 0.5, "faceColor": "#FF7F0E", "alpha": 0.6 },
  { "type": "arrow", "start": { "dataX": 0, "dataY": 2 }, "dx": 2, "dy": 0, "width": 0.1 }
]
```

The kinds are `rectangle`, `polygon`, `arrow`, `circle`, `ellipse`, `wedge` and `arc`. A kind the reader does not know is
skipped with a `ChartDiagnostics` entry and the rest of the figure loads, a 1.28.0 reader skips the list, and the
[MCP server](mcp.md) checks the kind, the outline style, the hatch and an arrow's shape by name. A spec that holds
patches and no series is drawn.

## Where it differs from matplotlib

- Patches and spans are drawn above the grid; matplotlib draws the grid above them.
- `Alpha` multiplies each colour's opacity; matplotlib's replaces it.
- An axes of patches alone frames them, and a rectangle of no size still counts in the range.
- A circle, an ellipse, a wedge or an arc of size 0 or less draws nothing; matplotlib draws a negative size mirrored.
- A patch has no `zorder` of its own, and a rectangle no `rotation_point`: a rectangle turned about its centre is a
  `Polygon` of its four corners.
- The legend lists the series first and the patches after them; matplotlib lists them in the order they were added.

A second axis that shows the values of the first in another unit is on the [linked axes](linked-axes.md) page.
