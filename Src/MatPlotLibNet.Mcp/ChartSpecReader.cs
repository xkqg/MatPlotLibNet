// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Mcp;

/// <summary>The one door between a model-authored document and the library's serializer. <c>ChartSerializer.FromJson</c>
/// is a round-trip reader for its own writer: it drops an unknown series type leniently, skips an unknown property,
/// ignores a misspelled enum value and overwrites the figure defaults with zero when a size is absent. Handed to a
/// model every one of those turns a typo into a blank picture reported as success. So the reader refuses first, by
/// field and path, and only a document it has normalised reaches <see cref="IChartSerializer.FromJson"/>.</summary>
/// <remarks>Order: parse → normalise (defaults, colours, enum spelling, bounds, text lengths) → strict structural pass
/// over the SAME DTO records the serializer reads (an unknown field is a refusal with its path) → semantic pass
/// (types against the catalog, array lengths) → the serializer.</remarks>
internal sealed partial class ChartSpecReader
{
    private const double DefaultWidth = 800;
    private const double DefaultHeight = 600;
    private const double DefaultDpi = Figure.ReferenceDpi;

    /// <summary>A copy of the serializer's own read options (naming, converters, the number rule that reads "NaN" as
    /// the missing value), plus the one thing it deliberately lacks: a refusal of unmapped members. Its lenience is wire
    /// compatibility for its other consumers; here a document is authored by hand and an unknown field is a mistake to
    /// report.</summary>
    private static readonly JsonSerializerOptions Strict = new(ChartSerializer.ReadOptions)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>The types whose JSON carries y values alone: a signal is sampled at a rate (<c>signalSampleRate</c>,
    /// <c>signalXStart</c>), so its x values follow from the rate and the library never writes them. Every other
    /// x/y type needs both halves of the pair.</summary>
    private static readonly HashSet<string> DerivesItsXData = new(StringComparer.Ordinal) { "signal" };

    private readonly ChartTypeCatalog _catalog;
    private readonly RenderLimits _limits;
    private readonly SpecVocabulary _vocabulary = new();

    /// <summary>Creates a reader that accepts the types <paramref name="catalog"/> lists within <paramref name="limits"/>.</summary>
    public ChartSpecReader(ChartTypeCatalog catalog, RenderLimits limits)
    {
        _catalog = catalog;
        _limits = limits;
    }

    /// <summary>The <see cref="Figure"/> the spec describes, or a <see cref="ToolRefusalException"/> naming what is wrong.</summary>
    public Figure Read(ChartSpec spec)
    {
        JsonObject root = ParseObject(spec.Json);
        Normalise(root);
        FigureDto dto = DeserializeStrictly(root);
        Validate(dto);
        return Deserialize(root.ToJsonString());
    }

    private static JsonObject ParseObject(string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ToolRefusalException($"The spec is not valid JSON: {ex.Message}");
        }

        return node as JsonObject
            ?? throw new ToolRefusalException($"The spec must be a JSON object ({{...}}), not {node?.GetValueKind().ToString().ToLowerInvariant() ?? "null"}.");
    }

    // ---- normalisation: defaults, bounds, text, colours, enum spelling ------------------------------------------

    private void Normalise(JsonObject figure)
    {
        EnsureSize(figure, "width", DefaultWidth, _limits.MaxWidth);
        EnsureSize(figure, "height", DefaultHeight, _limits.MaxHeight);
        EnsureSize(figure, "dpi", DefaultDpi, int.MaxValue);
        EnsurePictureFits(figure);
        CheckText(figure, "$", "title");
        CheckText(figure, "$", "altText");
        CheckText(figure, "$", "description");
        CheckText(figure, "$", "chartId");
        NormaliseColours(figure, "$");
        NormaliseColorBar(figure, "figureColorBar", "$");
        NormaliseTheme(figure, "$");
        ForEachObject(figure, "subPlots", "$", NormaliseAxes);
    }

