// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MatPlotLibNet.Mcp;

internal sealed partial class ChartSpecReader
{
    // The colour spelling of a model-authored document. The serializer's converter reads a hex of three, four, six or eight
    // digits through <c>Color.FromHex</c> and <c>"red"</c> would fail deep inside it without a path, so the spec reader turns a CSS4
    // name into its hex and checks every other colour's digits first, refusing a bad one with the path it stands at.

    /// <summary>The series field that holds another package's own data, which the colour walk leaves as written.</summary>
    private const string PackageDataField = "packageData";

    /// <summary>Every colour in the document, wherever it sits: a CSS4 name becomes the hex the serializer reads,
    /// a hex of three, four, six or eight digits is kept as written, and anything else is refused by path. A series' package
    /// data is another package's own and is left as written: a key in it is that package's (a map's features keep any
    /// properties their author gave them, a <c>color</c> among them).</summary>
    /// <param name="path">Where <paramref name="node"/> stands.</param>
    /// <param name="owner">The field <paramref name="node"/> is the value of: every field of a theme's alarm palette is a
    /// colour.</param>
    /// <exception cref="ToolRefusalException">A colour is neither a CSS4 name nor a hex of three, four, six or eight digits.</exception>
    /// <param name="node">The value it works on.</param>
    internal static void NormaliseColours(JsonNode? node, string path, string? owner = null)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var name in obj.Select(pair => pair.Key).Where(name => name != PackageDataField).ToArray())
                {
                    var child = obj[name];
                    string childPath = $"{path}.{name}";
                    if ((IsColourField(name) || owner == "alarm") && child is JsonValue value && value.TryGetValue<string>(out var written))
                    {
                        obj[name] = AsColour(written, childPath);
                    }
                    else if (IsColourListField(name) && child is JsonArray colours)
                    {
                        for (int i = 0; i < colours.Count; i++)
                        {
                            if (colours[i] is JsonValue item && item.TryGetValue<string>(out var entry))
                            {
                                colours[i] = AsColour(entry, string.Invariant($"{childPath}[{i}]"));
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
                    NormaliseColours(array[i], string.Invariant($"{path}[{i}]"));
                }
                break;
        }
    }

    /// <summary>A field that holds one colour: <c>color</c>, any name ending in <c>Color</c>, and the grounds of a theme.</summary>
    /// <param name="name">The <see cref="string"/> it works on.</param>
    internal static bool IsColourField(string name) =>
        name is "color" or "background" or "foregroundText" or "axesBackground" || name.EndsWith("Color", StringComparison.Ordinal);

    /// <summary>A field that holds a list of colours.</summary>
    /// <param name="name">The <see cref="string"/> it works on.</param>
    internal static bool IsColourListField(string name) => name is "stateSegmentColors" or "bandColors" or "colors" or "edgeColors" or "cycleColors";

    /// <summary>The hex the serializer reads for a colour written as a CSS4 name (its six digits) or as a hex of three, four, six or
    /// eight digits (the digits as written, with the '#').</summary>
    /// <param name="path">Where the colour stands, as a refusal names it.</param>
    /// <exception cref="ToolRefusalException">The colour is neither.</exception>
    /// <param name="written">The <see cref="string"/> it works on.</param>
    internal static string AsColour(string written, string path)
    {
        if (Color.TryFromName(written, out var named))
        {
            return named.ToHex();
        }

        string hex = written.StartsWith('#') ? written[1..] : written;
        if (hex.Length is 3 or 4 or 6 or 8 && HexDigits().IsMatch(hex))
        {
            return "#" + hex.ToUpperInvariant();
        }

        throw new ToolRefusalException($"'{written}' at {path} is not a colour the library knows: use a CSS4 colour name or #RRGGBB.");
    }

    [GeneratedRegex("^[0-9A-Fa-f]+$")]
    private static partial Regex HexDigits();
}
