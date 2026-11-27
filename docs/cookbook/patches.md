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
| `Rectangle` | `Rectangle` | `new Rectangle(anchor, width, height)` | `Angle`, degrees counter-clockwise about the `RotationPoint` (the anchor, `RotationPoint.Center` or `RotationPoint.At(point)`); a negative width or height extends it left of or below the anchor |
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
| `EdgeColor` | none on a filled shape, black on an `Arrow` and an `Arc` | The outline. A colour of none is `Colors.Transparent`. Under the classic matplotlib theme a shape that was given no colour is edged in black, as matplotlib's `patch.force_edgecolor` does; one set to `Colors.Transparent` stays without an edge. |
| `EdgeWidth` | 1 | The width of the outline in pixels; 0 draws none, and a negative width is refused. Until you set it the theme decides, as matplotlib's `patch.linewidth` does: 1 point (1.39 pixels) under the matplotlib themes, 1 pixel under the library's own. |
| `EdgeLineStyle` | `LineStyle.Solid` | The dash of the outline; `LineStyle.None` draws none. |
| `Alpha` | none given (reads 1) | The opacity of the whole patch, in place of the opacity of the face, the edge and the hatch, as matplotlib's `alpha` replaces it; a colour of none stays none; a value outside 0 to 1 is refused. Left alone, each colour keeps its own opacity. |
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
        .SetXLim(0, 15).SetYLim(0, 8)
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

The axes holds the very patch it was given, so a patch can be made first, added, and changed afterwards. A patch stands at
matplotlib's z-order of a patch, 1, so a line (2) runs over it; under the default style the grid (1.5) is drawn over it too,
and under the library's own themes the grid stays under everything:

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
drawn over it, and the triangle marks the start of the rise. `Alpha` replaces the opacity of the face, the edge and the hatch
alike, as matplotlib's `alpha` does, so a colour that is already translucent becomes as opaque as `Alpha` says and a colour
of none stays none.

## A patch over the series

`ZOrder` places a patch among the series, as matplotlib's `zorder` does: one with a higher value is drawn over one with a
lower value. A label box given 3 stands over the line it marks:

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

double[] hour = [0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24];
double[] load = [31, 28, 26, 30, 52, 71, 74, 70, 66, 69, 58, 41, 33];

Plt.Create()
    .AddSubPlot(1, 1, 1, ax => ax
        .Plot(hour, load, line => line.Label = "Load (%)")
        .AddPatch(new Rectangle(new(9, 66), 6, 12) { FaceColor = Colors.White, EdgeColor = Colors.Black, ZOrder = 3 })
        .SetXLabel("Hour"))
    .Save("peak_box.svg");
```

The JSON writes a z-order only when one was given (`"zOrder": 3`), so a patch at rest keeps its kind's place.

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
        .SetXLim(0, 12).SetYLim(0, 9)
        .AddPatch(new Circle(new(4, 6), 2.5))
        .AddPatch(new Rectangle(new(7, 1), 4, 2))
        .AddPatch(new Polygon([new(1, 1), new(5, 1), new(1, 4)])))
    .AddSubPlot(1, 2, 2, ax => ax
        .WithTitle("Log X axis")
        .SetXScale(AxisScale.Log)
        .SetXLim(0.8, 12).SetYLim(0, 9)
        .AddPatch(new Circle(new(4, 6), 2.5))
        .AddPatch(new Rectangle(new(7, 1), 4, 2))
        .AddPatch(new Polygon([new(1, 1), new(5, 1), new(1, 4)])))
    .Save("patches_log_axis.svg");
```

![The same circle, rectangle and triangle on a linear and on a log X axis: on the log axis the circle is stretched on its left and narrow on its right, while the rectangle and the triangle keep their straight edges](../images/patches_log_axis.png)

A coordinate a log axis cannot place, 0 or below, stands far outside the axis, where matplotlib's log scale puts it, and
the outline is cut where it leaves the plot: an edge to such a point dives almost straight to the edge of the plot, and a
filled shape runs along that edge. A point inside an axis break leaves the outline open, and it resumes after the gap. Every vertex of every kind is held to the one matplotlib computes for the same patch, to 1e-12.

## Turning a rectangle about its centre