    /// <summary>The figure's theme: the enum values of its font and its grid, and the line styles and markers its cycler
    /// steps through. Its colours are normalised with every other colour of the document.</summary>
    private void NormaliseTheme(JsonObject figure, string path)
    {
        if (figure["theme"] is not JsonObject theme)
        {
            return;
        }

        string at = $"{path}.theme";
        NormaliseFields(theme, "defaultFont", at, SpecNodeKind.Font, "weight", "slant");
        NormaliseFields(theme, "defaultGrid", at, SpecNodeKind.Grid, "lineStyle", "which", "axis");
        if (theme["propCycler"] is JsonObject cycler)
        {
            NormaliseEnum(cycler, SpecNodeKind.PropCycler, "lineStyles", $"{at}.propCycler", seriesType: null);
            NormaliseEnum(cycler, SpecNodeKind.PropCycler, "markerStyles", $"{at}.propCycler", seriesType: null);
        }
    }

    private void NormaliseAxes(JsonObject axes, string path)
    {
        CheckText(axes, path, "title");
        foreach (var axisName in new[] { "xAxis", "yAxis", "secondaryYAxis", "secondaryXAxis" })
        {
            if (axes[axisName] is JsonObject axis)
            {
                NormaliseAxis(axis, $"{path}.{axisName}");
            }
        }

        NormaliseEnum(axes, SpecNodeKind.Axes, "barMode", path, seriesType: null);
        NormaliseEnum(axes, SpecNodeKind.Axes, "titleLoc", path, seriesType: null);
        NormaliseFields(axes, "titleStyle", path, SpecNodeKind.TextStyle, "fontWeight", "fontSlant");
        NormaliseFields(axes, "legend", path, SpecNodeKind.Legend, "position");
        NormaliseColorBar(axes, "colorBar", path);
        NormaliseFields(axes, "grid", path, SpecNodeKind.Grid, "lineStyle", "which", "axis");
        if (axes["spines"] is JsonObject spines)
        {
            foreach (var side in new[] { "top", "bottom", "left", "right" })
            {
                NormaliseFields(spines, side, $"{path}.spines", SpecNodeKind.Spine, "lineStyle");
            }
        }

        ForEachObject(axes, "series", path, NormaliseSeries);
        ForEachObject(axes, "secondarySeries", path, NormaliseSeries);
        ForEachObject(axes, "xSecondarySeries", path, NormaliseSeries);
        ForEachObject(axes, "annotations", path, (annotation, p) =>
        {
            CheckText(annotation, p, "text");
            NormaliseEnum(annotation, SpecNodeKind.Annotation, "connectionStyle", p, null);
            NormaliseEnum(annotation, SpecNodeKind.Annotation, "boxStyle", p, null);
            NormaliseEnum(annotation, SpecNodeKind.Annotation, "coordinates", p, null);
            NormaliseEnum(annotation, SpecNodeKind.Annotation, "alignment", p, null);
            NormaliseEnum(annotation, SpecNodeKind.Annotation, "arrowStyle", p, null);
            NormaliseFields(annotation, "font", p, SpecNodeKind.Font, "weight", "slant");
        });
        ForEachObject(axes, "referenceLines", path, (line, p) =>
        {
            CheckText(line, p, "label");
            NormaliseEnum(line, SpecNodeKind.ReferenceLine, "orientation", p, null);
            NormaliseEnum(line, SpecNodeKind.ReferenceLine, "lineStyle", p, null);
        });
        ForEachObject(axes, "spans", path, (span, p) =>
        {
            CheckText(span, p, "label");
            NormaliseEnum(span, SpecNodeKind.Span, "orientation", p, null);
            NormaliseEnum(span, SpecNodeKind.Span, "lineStyle", p, null);
        });
        ForEachObject(axes, "trendlines", path, (line, p) => NormaliseEnum(line, SpecNodeKind.Trendline, "lineStyle", p, null));
        ForEachObject(axes, "horizontalLevels", path, (level, p) => NormaliseEnum(level, SpecNodeKind.HorizontalLevel, "lineStyle", p, null));
        ForEachObject(axes, "xBreaks", path, (axisBreak, p) => NormaliseEnum(axisBreak, SpecNodeKind.AxisBreak, "style", p, null));
        ForEachObject(axes, "yBreaks", path, (axisBreak, p) => NormaliseEnum(axisBreak, SpecNodeKind.AxisBreak, "style", p, null));
        ForEachObject(axes, "insets", path, NormaliseAxes);
    }

