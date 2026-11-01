// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Mcp;

/// <summary>Describes the spec to the model that has to write one. The field lists are REFLECTED off the very DTO
/// records the serializer reads, so the documentation cannot drift from the reader: add a field to the wire format
/// and it appears here on the next build. The worked example is a real spec — a test renders it.</summary>
internal sealed class ChartSchemaDescription
{
    /// <summary>Series fields whose meaning is not obvious from the name alone.</summary>
    private static readonly IReadOnlyDictionary<string, string> Notes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["type"] = "the chart type; list_chart_types names every accepted value",
        ["xData"] = "x values; must be the same length as yData, or as each dataset of a stacked area (a signal has none: its x values follow from signalSampleRate)",
        ["yData"] = "y values; must be the same length as xData",
        ["color"] = "a CSS4 colour name (\"steelblue\") or #RRGGBB (#RRGGBBAA with transparency)",
        ["label"] = "the name of this series: a legend entry, unless labelPlacement says lineEnd",
        ["labelPlacement"] = "Legend (the default) or LineEnd; LineEnd writes the label at the end of a line series, in its colour, instead of in the legend",
        ["c"] = "a scatter's value per point, which the colour map (colorMapName or colorMap) turns into that point's colour",
        ["colorMapName"] = "the name of a colour map the library ships (viridis, coolwarm, greens, tab10, ...; a name ending in _r is that map reversed)",
        ["colorMap"] = "a colour map built in code, written by kind: {\"type\": a colour map kind named in the overview, then that kind's fields}; a map the library ships goes by its name in colorMapName instead",
        ["pairGridOffDiagonalColorMapDescription"] = "a pair grid's off-diagonal colour map built in code, written by kind as colorMap is; a map the library ships goes by its name in pairGridOffDiagonalColorMap",
        ["drawStyle"] = "how a line, or an area, steps between its points (StepsPre, StepsMid, StepsPost)",
        ["stackBaseline"] = "where each bar starts, one value per bar; a bar is drawn from there up",
        ["fill"] = "true or false: false draws a density curve (kde) as its line alone, without the area under it; a colour goes in color",
        ["hatches"] = "the hatch of each band of a filled contour, lowest band first, one hatch name per band",
        ["rMax"] = "the outer radius of a polar heatmap, in data units",
        ["totalIndices"] = "the steps of a waterfall that are totals, by position from 0: a total bar stands on zero, reaches the value it holds (a level, not a change) and wears totalColor, and the steps after it start from that level",
        ["colors"] = "one colour per part, in order: each point of a scatter, each slice of a pie or a donut, each stage of a funnel, each row of an event plot, each group of a pair grid's hue",
        ["density"] = "true scales a histogram so that its bars cover an area of 1; false (the default) counts",
        ["streamDensity"] = "how closely a stream plot packs its streamlines; 1 is the default",
        ["rWidth"] = "the width of a histogram bar as a fraction of its bin; 1 fills the bin",
        ["autoPct"] = "the format of the percentage written on each slice of a pie, such as \"%1.1f%%\"",
        ["explode"] = "how far each slice of a pie is pulled out of the circle, one value per slice",
        ["fontSize"] = "the point size of the text a table, a tree grid or a line contour's labels write",
        ["zOrder"] = "the drawing order: a higher one is drawn on top; a filled area defaults to -1, everything else to 0",
        ["packageData"] = "data another package writes for its own series type (a map from MatPlotLibNet.Geo); this server reads none, so a spec leaves it out",
        ["tickLocator"] = "where the ticks go, written by kind: {\"type\": a tick locator named below, then that kind's fields}",
        ["tickFormatter"] = "how the tick labels read, written by kind: {\"type\": a tick formatter named below, then that kind's fields}",
        ["link"] = "makes the axis show the primary axis' range in another unit (matplotlib's secondary_xaxis / secondary_yaxis), written by kind: {\"type\": an axis link kind named below, then that kind's fields}; drawn on the top X axis (secondaryXAxis) and on a Y axis past the primary, which then holds no series and has no range of its own; absent for an axis with a range of its own",
        ["normalizer"] = "how the values of a series drawn through a colour map (scatter, heatmap, contour, image, 3-D surface and the like) map onto it, written by kind: {\"type\": a normaliser named in the overview, then that kind's fields}",
        ["dpi"] = "the resolution, 96 by default: a PNG is drawn with dpi / 96 pixels for each unit of width and height, so 192 doubles its size in pixels with the same layout; the picture may not exceed the canvas ceiling in pixels. SVG and PDF are vector and ignore it",
        ["theme"] = "the figure's look; absent for the matplotlib-v2 default. Written field by field over the plain 'default' theme: a field left out is that theme's. Its name is a label, not a preset to load",
        ["gridOverridesTheme"] = "true when grid is the axes' own choice, drawn whatever the theme's grid is (a grid switched off included); a reader that knows gridChange reads that instead, so leave this out when you write gridChange",
        ["firstCellDate"] = "the date of the cell in row 0, column 0 of a heatmap that is a calendar (a week to a column, the seven days of it down the column): the table of the chart then gives every cell its date, the first cell's plus 7 days for each column and one for each row; absent for a heatmap that is no calendar",
        ["neutralColor"] ="the grey that reference lines, trendlines, price levels, a map's outlines and the like wear over the plot; absent, the theme derives it from its plot (#808080 where that stands at 3 : 1 or more against the plot, otherwise the nearest grey that does)",
        ["wearsNeutral"] = "true for a series that wears the theme's neutral grey in place of a colour of its own (a dumbbell's connector, a map's outline); a colour the series names wins, and the series then holds no colour of the cycle",
        ["gridChange"] ="what the axes changed about the theme's grid: only the properties it names, each with its value, applied over the grid of the theme the figure is drawn under, so it follows the theme (visible false draws no grid even under a theme that draws one); absent when nothing was changed",
        ["extraRightYAxes"] = "the right Y axes past the first, outward from the plot: entry i is the right axis i + 1, secondaryYAxis being axis 0 (with none, an empty axis 0 stands first); each entry is a Y axis slot, below",
        ["rightYAxesOverflow"] = "more right Y axes, in the same outward order: it continues where extraRightYAxes stops, and the reader takes both lists in that order; each entry is a Y axis slot",
        ["leftYAxes"] = "the left Y axes past the primary yAxis, outward from the plot: entry i is the left axis i + 1; each entry is a Y axis slot, below",
        ["leftYAxesOverflow"] = "more left Y axes, in the same outward order: it continues where leftYAxes stops, and the reader takes both lists in that order; each entry is a Y axis slot",
        ["yAxisTitleGap"] = "the blank gap in pixels between each Y axis title and its tick labels, and between two stacked Y axes; 4 when absent, never negative",
        ["legend"] = "the legend box: its place (position), columns, frame and title. A subplot draws its own legend whether or not this is written. On the figure it is the legend of the whole figure (matplotlib's figlegend): it lists the labelled series of every subplot, each entry that looks the same once, and is static; absent draws none, and {} is a legend at its defaults",
        ["texts"] = "the texts placed on the figure itself, outside every subplot (matplotlib's figtext): a note, a source line, a panel letter; drawn above the subplots in the order written, each entry a figure text, below",
        ["images"] = "the images placed on the figure itself, under every subplot (matplotlib's figimage): a logo, a watermark, a texture; drawn in the order written, each entry a figure image, below",
        ["alignment"] = "how the text stands against its point horizontally: Left (the default; the text starts at the point), Center or Right (the text ends at the point)",
        ["verticalAlignment"] = "how the text stands against its point vertically: Baseline (the default; the baseline is on the point), Center, Top (the text hangs from the point) or Bottom (the text stands on it)",
        ["rotation"] = "the angle the text is turned through about its point, in degrees, counter-clockwise when positive; absent for 0",
        ["subplot"] = "the cell of a rows-by-columns grid the subplot stands in, as matplotlib's add_subplot(rows, cols, index) names it: {\"rows\": 2, \"cols\": 2, \"index\": 3}, the index counted from 1 along the rows from the top left; absent, a subplot stands where gridPosition puts it, or else fills the figure when it is the only one, and the subplots that name no cell stand side by side in one row",
        ["signals"] = "buy and sell markers, each {\"x\", \"y\", \"direction\": Buy (an upward triangle below the point) or Sell (a downward one above it), and optionally \"color\", \"size\" in pixels (12 by default) and \"label\"}",
        ["patches"] = "shapes drawn in data coordinates, matplotlib's patches (rectangle, polygon, arrow, circle, ellipse, wedge, arc), in the order written, each entry a patch told apart by its type, below; a subplot that holds patches and no series is drawn, framed around its patches",
        ["zoomIndicator"] = "on an inset (an entry of insets[]) only: the zoom the inset indicates on its parent (matplotlib's indicate_inset_zoom), a rectangle over the region the inset shows, spanning the limits the inset draws, and the lines from it to the inset's frame; drawn on a Cartesian parent only; absent draws none, {} is an indicator at its defaults; its fields are below",
    };

    /// <summary>Notes that hold for the field of one record only, where the same wire name means something else in another:
    /// <c>x</c> is a data coordinate on an annotation and a fraction of the figure on a figure text. Read before <see cref="Notes"/>.</summary>
    private static readonly IReadOnlyDictionary<(string Record, string Field), string> RecordNotes = new Dictionary<(string, string), string>
    {
        [("FigureTextDto", "text")] = "the words to draw",
        [("FigureTextDto", "x")] = "the place of the text's point as a fraction of the figure's width from the left: 0 is the left edge, 1 the right; a place outside 0 to 1 is kept and not drawn",
        [("FigureTextDto", "y")] = "the place of the text's point as a fraction of the figure's height counted up from the bottom, as in matplotlib: 0 is the bottom edge, 1 the top, so 0.97 is just under the top",
        [("FigureImageDto", "data")] = "the pixel values as a list of rows of equal length, the first row being the top row of the image; each value is one pixel of the figure, drawn at its native size through the colour map; a missing cell is the string \"NaN\"",
        [("FigureImageDto", "xOffset")] = "pixels from the left edge of the figure to the left edge of the image",
        [("FigureImageDto", "yOffset")] = "pixels from the bottom edge of the figure to the bottom edge of the image, counted up as in matplotlib's figimage",
        [("FigureImageDto", "vMin")] = "the value at the low end of the colour range; absent for the smallest value that can be drawn (a missing cell and an infinity are left out)",
        [("FigureImageDto", "vMax")] = "the value at the high end of the colour range; absent for the largest value that can be drawn",
        [("FigureImageDto", "alpha")] = "the opacity of the whole image, 0 (clear) to 1 (opaque); absent for 1",
        [("ZoomIndicatorDto", "color")] = "the colour of the rectangle and the connectors, a CSS4 colour name or #RRGGBB; absent for the theme's neutral grey (matplotlib's 0.5 grey)",
        [("ZoomIndicatorDto", "alpha")] = "the opacity of the rectangle and the connectors, 0 (clear) to 1 (opaque); absent for 0.5, matplotlib's",
        [("ZoomIndicatorDto", "lineWidth")] = "the width of their lines in pixels, 0 or more; absent for 1",
        [("ZoomIndicatorDto", "lineStyle")] = "the dash pattern of their lines: Solid (the default), Dashed, Dotted, DashDot or None (draws nothing)",
        [("PatchDto", "edgeColor")] = "the colour of the outline, a CSS4 colour name or #RRGGBB; absent for no outline (an arrow and an arc: black)",
        [("PatchDto", "edgeWidth")] = "the width of the outline in pixels; absent for 1",
        [("PatchDto", "edgeLineStyle")] = "the dash pattern of the outline: Solid (the default), Dashed, Dotted, DashDot or None (draws no outline)",
        [("PatchDto", "alpha")] = "the opacity of the whole patch, face and outline, 0 (clear) to 1 (opaque); absent for 1",
        [("PatchDto", "label")] = "the name the legend lists the patch under, beside a rectangle swatch; absent for no entry; a label that starts with _ is left out",
        [("PatchDto", "hatch")] = "the pattern over the face: None (the default), ForwardDiagonal, BackDiagonal, Horizontal, Vertical, Cross, DiagonalCross, Dots or Stars",
        [("PatchDto", "hatchColor")] = "the colour of the hatch strokes; absent for the colour of the edge",
        [("FilledPatchDto", "faceColor")] = "the colour of the face, a CSS4 colour name or #RRGGBB; absent for matplotlib's first colour; an arc has no face and takes none",
        [("RectangleDto", "anchor")] = "the corner the rectangle is measured from, a point",
        [("RectangleDto", "width")] = "along X, in data units; a negative width extends the rectangle to the left",
        [("RectangleDto", "height")] = "along Y, in data units; a negative height extends the rectangle downward",
        [("RectangleDto", "angle")] = "the angle the rectangle is turned through about its anchor, in degrees, counter-clockwise; absent for 0",
        [("PolygonDto", "vertices")] = "the corners in order, each a point",
        [("PolygonDto", "closed")] = "true (the default) joins the last vertex back to the first; false leaves the outline open",
        [("ArrowDto", "start")] = "where the shaft starts, a point",
        [("ArrowDto", "dx")] = "how far the shaft runs along X, in data units",
        [("ArrowDto", "dy")] = "how far the shaft runs along Y, in data units",
        [("ArrowDto", "width")] = "the width of the shaft, in data units; absent for 0.001",
        [("ArrowDto", "headWidth")] = "the width of the head across the shaft, in data units; absent for three times the width of the shaft",
        [("ArrowDto", "headLength")] = "the length of the head along the shaft, in data units; absent for one and a half times the width of the head",
        [("ArrowDto", "lengthIncludesHead")] = "true puts the tip on the end point (start plus dx, dy); false (the default) puts it beyond the end point, as matplotlib's FancyArrow does",
        [("ArrowDto", "shape")] = "which part of the arrow is drawn: Full (the default), or Left or Right for one half of it",
        [("ArrowDto", "overhang")] = "how far the back of the head is swept towards the tip, as a fraction of its length; absent for 0",
        [("CircleDto", "center")] = "the centre of the circle, a point",
        [("CircleDto", "radius")] = "in data units: the circle is round in data units, so it looks like an ellipse when the two axes have unequal scales",
        [("EllipseDto", "center")] = "the centre of the ellipse, a point",
        [("EllipseDto", "width")] = "the full width before the ellipse is turned, in data units (a diameter, not a radius)",
        [("EllipseDto", "height")] = "the full height before the ellipse is turned, in data units (a diameter, not a radius)",
        [("EllipseDto", "angle")] = "the angle the ellipse is turned through about its centre, in degrees, counter-clockwise; absent for 0",
        [("WedgeDto", "center")] = "the centre of the disc the wedge is cut from, a point",
        [("WedgeDto", "radius")] = "the radius of that disc, in data units",
        [("WedgeDto", "theta1")] = "the angle the wedge starts at, in degrees counter-clockwise from the positive X direction",
        [("WedgeDto", "theta2")] = "the angle the wedge ends at; it runs counter-clockwise from theta1 to theta2",
        [("WedgeDto", "width")] = "absent: the wedge reaches the centre; set: a ring segment this thick, measured in from the radius, in data units",
        [("ArcDto", "center")] = "the centre of the ellipse the arc lies on, a point",
        [("ArcDto", "width")] = "the full width of that ellipse before it is turned, in data units",
        [("ArcDto", "height")] = "the full height of that ellipse before it is turned, in data units",
        [("ArcDto", "angle")] = "the angle the ellipse is turned through about its centre, in degrees, counter-clockwise; absent for 0",
        [("ArcDto", "theta1")] = "the angle the arc starts at, in degrees counter-clockwise from the positive X direction; absent for 0",
        [("ArcDto", "theta2")] = "the angle the arc ends at; it runs counter-clockwise from theta1; absent for 360, the whole ellipse",
        [("LinearAxisTransformDto", "scale")] = "what the primary value is multiplied by: a number other than 0; below 0 the linked axis runs the other way",
        [("LinearAxisTransformDto", "offset")] = "what is added after the scale; absent for 0",
        [("ReciprocalAxisTransformDto", "numerator")] = "what is divided by the primary value: a number other than 0; absent for 1",
        [("LogarithmAxisTransformDto", "base")] = "the base of the logarithm: a number above 0 other than 1; absent for 10; below 1 the linked axis runs the other way",
        [("PowerAxisTransformDto", "exponent")] = "the power the primary value is raised to: a number other than 0; a negative one runs the linked axis the other way",
        [("ZoomIndicatorDto", "connectors")] = "true (the default) draws the two of the four corner lines matplotlib's rule keeps for where the inset stands against the region (none when one holds the other); false draws the rectangle alone",
    };

    /// <summary>What each kind of patch is, by the kind a spec writes.</summary>
    private static readonly IReadOnlyDictionary<string, string> PatchKindNotes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["rectangle"] = "a rectangle measured from one corner",
        ["polygon"] = "straight edges through the vertices in order",
        ["arrow"] = "a flat arrow in data units (matplotlib's FancyArrow): a shaft from start along dx, dy and a head at its end",
        ["circle"] = "a circle of a radius in data units",
        ["ellipse"] = "an ellipse; the width and the height are the full diameters",
        ["wedge"] = "a pie slice of a disc, or a segment of a ring when width is set",
        ["arc"] = "the outline of an ellipse between two angles; it has an edge and no face, as matplotlib draws one",
    };

    /// <summary>What each kind of axis link is, by the kind a spec writes.</summary>
    private static readonly IReadOnlyDictionary<string, string> AxisLinkKindNotes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["linear"] = "the primary value times scale, plus offset (Fahrenheit beside Celsius: scale 1.8, offset 32; radians beside degrees: scale 0.017453292519943295)",
        ["reciprocal"] = "numerator divided by the primary value (the period beside a frequency); it is its own inverse, and the primary range must not cross 0",
        ["logarithm"] = "the logarithm of the primary value in a base; the primary range must lie above 0",
        ["power"] = "the primary value raised to an exponent (an area beside a length: exponent 2); defined on 0 and above",
    };

    /// <summary>One field as the spec writes it: its wire name, what kind of value it takes and the record it is a field of.</summary>
    private readonly record struct WireField(string Name, string Type, string Record);

    private readonly ChartTypeCatalog _catalog;

    /// <summary>Creates the description over the types <paramref name="catalog"/> lists.</summary>
    public ChartSchemaDescription(ChartTypeCatalog catalog) => _catalog = catalog;

    /// <summary>A renderable spec that shows the shape — the smallest document that draws something.</summary>
    public const string Example = """
        {
          "width": 800,
          "height": 600,
          "title": "Revenue",
          "subPlots": [
            {
              "xAxis": { "label": "Quarter" },
              "yAxis": { "label": "€M" },
              "series": [
                { "type": "line", "xData": [1, 2, 3, 4], "yData": [12, 18, 15, 22], "label": "2026", "color": "steelblue" }
              ]
            }
          ]
        }
        """;

    /// <summary>The whole spec shape, or the fields of one chart type when <paramref name="seriesType"/> names one.</summary>
    public string Describe(string? seriesType)
    {
        if (string.IsNullOrWhiteSpace(seriesType))
        {
            return Overview();
        }

        if (!_catalog.IsListed(seriesType))
        {
            string hint = _catalog.Suggest(seriesType) is { } meant ? $" Did you mean '{meant}'?" : "";
            throw new ToolRefusalException($"'{seriesType}' is not a chart type this server lists.{hint} list_chart_types names them all.");
        }

        var text = new StringBuilder();
        text.AppendLine($"Chart type '{seriesType}' — a series object at any series seat of a subplot ({SeatPaths(SeatKind.Series)}).")
            .AppendLine()
            .AppendLine("Every field a series may carry (the same set the renderer reads; a type uses the subset that applies to it):");
        AppendFields(text, FieldsOf(typeof(SeriesDto)));
        return text.ToString().TrimEnd();
    }

    private string Overview()
    {
        var text = new StringBuilder();
        text.AppendLine("A chart spec is the MatPlotLibNet figure JSON: a figure, its subplots, and the series on them.")
            .AppendLine()
            .AppendLine("Figure fields:");
        AppendFields(text, FieldsOf(TypeNamed("FigureDto")));
        text.AppendLine().AppendLine("Subplot fields (subPlots[]):");
        AppendFields(text, FieldsOf(TypeNamed("AxesDto")));
        text.AppendLine().AppendLine($"Axis fields ({SeatPaths(SeatKind.Axis)}):");
        AppendFields(text, FieldsOf(TypeNamed("AxisDto")));
        text.AppendLine().AppendLine($"Y axis slot fields (an entry of {SlotListPaths()}: one more Y axis of its side, and the series drawn against it):");
        AppendFields(text, FieldsOf(TypeNamed("SecondaryAxisSlotDto")));
        text.AppendLine().AppendLine("Tick fields (an axis's majorTicks / minorTicks):");
        AppendFields(text, FieldsOf(TypeNamed("TickConfigDto")));
        text.AppendLine().AppendLine("Text style fields (subPlots[].titleStyle, an axis's labelStyle):");
        AppendFields(text, FieldsOf(TypeNamed("TextStyleDto")));
        text.AppendLine().AppendLine("Legend fields (subPlots[].legend, and the legend of the figure as a whole):");
        AppendFields(text, FieldsOf(TypeNamed("LegendDto")));
        text.AppendLine().AppendLine("Colour bar fields (subPlots[].colorBar, and the figure's figureColorBar):");
        AppendFields(text, FieldsOf(TypeNamed("ColorBarDto")));
        text.AppendLine().AppendLine("Zoom indicator fields (subPlots[].insets[].zoomIndicator: the zoom an inset indicates on its parent):");
        AppendFields(text, FieldsOf(TypeNamed("ZoomIndicatorDto")));
        text.AppendLine().AppendLine("Patch kinds (an entry of subPlots[].patches[], and of an inset's: a shape in data coordinates, drawn with the spans, under the reference lines and the series; \"type\" names the kind and the fields of that kind follow it. A point is {\"dataX\": x, \"dataY\": y}; sizes and radii are in data units; angles are in degrees, counter-clockwise as in matplotlib):");
        AppendNotedKinds(text, "PatchDto", PatchKindNotes);
        text.AppendLine().AppendLine("Patch style fields (every kind of patch takes these; faceColor every kind but arc):");
        AppendFields(text, [.. FieldsDeclaredBy(TypeNamed("PatchDto")), .. FieldsDeclaredBy(TypeNamed("FilledPatchDto"))]);
        text.AppendLine().AppendLine("Figure text fields (an entry of the figure's texts[]: a text placed on the figure itself, in fractions of it):");
        AppendFields(text, FieldsOf(TypeNamed("FigureTextDto")));
        text.AppendLine().AppendLine("Figure image fields (an entry of the figure's images[]: an image placed on the figure itself, in pixels of it):");
        AppendFields(text, FieldsOf(TypeNamed("FigureImageDto")));
        text.AppendLine().AppendLine("Grid fields (subPlots[].grid, and a theme's defaultGrid):");
        AppendFields(text, FieldsOf(TypeNamed("GridDto")));
        text.AppendLine().AppendLine("Grid change fields (subPlots[].gridChange: only the fields named change the theme's grid, the rest stay the theme's):");
        AppendFields(text, FieldsOf(TypeNamed("GridChangeDto")));
        text.AppendLine().AppendLine("Theme fields (the figure's theme):");
        AppendFields(text, FieldsOf(TypeNamed("ThemeDto")));
        text.AppendLine().AppendLine("Cycler fields (a theme's propCycler: the lists each next series steps through):");
        AppendFields(text, FieldsOf(TypeNamed("PropCyclerDto")));
        text.AppendLine().AppendLine("Alarm palette fields (a theme's alarm):");
        AppendFields(text, FieldsOf(TypeNamed("AlarmPaletteDto")));
        text.AppendLine().AppendLine("Spacing fields (the figure's spacing, and a theme's defaultSpacing):");
        AppendFields(text, FieldsOf(TypeNamed("SubPlotSpacingDto")));
        text.AppendLine().AppendLine("Interaction theme fields (the figure's interactionTheme):");
        AppendFields(text, FieldsOf(TypeNamed("InteractionThemeDto")));
        text.AppendLine().AppendLine("Tick locators (an axis's tickLocator: \"type\" names the kind, the other fields are that kind's):");
        AppendKinds(text, typeof(TickLocatorDto));
        text.AppendLine().AppendLine("Tick formatters (an axis's tickFormatter):");
        AppendKinds(text, typeof(TickFormatterDto));
        text.AppendLine().AppendLine("Axis link kinds (an axis's link, written by kind: on the top X axis (subPlots[].secondaryXAxis) or on a Y axis past the primary (subPlots[].secondaryYAxis, and the axis of an entry of the Y axis slot lists) it makes that axis show the primary axis' range in another unit, as matplotlib's secondary_xaxis and secondary_yaxis do; a linked axis has no range of its own and holds no series. \"type\" names the kind and the fields of that kind follow it; the ticks are laid out in the linked unit, each where the primary axis shows its value):");
        AppendNotedKinds(text, "AxisTransformDto", AxisLinkKindNotes);
        text.AppendLine().AppendLine("Normalisers (the normalizer of a series, or of a figure image, drawn through a colour map):");
        AppendKinds(text, typeof(NormalizerDto));
        text.AppendLine().AppendLine("Colour maps built in code (a series', a colour bar's or a figure image's colorMap; colours as names or #RRGGBB; positions rise from 0 to 1, one per colour; a reversed map holds the map it reverses, a named one the name a map is registered under):");
        AppendKinds(text, typeof(ColorMapDto));
        text.AppendLine().AppendLine($"Series fields (a series stands at {SeatPaths(SeatKind.Series)}) — {FieldsOf(typeof(SeriesDto)).Count} in total, each type uses the ones that apply:");
        AppendFields(text, FieldsOf(typeof(SeriesDto)).Take(24).ToArray());
        text.AppendLine("  … ask describe_chart_schema for one chart type to see the whole list.")
            .AppendLine().AppendLine("Example:").AppendLine(Example);
        return text.ToString().TrimEnd();
    }

    /// <summary>The seats of <paramref name="kind"/> a subplot keeps, written as the paths the reader names in a refusal
    /// (<c>subPlots[].xAxis / .yAxis / ...</c>). They come off the shape the reader walks, so the schema cannot name a seat
    /// the reader does not check, nor leave one out.</summary>
    private static string SeatPaths(SeatKind kind) =>
        PathsOf(AxesSeatShape.OfSubplot.Names.Where(seat => seat.Kind == kind).Select(seat => seat.Path));

    /// <summary>The lists of Y axis slots a subplot keeps, in the same notation as <see cref="SeatPaths"/>.</summary>
    private static string SlotListPaths() => PathsOf(AxesSeatShape.OfSubplot.SlotLists);

    private static string PathsOf(IEnumerable<string> paths) => $"subPlots[].{string.Join(" / .", paths)}";

    private static void AppendFields(StringBuilder text, IReadOnlyList<WireField> fields, string indent = "  ")
    {
        foreach (var (name, type, record) in fields)
        {
            text.Append(indent).Append(name).Append(": ").Append(type);
            if (RecordNotes.TryGetValue((record, name), out var note) || Notes.TryGetValue(name, out note))
            {
                text.Append("  — ").Append(note);
            }
            text.AppendLine();
        }
    }

    /// <summary>One kind per line, as the reader accepts it: the <c>type</c> value, then the fields that kind takes.
    /// Read off the <c>JsonDerivedType</c> attributes of the family's base record, the list the serializer reads by.</summary>
    private static void AppendKinds(StringBuilder text, Type family)
    {
        foreach (var kind in family.GetCustomAttributes<JsonDerivedTypeAttribute>())
        {
            var fields = FieldsOf(kind.DerivedType);
            text.Append("  ").Append((string)kind.TypeDiscriminator!);
            if (fields.Count > 0)
            {
                text.Append(": ").Append(string.Join(", ", fields.Select(field => $"{field.Name}: {field.Type}")));
            }
            text.AppendLine();
        }
    }

    /// <summary>One kind of a described family per block, as the reader accepts it: the <c>type</c> value and what the kind is, then
    /// the fields that kind adds to what the layers of its family above it hold, each with its note. Read off the
    /// <c>JsonDerivedType</c> attributes of the family's base record, the list the serializer reads by. The patches and the links of an
    /// axis are listed this way.</summary>
    /// <param name="text">Where the kinds are written.</param>
    /// <param name="family">The name of the family's base record.</param>
    /// <param name="kindNotes">What each kind is, by the kind a spec writes.</param>
    /// <exception cref="KeyNotFoundException">A kind of the family has no entry in <paramref name="kindNotes"/>: a kind cannot be
    /// listed without saying what it is, and the schema tests run this.</exception>
    private static void AppendNotedKinds(StringBuilder text, string family, IReadOnlyDictionary<string, string> kindNotes)
    {
        foreach (var kind in TypeNamed(family).GetCustomAttributes<JsonDerivedTypeAttribute>())
        {
            string name = (string)kind.TypeDiscriminator!;
            text.Append("  ").Append(name).Append("  — ").AppendLine(kindNotes[name]);
            AppendFields(text, FieldsDeclaredBy(kind.DerivedType), indent: "    ");
        }
    }

    /// <summary>The wire fields of a DTO record, in the camelCase the serializer writes and reads: its public properties
    /// and the internal ones it carries on the wire (<c>JsonInclude</c>).</summary>
    private static IReadOnlyList<WireField> FieldsOf(Type dto) => WireFieldsOf(dto, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>The wire fields <paramref name="dto"/> declares itself, without those it inherits: a layer of a stacked record
    /// family, whose base layers are listed once rather than under every kind.</summary>
    private static IReadOnlyList<WireField> FieldsDeclaredBy(Type dto) =>
        WireFieldsOf(dto, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    private static IReadOnlyList<WireField> WireFieldsOf(Type dto, BindingFlags which) =>
        [.. dto.GetProperties(which)
            .Where(p => p.GetMethod is { IsPublic: true } || p.IsDefined(typeof(JsonIncludeAttribute)))
            .Select(p => new WireField(JsonNamingPolicy.CamelCase.ConvertName(p.Name), WireTypeOf(p.PropertyType), dto.Name))];

    private static Type TypeNamed(string name) =>
        typeof(SeriesDto).Assembly.GetType($"MatPlotLibNet.Serialization.{name}")
        ?? throw new InvalidOperationException($"The serializer no longer declares {name}.");

    private static string WireTypeOf(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying.IsArray)
        {
            return WireTypeOf(underlying.GetElementType()!) + "[]";
        }

        if (underlying.IsGenericType && underlying.GetGenericTypeDefinition() == typeof(List<>))
        {
            return WireTypeOf(underlying.GetGenericArguments()[0]) + "[]";
        }

        return underlying.Name switch
        {
            nameof(String) => "string",
            nameof(Double) => "number",
            nameof(Int32) => "integer",
            nameof(Boolean) => "boolean",
            nameof(DateTime) => "string (date)",
            "Color" => "string (colour)",
            _ => underlying.IsEnum ? "string (enum)" : "object",
        };
    }
}
