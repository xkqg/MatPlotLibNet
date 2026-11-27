// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MatPlotLibNet.Serialization;

/// <summary>Reads a JSON list of numbers into a <c>double[]</c> of exactly its length. System.Text.Json's own reader for a
/// <c>double[]</c> fills a list that grows by doubling and then copies it out, and that growth was most of what reading a
/// figure allocated; here the items are read into one pooled buffer and copied once into the array the figure keeps.</summary>
/// <remarks>It reads what System.Text.Json reads, bit for bit: a number token through <see cref="Utf8JsonReader.GetDouble"/>,
/// and the strings "NaN", "Infinity" and "-Infinity" only when the options' <see cref="JsonSerializerOptions.NumberHandling"/>
/// allows the named literals (the serializer's one number rule). Anything else is refused as a <see cref="JsonException"/>,
/// whose path names the list but not the item: only <see cref="ChartSerializer.FromJson"/> reads with it, while the writer and
/// the MCP spec reader (which names the index of an item that is not a number) keep System.Text.Json's own list handling.</remarks>
internal sealed class DoubleArrayReadConverter : JsonConverter<double[]>
{
    /// <summary>The buffer a read starts with; a longer list moves to a buffer twice as long, so a typical line is read
    /// without moving at all.</summary>
    private const int StartLength = 1024;

    /// <inheritdoc />
    public override double[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();

        bool named = (options.NumberHandling & JsonNumberHandling.AllowNamedFloatingPointLiterals) != 0;
        double[] buffer = ArrayPool<double>.Shared.Rent(StartLength);
        int count = 0;
        try
        {
            for (reader.Read(); reader.TokenType != JsonTokenType.EndArray; reader.Read())
            {
                if (count == buffer.Length) buffer = Grown(buffer);
                buffer[count++] = Item(ref reader, named);
            }
            return buffer.AsSpan(0, count).ToArray();
        }
        finally
        {
            ArrayPool<double>.Shared.Return(buffer);
        }
    }

    /// <summary>Refused: this converter only reads. The options it sits in are never handed to the writer, so a written figure
    /// keeps System.Text.Json's own bytes.</summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public override void Write(Utf8JsonWriter writer, double[] value, JsonSerializerOptions options) =>
        throw new NotSupportedException($"{nameof(DoubleArrayReadConverter)} only reads; the writer writes a list of numbers itself.");

    /// <summary>One item of the list, as System.Text.Json reads a <c>double</c> under the same number rule.</summary>
    private static double Item(ref Utf8JsonReader reader, bool named) => reader.TokenType switch
    {
        JsonTokenType.Number => reader.GetDouble(),
        JsonTokenType.String when named && reader.ValueTextEquals("NaN"u8) => double.NaN,
        JsonTokenType.String when named && reader.ValueTextEquals("Infinity"u8) => double.PositiveInfinity,
        JsonTokenType.String when named && reader.ValueTextEquals("-Infinity"u8) => double.NegativeInfinity,
        _ => throw new JsonException(),
    };

    /// <summary>A pooled buffer twice as long holding what <paramref name="full"/> held; <paramref name="full"/> goes back to
    /// the pool.</summary>
    private static double[] Grown(double[] full)
    {
        double[] larger = ArrayPool<double>.Shared.Rent(full.Length * 2);
        full.CopyTo(larger, 0);
        ArrayPool<double>.Shared.Return(full);
        return larger;
    }
}
