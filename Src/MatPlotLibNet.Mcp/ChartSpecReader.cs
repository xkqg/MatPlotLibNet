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
    private const double DefaultDpi = 96;

    /// <summary>The serializer's own naming and converters, plus the one thing it deliberately lacks: a refusal of
    /// unmapped members. Its lenience is wire compatibility for its other consumers; here a document is authored by
    /// hand and an unknown field is a mistake to report.</summary>
    private static readonly JsonSerializerOptions Strict = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new ColorJsonConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

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
        CheckText(figure, "$", "title");
        CheckText(figure, "$", "altText");
        CheckText(figure, "$", "description");
        NormaliseColours(figure, "$");
        ForEachObject(figure, "subPlots", "$", NormaliseAxes);
    }

    private void NormaliseAxes(JsonObject axes, string path)
    {
        CheckText(axes, path, "title");
        foreach (var axisName in new[] { "xAxis", "yAxis", "secondaryYAxis" })
        {
            if (axes[axisName] is JsonObject axis)
            {
                CheckText(axis, $"{path}.{axisName}", "label");
                NormaliseEnum(axis, SpecNodeKind.Axis, "scale", $"{path}.{axisName}", seriesType: null);
            }
        }

        NormaliseEnum(axes, SpecNodeKind.Axes, "barMode", path, seriesType: null);
        ForEachObject(axes, "series", path, NormaliseSeries);
        ForEachObject(axes, "secondarySeries", path, NormaliseSeries);
        ForEachObject(axes, "annotations", path, (annotation, p) =>
        {
            CheckText(annotation, p, "text");
            NormaliseEnum(annotation, SpecNodeKind.Annotation, "connectionStyle", p, null);
            NormaliseEnum(annotation, SpecNodeKind.Annotation, "boxStyle", p, null);
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

    private void CheckText(JsonObject node, string path, string field)
    {
        if (node[field] is JsonValue value && value.TryGetValue<string>(out var text) && text.Length > _limits.MaxTextLength)
        {
            throw new ToolRefusalException($"'{field}' at {path} is {text.Length} characters long; the limit is {_limits.MaxTextLength}.");
        }
    }

    private void NormaliseEnum(JsonObject node, SpecNodeKind kind, string field, string path, string? seriesType)
    {
        if (node[field] is not JsonValue value || !value.TryGetValue<string>(out var written))
        {
            return;
        }

        var rule = _vocabulary.RuleFor(kind, field, seriesType);
        if (rule is null)
        {
            return;
        }

        node[field] = rule.Canonical(written)
            ?? throw new ToolRefusalException($"'{written}' at {path}.{field} is not an accepted value; accepted: {string.Join(", ", rule.Accepted)}.");
    }

    /// <summary>Every colour in the document, wherever it sits: a CSS4 name becomes the hex the serializer reads,
    /// a short hex is expanded, and anything else is refused by path. The serializer's converter accepts six or
    /// eight hex digits only, and <c>"red"</c> would otherwise fail deep inside it without a path.</summary>
    private static void NormaliseColours(JsonNode? node, string path)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var name in obj.Select(pair => pair.Key).ToArray())
                {
                    var child = obj[name];
                    string childPath = $"{path}.{name}";
                    if (IsColourField(name) && child is JsonValue value && value.TryGetValue<string>(out var written))
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
                        NormaliseColours(child, childPath);
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

    private static bool IsColourField(string name) => name == "color" || name.EndsWith("Color", StringComparison.Ordinal);

    private static bool IsColourListField(string name) => name is "stateSegmentColors" or "bandColors";

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
        if (hasX != hasY)
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
            return ChartServices.Serializer.FromJson(json);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or FormatException or JsonException)
        {
            throw new ToolRefusalException($"The library refused the figure: {ex.Message}");
        }
    }
}