    private void NormaliseSeries(JsonObject series, string path)
    {
        CheckText(series, path, "label");
        string? type = series["type"] is JsonValue value && value.TryGetValue<string>(out var typed) ? typed : null;
        foreach (var field in _vocabulary.SeriesFields)
        {
            NormaliseEnum(series, SpecNodeKind.Series, field, path, type);
        }

        NormaliseKind(series, "normalizer", path, SpecNodeKind.Normalizer);
        NormaliseColorMap(series, "colorMap", path);
        NormaliseColorMap(series, "pairGridOffDiagonalColorMapDescription", path);
    }

    /// <summary>A colour map built in code, described by kind: its kind is checked and moved first as a normaliser's is,
    /// and so is the kind of the map a reversed map reverses. Its colours are normalised with every other colour of the
    /// document.</summary>
    private void NormaliseColorMap(JsonObject parent, string field, string path)
    {
        NormaliseKind(parent, field, path, SpecNodeKind.ColorMap);
        if (parent[field] is JsonObject map)
        {
            NormaliseColorMap(map, "map", $"{path}.{field}");
        }
    }

    /// <summary>One axis: its label, its scale, its ticks, its label style, and the kinds of its tick locator and formatter.</summary>
    private void NormaliseAxis(JsonObject axis, string path)
    {
        CheckText(axis, path, "label");
        NormaliseEnum(axis, SpecNodeKind.Axis, "scale", path, seriesType: null);
        NormaliseFields(axis, "majorTicks", path, SpecNodeKind.Ticks, "direction");
        NormaliseFields(axis, "minorTicks", path, SpecNodeKind.Ticks, "direction");
        NormaliseFields(axis, "labelStyle", path, SpecNodeKind.TextStyle, "fontWeight", "fontSlant");
        NormaliseKind(axis, "tickLocator", path, SpecNodeKind.TickLocator);
        NormaliseKind(axis, "tickFormatter", path, SpecNodeKind.TickFormatter);
    }

    private void NormaliseColorBar(JsonObject parent, string field, string path)
    {
        NormaliseFields(parent, field, path, SpecNodeKind.ColorBar, "extend", "orientation");
        if (parent[field] is JsonObject colorBar)
        {
            NormaliseColorMap(colorBar, "colorMap", $"{path}.{field}");
        }
    }

    /// <summary>The enum-valued <paramref name="fields"/> of the object at <paramref name="field"/>, when there is one.</summary>
    private void NormaliseFields(JsonObject parent, string field, string path, SpecNodeKind kind, params string[] fields)
    {
        if (parent[field] is not JsonObject node)
        {
            return;
        }

        foreach (var name in fields)
        {
            NormaliseEnum(node, kind, name, $"{path}.{field}", seriesType: null);
        }
    }

    /// <summary>A described value (a tick locator, a tick formatter, a normaliser): its <c>type</c> must name a kind the
    /// serializer knows, spelt as it reads it, and stand first, since the serializer reads the kind only as the first
    /// field of the object. A value without a <c>type</c> is refused: the serializer would skip it.</summary>
    private void NormaliseKind(JsonObject parent, string field, string path, SpecNodeKind kind)
    {
        if (parent[field] is not JsonObject node)
        {
            return;
        }

        string where = $"{path}.{field}";
        var rule = _vocabulary.RuleFor(kind, "type", seriesType: null)!;
        if (node["type"] is null)
        {
            throw new ToolRefusalException($"The {field} at {where} has no 'type'; accepted: {string.Join(", ", rule.Accepted)}.");
        }

        NormaliseEnum(node, kind, "type", where, seriesType: null);
        var fields = node.ToList();
        node.Clear();
        node["type"] = fields.Single(pair => pair.Key == "type").Value;
        foreach (var pair in fields.Where(pair => pair.Key != "type"))
        {
            node[pair.Key] = pair.Value;
        }
    }

