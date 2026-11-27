// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MatPlotLibNet.Serialization;

/// <summary>How an enum-typed field of a document is read: by the one name rule of <see cref="EnumWireNames{T}"/>, so a member's
/// name in any case reads as that member and every other text (a number, an undefined value, two names joined by a comma, a name
/// with a space around it) is refused as an unknown name always was, with a <see cref="JsonException"/> that names the field. The
/// framework's string enum converter, which writes the field as the camel-case name of its member, would make a value of each of
/// those. Only the read options hold this converter (<see cref="ChartSerializer.ReadOptions"/>); the writer keeps the framework's.</summary>
internal sealed class WireEnumJsonConverter : JsonConverterFactory
{
    /// <summary>The longest written name, in UTF-8 bytes, that is matched without a string being made for it.</summary>
    private const int MaxNameOnStack = 128;

    /// <summary>The framework's converter, which writes the camel-case name; its reading is not used.</summary>
    private static readonly JsonStringEnumConverter Writer = new(JsonNamingPolicy.CamelCase);

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Of<>).MakeGenericType(typeToConvert), Writer.CreateConverter(typeToConvert, options))!;

    /// <summary>The converter of one enum.</summary>
    /// <param name="written">The framework's converter of the enum, which writes it.</param>
    private sealed class Of<T>(JsonConverter written) : JsonConverter<T> where T : struct, Enum
    {
        private readonly JsonConverter<T> _written = (JsonConverter<T>)written;

        /// <inheritdoc />
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                // The text's length in bytes bounds its length in characters; one of the two spans is empty.
                int length = checked((int)Math.Max(reader.ValueSpan.Length, reader.ValueSequence.Length));
                Span<char> text = length <= MaxNameOnStack ? stackalloc char[length] : new char[length];
                if (EnumWireNames<T>.TryMatch(text[..reader.CopyString(text)], out var member))
                {
                    return member;
                }
            }

            // No message: the serializer writes its own, with the path of the field.
            throw new JsonException();
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => _written.Write(writer, value, options);
    }
}
