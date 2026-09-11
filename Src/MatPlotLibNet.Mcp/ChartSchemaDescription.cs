// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text;
using System.Text.Json;
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
        ["xData"] = "x values; must be the same length as yData",
        ["yData"] = "y values; must be the same length as xData",
        ["color"] = "a CSS4 colour name (\"steelblue\") or #RRGGBB",
        ["label"] = "the name this series gets in the legend",
    };

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
        text.AppendLine($"Chart type '{seriesType}' — a series object inside subPlots[].series[].")
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
        text.AppendLine().AppendLine("Axis fields (subPlots[].xAxis / .yAxis):");
        AppendFields(text, FieldsOf(TypeNamed("AxisDto")));
        text.AppendLine().AppendLine($"Series fields (subPlots[].series[]) — {FieldsOf(typeof(SeriesDto)).Count} in total, each type uses the ones that apply:");
        AppendFields(text, FieldsOf(typeof(SeriesDto)).Take(24).ToArray());
        text.AppendLine("  … ask describe_chart_schema for one chart type to see the whole list.")
            .AppendLine().AppendLine("Example:").AppendLine(Example);
        return text.ToString().TrimEnd();
    }

    private static void AppendFields(StringBuilder text, IReadOnlyList<(string Name, string Type)> fields)
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

    /// <summary>The wire fields of a DTO record, in the camelCase the serializer writes and reads.</summary>
    private static IReadOnlyList<(string Name, string Type)> FieldsOf(Type dto) =>
        [.. dto.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => (Name: JsonNamingPolicy.CamelCase.ConvertName(p.Name), Type: WireTypeOf(p.PropertyType)))];

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
            "Color" => "string (colour)",
            _ => underlying.IsEnum ? "string (enum)" : "object",
        };
    }
}