    /// <summary>Visits every object element of the array at <paramref name="field"/>; a value of another shape is
    /// left for the strict pass, which names it.</summary>
    private static void ForEachObject(JsonObject parent, string field, string path, Action<JsonObject, string> visit)
    {
        if (parent[field] is not JsonArray items)
        {
            return;
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is JsonObject item)
            {
                visit(item, $"{path}.{field}[{i}]");
            }
        }
    }

    private void EnsureSize(JsonObject figure, string field, double fallback, int max)
    {
        var node = figure[field];
        if (node is null)
        {
            figure[field] = fallback;
            return;
        }

        if (node is JsonValue value && value.TryGetValue<double>(out var size) && !(size >= 1 && size <= max))
        {
            throw new ToolRefusalException(
                $"'{field}' must be between 1 and {max} (got {size.ToString(CultureInfo.InvariantCulture)}): the picture goes into a context window.");
        }
    }

    /// <summary>A PNG is drawn with <c>dpi / 96</c> pixels for each unit of the figure, so the canvas ceiling holds for
    /// the picture the spec draws, not for its width and height alone: a figure inside the ceiling at 96 dpi can leave it
    /// at 300, and one at 100000 dpi is a bitmap no machine allocates. The picture goes into a context window, so the
    /// ceiling is the picture's, in pixels.</summary>
    private void EnsurePictureFits(JsonObject figure)
    {
        if (!TryNumber(figure, "width", out double width)
            || !TryNumber(figure, "height", out double height)
            || !TryNumber(figure, "dpi", out double dpi))
        {
            return;
        }

        double pixelsAcross = width * dpi / Figure.ReferenceDpi;
        double pixelsDown = height * dpi / Figure.ReferenceDpi;
        if (pixelsAcross > _limits.MaxWidth || pixelsDown > _limits.MaxHeight)
        {
            throw new ToolRefusalException(
                $"'dpi' {dpi.ToString(CultureInfo.InvariantCulture)} draws the picture {Math.Ceiling(pixelsAcross).ToString(CultureInfo.InvariantCulture)} by "
                + $"{Math.Ceiling(pixelsDown).ToString(CultureInfo.InvariantCulture)} pixels (width and height times dpi / {Figure.ReferenceDpi}); "
                + $"the ceiling is {_limits.MaxWidth} by {_limits.MaxHeight}: the picture goes into a context window. Lower the dpi or the size.");
        }
    }

    private static bool TryNumber(JsonObject node, string field, out double value)
    {
        value = 0;
        return node[field] is JsonValue json && json.TryGetValue(out value);
    }

    private void CheckText(JsonObject node, string path, string field)
    {
        if (node[field] is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > _limits.MaxTextLength)
        {
            throw new ToolRefusalException($"'{field}' at {path} is {text.Length} characters long; the limit is {_limits.MaxTextLength}.");
        }
    }

    /// <summary>The enum value at <paramref name="field"/>, or each item of the list of enum values there: checked against
    /// the field's vocabulary and spelt as the serializer reads it. A field that is not enum-valued, or holds another
    /// kind of value, is left for the strict pass.</summary>
    private void NormaliseEnum(JsonObject node, SpecNodeKind kind, string field, string path, string? seriesType)
    {
        var rule = _vocabulary.RuleFor(kind, field, seriesType);
        if (rule is null)
        {
            return;
        }

        switch (node[field])
        {
            case JsonValue value when value.TryGetValue<string>(out var written):
                node[field] = CanonicalOrRefuse(rule, written, $"{path}.{field}");
                break;
            case JsonArray items:
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] is JsonValue item && item.TryGetValue<string>(out var entry))
                    {
                        items[i] = CanonicalOrRefuse(rule, entry, $"{path}.{field}[{i}]");
                    }
                }
                break;
        }
    }

    /// <summary>The spelling <paramref name="rule"/> accepts for <paramref name="written"/>, or the refusal that names
    /// where it stands and what is accepted there.</summary>
    private static string CanonicalOrRefuse(SpecVocabulary.Rule rule, string written, string where) =>
        rule.Canonical(written)
        ?? throw new ToolRefusalException($"'{written}' at {where} is not an accepted value; accepted: {string.Join(", ", rule.Accepted)}.");

    /// <summary>Every colour in the document, wherever it sits: a CSS4 name becomes the hex the serializer reads,
    /// a short hex is expanded, and anything else is refused by path. The serializer's converter accepts six or
    /// eight hex digits only, and <c>"red"</c> would otherwise fail deep inside it without a path.</summary>
    /// <param name="node">The node to walk.</param>
    /// <param name="path">Where <paramref name="node"/> stands.</param>
    /// <param name="owner">The field <paramref name="node"/> is the value of: every field of a theme's alarm palette is a
    /// colour.</param>
    private static void NormaliseColours(JsonNode? node, string path, string? owner = null)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var name in obj.Select(pair => pair.Key).ToArray())
                {
                    var child = obj[name];
                    string childPath = $"{path}.{name}";
                    if ((IsColourField(name) || owner == "alarm") && child is JsonValue value && value.TryGetValue<string>(out var written))
                    {
                        obj[name] = NormaliseColour(written, childPath);
                    }
                    else if (IsColourListField(name) && child is JsonArray colours)
                    {
                        for (int i = 0; i < colours.Count; i++)
                        {
                            if (colours[i] is JsonValue item && item.TryGetValue<string>(out var entry))
                            {
                                colours[i] = NormaliseColour(entry, $"{childPath}[{i}]");
                            }
                        }
                    }
                    else
                    {
                        NormaliseColours(child, childPath, owner: name);
                    }
                }
                break;
            case JsonArray array:
                for (int i = 0; i < array.Count; i++)
                {
                    NormaliseColours(array[i], $"{path}[{i}]");
                }
                break;
        }
    }

    /// <summary>A field that holds one colour: <c>color</c>, any name ending in <c>Color</c>, and the grounds of a theme.</summary>
    private static bool IsColourField(string name) =>
        name is "color" or "background" or "foregroundText" or "axesBackground" || name.EndsWith("Color", StringComparison.Ordinal);

    private static bool IsColourListField(string name) => name is "stateSegmentColors" or "bandColors" or "colors" or "edgeColors" or "cycleColors";

    private static string NormaliseColour(string written, string path)
    {
        if (Color.TryFromName(written, out var named))
        {
            return named.ToHex();
        }

        string hex = written.StartsWith('#') ? written[1..] : written;
        if (HexDigits().IsMatch(hex))
        {
            if (hex.Length is 3 or 4)
            {
                hex = string.Concat(hex.Select(digit => new string(digit, 2)));
            }

            if (hex.Length is 6 or 8)
            {
                return "#" + hex.ToUpperInvariant();
            }
        }

        throw new ToolRefusalException($"'{written}' at {path} is not a colour the library knows: use a CSS4 colour name or #RRGGBB.");
    }

    [GeneratedRegex("^[0-9A-Fa-f]+$")]
    private static partial Regex HexDigits();

    // ---- the strict structural pass -----------------------------------------------------------------------------

    private static FigureDto DeserializeStrictly(JsonObject root)
    {
        try
        {
            return JsonSerializer.Deserialize<FigureDto>(root.ToJsonString(), Strict)
                ?? throw new ToolRefusalException("The spec is empty.");
        }
        catch (JsonException ex)
        {
            throw new ToolRefusalException(Describe(ex));
        }
    }

    private static string Describe(JsonException ex)
    {
        var unmapped = UnmappedProperty().Match(ex.Message);
        string where = ex.Path ?? "$";
        return unmapped.Success
            ? $"Unknown field '{unmapped.Groups[1].Value}' at {where}. describe_chart_schema lists the fields a spec accepts."
            : $"The field at {where} has the wrong shape: {ex.Message}";
    }

    [GeneratedRegex("property '([^']+)'")]
    private static partial Regex UnmappedProperty();

    // ---- the semantic pass ----------------------------------------------------------------------------------------

    private void Validate(FigureDto dto)
    {
        int drawn = 0;
        var subPlots = dto.SubPlots ?? [];
        for (int i = 0; i < subPlots.Count; i++)
        {
            drawn += ValidateAxes(subPlots[i], $"$.subPlots[{i}]");
        }

        if (drawn == 0)
        {
            throw new ToolRefusalException("Nothing to draw: the spec has no series. Put at least one series with a 'type' in subPlots[].series[].");
        }
    }

    private int ValidateAxes(AxesDto axes, string path)
    {
        int drawn = 0;
        var series = axes.Series ?? [];
        for (int j = 0; j < series.Count; j++)
        {
            ValidateSeries(series[j], $"{path}.series[{j}]", secondary: false);
            drawn++;
        }

        var secondary = axes.SecondarySeries ?? [];
        for (int j = 0; j < secondary.Count; j++)
        {
            ValidateSeries(secondary[j], $"{path}.secondarySeries[{j}]", secondary: true);
            drawn++;
        }

        var insets = axes.Insets ?? [];
        for (int k = 0; k < insets.Count; k++)
        {
            drawn += ValidateAxes(insets[k], $"{path}.insets[{k}]");
        }

        return drawn;
    }

    private void ValidateSeries(SeriesDto series, string path, bool secondary)
    {
        if (string.IsNullOrEmpty(series.Type))
        {
            throw new ToolRefusalException($"The series at {path} has no 'type'. list_chart_types names the accepted types.");
        }

        if (secondary)
        {
            // The serializer routes secondary-axis series through a switch that ends in "_ => null" without a
            // diagnostic: a bar on the secondary axis vanishes without a word.
            if (series.Type is not ("line" or "scatter"))
            {
                throw new ToolRefusalException($"'{series.Type}' at {path}.type is not supported on the secondary axis: only 'line' and 'scatter' are.");
            }
        }
        else if (!_catalog.IsListed(series.Type))
        {
            if (_catalog.Excluded.TryGetValue(series.Type, out var reason))
            {
                throw new ToolRefusalException($"'{series.Type}' at {path}.type cannot be built from JSON in this version: {reason}.");
            }

            string hint = _catalog.Suggest(series.Type) is { } meant ? $" — did you mean '{meant}'?" : "";
            throw new ToolRefusalException($"'{series.Type}' at {path}.type is not a chart type this server lists{hint} list_chart_types names the accepted types.");
        }

        // The serializer substitutes an empty array for a missing half of the pair and then throws
        // "Array lengths must match. Got 3 and 0." with no field name and no path.
        bool hasX = series.XData is not null;
        bool hasY = series.YData is not null;
        if (hasX != hasY && !DerivesItsXData.Contains(series.Type))
        {
            throw new ToolRefusalException($"The series at {path} has '{(hasX ? "xData" : "yData")}' but no '{(hasX ? "yData" : "xData")}'.");
        }

        if (hasX && series.XData!.Length != series.YData!.Length)
        {
            throw new ToolRefusalException(
                $"The series at {path} has {series.XData.Length} 'xData' values and {series.YData.Length} 'yData' values; they must match.");
        }
    }

    // ---- the serializer -------------------------------------------------------------------------------------------

    private static Figure Deserialize(string json)
    {
        try
        {
            return ChartSerializerDefault.Current.FromJson(json);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException or JsonException)
        {
            throw new ToolRefusalException($"The library refused the figure: {ex.Message}");
        }
    }
}