A rectangle turns about its anchor corner unless its `RotationPoint` says otherwise, as matplotlib's `rotation_point`
does: `RotationPoint.Center` for the middle of the four corners, `RotationPoint.At(point)` for a point of your own in data
units.

```csharp
using MatPlotLibNet;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Patches;
using MatPlotLibNet.Styling;

Plt.Create()
    .WithSize(640, 400)
    .AddSubPlot(1, 1, 1, ax => ax
        .WithTitle("The same rectangle turned 30 degrees about three points")
        .SetXLim(-1, 7)
        .SetYLim(-2, 5)
        .AddPatch(new Rectangle(new(1, 1), 2, 1) { Angle = 30, FaceColor = Colors.Tab10Blue, Alpha = 0.5 })
        .AddPatch(new Rectangle(new(1, 1), 2, 1) { Angle = 30, RotationPoint = RotationPoint.Center, FaceColor = Colors.Tab10Orange, Alpha = 0.5 })
        .AddPatch(new Rectangle(new(1, 1), 2, 1) { Angle = 30, RotationPoint = RotationPoint.At(new(3, 0)), FaceColor = Colors.Tab10Green, Alpha = 0.5 }))
    .Save("patches_rotation_point.svg");
```

The point travels in JSON as `rotationCenter` (true) or `rotationPoint` (`{"dataX": …, "dataY": …}`); a rectangle turned
about its anchor writes neither.

## The range, the clip and the legend

- **The automatic range holds every patch**, over its true extent: its vertices, and the points where a curve turns, not
  its control points. A patch adds no sticky edge, and axes that share a scale share its range. Under the library's own
  themes an axes that holds patches and nothing else frames them, and a rectangle of no size counts. Under the matplotlib
  themes (`Theme.MatplotlibV2`, `Theme.MatplotlibClassic`) a patch widens an axis only where a series or a reference line
  asks for the range, as matplotlib's `add_patch` does, so an axes of patches alone stays at 0 to 1 (set the limits, as the
  examples above do) and a rectangle with neither width nor height is left out.
- **Under a limit you set** (`SetXLim`, `SetYLim`), the patches are clipped to the plot, as the series are. Under the matplotlib
  themes they are clipped too where nothing ranges the axes for them (an axes of patches alone, or a reference line that ranges
  one axis), as matplotlib clips every patch to the axes.
- **A circle, an ellipse, a wedge or an arc of size 0 draws nothing**, and a negative size mirrors the shape, as
  matplotlib's scale does.
- **A labelled patch has a row in the legend**, among the rows of the series in the order they were added, as matplotlib
  lists them, drawn as a rectangle in the patch's own face, edge, dash, hatch and opacity whatever its shape (matplotlib's
  patch handle). A click on the row hides the patch and a second click shows it again (`Patch.Visible`), as a click does a
  series. A label that is empty or starts with an underscore (`"_hidden"`) gives no row. A figure legend (`FigLegend`) lists
  every row of every subplot under the matplotlib themes, as `fig.legend()` does; under the library's own themes it lists a
  patch drawn the same in two subplots once.
- **Where patches are drawn:** on every axes, each placing the patch's data its own way, as matplotlib draws a patch through
  the axes' `transData`. A Cartesian axes and an inset through their transform; a polar axes with x the angle in radians and
  y the radius, each vertex and control point mapped on its own (a rectangle is its four corners joined straight, as matplotlib
  joins them), the patches ranging the radius as they range a Cartesian axis and cut off at the circle where they reach past
  it; a ternary axes in the triangle's own frame, one unit wide (`TernaryPoint.InFrame()` gives a composition's place), each
  patch cut off at the triangle; a 3-D axes on a plane, `Z` along `ZDir`
  (`ZDirection.Z`, the X-Y plane at height 0, by default), matplotlib's `pathpatch_2d_to_3d`. A 3-D patch takes no part in the
  cube. On every axes a patch stands among the series by its `ZOrder`, under them when it is given none.

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
[MCP server](mcp.md) checks the kind, the outline style, the hatch, an arrow's shape and the plane's `zDir` by name. A spec that holds
patches and no series is drawn.

## Where it differs from matplotlib

- Under the library's own themes an axes of patches alone frames them, and a rectangle of no size still counts in the range;
  the matplotlib themes follow matplotlib on both.

A second axis that shows the values of the first in another unit is on the [linked axes](linked-axes.md) page.
