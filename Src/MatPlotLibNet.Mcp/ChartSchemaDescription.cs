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
    };

    /// <summary>One field as the spec writes it: its wire name and what kind of value it takes.</summary>
    private readonly record struct WireField(string Name, string Type);

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
        text.AppendLine().AppendLine("Legend fields (subPlots[].legend):");
        AppendFields(text, FieldsOf(TypeNamed("LegendDto")));
        text.AppendLine().AppendLine("Colour bar fields (subPlots[].colorBar, and the figure's figureColorBar):");
        AppendFields(text, FieldsOf(TypeNamed("ColorBarDto")));
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
        text.AppendLine().AppendLine("Normalisers (the normalizer of a series drawn through a colour map):");
        AppendKinds(text, typeof(NormalizerDto));
        text.AppendLine().AppendLine("Colour maps built in code (a series' or a colour bar's colorMap; colours as names or #RRGGBB; positions rise from 0 to 1, one per colour; a reversed map holds the map it reverses, a named one the name a map is registered under):");
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

    private static void AppendFields(StringBuilder text, IReadOnlyList<WireField> fields)
    {
        foreach (var (name, type) in fields)
        {
            text.Append("  ").Append(name).Append(": ").Append(type);
            if (Notes.TryGetValue(name, out var note))
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

    /// <summary>The wire fields of a DTO record, in the camelCase the serializer writes and reads: its public properties
    /// and the internal ones it carries on the wire (<c>JsonInclude</c>).</summary>
    private static IReadOnlyList<WireField> FieldsOf(Type dto) =>
        [.. dto.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.GetMethod is { IsPublic: true } || p.IsDefined(typeof(JsonIncludeAttribute)))
            .Select(p => new WireField(JsonNamingPolicy.CamelCase.ConvertName(p.Name), WireTypeOf(p.PropertyType)))];

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
