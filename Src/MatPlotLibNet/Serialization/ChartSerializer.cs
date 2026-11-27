// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>Serializes and deserializes <see cref="Figure"/> instances to and from JSON.</summary>
/// <remarks>A number JSON cannot hold is written as the string that names it: NaN, the library's missing value, as
/// <c>"NaN"</c>, and the infinities as <c>"Infinity"</c> and <c>"-Infinity"</c>. Each is read back as that number, so a
/// figure with a gap in a line or an empty heatmap cell survives the round trip unchanged. A reader outside .NET sees
/// those three strings where a number would stand.</remarks>
public sealed class ChartSerializer : IChartSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = CreateOptions(true);
    private static readonly JsonSerializerOptions CompactOptions = CreateOptions(false);

    /// <summary>The options a document is read by: the writer's own naming, converters and number rule, with an enum-typed field
    /// read by <see cref="WireEnumJsonConverter"/> ahead of the writer's enum converter (a member's name in any case, nothing
    /// else). Another reader of the same documents (the MCP spec reader) starts from a copy, so they are one definition. The
    /// writer never meets the read converter, so what it writes and what writing costs stay the framework's.</summary>
    internal static JsonSerializerOptions ReadOptions { get; } = CreateReadOptions();

    /// <summary>What <see cref="FromJson"/> reads with: <see cref="ReadOptions"/> plus <see cref="DoubleArrayReadConverter"/>,
    /// which reads each list of numbers into an array of its exact length (a figure's read allocates about half of what
    /// System.Text.Json's growing lists made it allocate). An instance of its own, so the writer cannot meet the converter.</summary>
    private static readonly JsonSerializerOptions FromJsonOptions = new(ReadOptions) { Converters = { new DoubleArrayReadConverter() } };

    private static JsonSerializerOptions CreateOptions(bool indented) => new()
    {
        WriteIndented = indented,
#if NET9_0_OR_GREATER
        // The library's line end, not the operating system's: the indented document is the same bytes everywhere.
        NewLine = LineEndExtensions.LineEnd,
#endif
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        // The one place the non-finite numbers are decided (see the class remarks): written as "NaN", "Infinity" or
        // "-Infinity" and read back as themselves. Without it the writer throws on the first missing value. A finite
        // number is written exactly as before, so a figure without one keeps its bytes.
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Converters =
        {
            new ColorJsonConverter(),
            // An enum-typed field travels as the camel-case NAME of its member, never an ordinal; it is read back by the
            // converter ReadOptions puts ahead of this one.
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        },
    };

    /// <summary>A copy of the compact writer's options with <see cref="WireEnumJsonConverter"/> first, so it, not the writer's
    /// enum converter, reads every enum-typed field (the first converter that takes a type wins).</summary>
    private static JsonSerializerOptions CreateReadOptions()
    {
        var options = new JsonSerializerOptions(CompactOptions);
        options.Converters.Insert(0, new WireEnumJsonConverter());
        return options;
    }

    /// <inheritdoc />
    public string ToJson(Figure figure, bool indented = false)
    {
        var dto = FigureDto.Of(figure);
#if NET9_0_OR_GREATER
        return JsonSerializer.Serialize(dto, indented ? IndentedOptions : CompactOptions);
#else
        // The .NET 8 writer has no NewLine option and ends an indented line with the operating system's line end. A carriage
        // return inside a string is written escaped, so every one in the text is a line end's.
        string json = JsonSerializer.Serialize(dto, indented ? IndentedOptions : CompactOptions);
        return indented ? json.Replace("\r\n", LineEndExtensions.LineEnd, StringComparison.Ordinal) : json;
#endif
    }

    /// <inheritdoc />
    public Figure FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<FigureDto>(json, FromJsonOptions)
            ?? throw new JsonException("Failed to deserialize figure.");
        return dto.ToFigure();
    }
}
