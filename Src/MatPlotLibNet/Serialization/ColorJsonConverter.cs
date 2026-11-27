// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Serialization;

/// <summary>Converts <see cref="Color"/> values to and from hex strings (e.g., "#FF0000") during JSON serialization.</summary>
internal sealed class ColorJsonConverter : JsonConverter<Color>
{
    /// <inheritdoc />
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();
        return hex is not null ? Color.FromHex(hex) : default;
    }

    /// <inheritdoc />
    /// <remarks>An opaque colour is written <c>#RRGGBB</c>, as it always was; a colour with transparency
    /// <c>#RRGGBBAA</c>, so its alpha survives the round trip (a translucent bad colour of a colour map, a stop that fades out).</remarks>
    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.A == byte.MaxValue ? value.ToHex() : string.Invariant($"{value.ToHex()}{value.A:X2}"));
    }
}
