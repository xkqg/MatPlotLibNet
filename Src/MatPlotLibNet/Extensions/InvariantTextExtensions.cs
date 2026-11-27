// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace MatPlotLibNet;

/// <summary>A number's text, and the number a text holds, the same on every machine: a point for the decimals and no grouping or
/// sign of the reader's culture. The one place the library names the invariant culture for a number, which every document (SVG, JSON,
/// a data attribute a script reads), every label, every data table and every message reads: a label reads the same in every culture,
/// and a document written on one machine reads back on another. A number is written in the shortest form that reads back to the same
/// number (the general form) or in the format its site names; a text with numbers in it is composed by <c>string.Invariant($"...")</c>,
/// each hole in the format it names.</summary>
internal static class InvariantTextExtensions
{
    extension<T>(T value) where T : IFormattable
    {
        /// <summary>The value as text in the invariant culture: in the shortest form that reads back to it, or in
        /// <paramref name="format"/>.</summary>
        /// <param name="format">A .NET format (<c>0.##</c>, <c>G6</c>, <c>yyyy-MM-dd</c>), or <see langword="null"/> for the shortest form.</param>
        /// <returns>The text of the value.</returns>
        internal string InvariantText(string? format = null) => value.ToString(format, CultureInfo.InvariantCulture);
    }

    extension<T>(T value) where T : ISpanFormattable
    {
        /// <summary>Writes the text <see cref="InvariantText"/> returns into <paramref name="into"/>, for a writer that puts the number
        /// into a buffer of its own and makes no string of it.</summary>
        /// <param name="into">Room for the text: 32 characters hold any double in the general form.</param>
        /// <param name="format">A .NET format, or empty for the shortest form.</param>
        /// <returns>How many characters were written.</returns>
        internal int FormatInvariant(Span<char> into, ReadOnlySpan<char> format = default)
        {
            value.TryFormat(into, out int written, format, CultureInfo.InvariantCulture);
            return written;
        }
    }

    extension(System.Text.StringBuilder text)
    {
        /// <summary>Appends the text <see cref="InvariantText"/> returns for <paramref name="value"/>, with no string of its own: for a
        /// writer that builds its document in a builder.</summary>
        /// <param name="value">The value.</param>
        /// <param name="format">A .NET format, or empty for the shortest form.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <returns>The builder.</returns>
        internal System.Text.StringBuilder AppendInvariant<T>(T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
        {
            Span<char> digits = stackalloc char[64];
            return text.Append(digits[..value.FormatInvariant(digits, format)]);
        }
    }

    extension(string)
    {
        /// <summary>The interpolated text with every number, date and time in it written in the invariant culture, each in the
        /// format its hole names (<c>{width:0.#}</c>) or in the shortest form.</summary>
        /// <param name="text">The interpolated text.</param>
        /// <returns>The text.</returns>
        internal static string Invariant(ref InvariantInterpolation text) => text.ToStringAndClear();
    }

    extension(string composite)
    {
        /// <summary>The composite format (<c>{0:F1}%</c>) with <paramref name="argument"/> in its hole, in the invariant culture.</summary>
        /// <param name="argument">The value of the hole.</param>
        /// <returns>The text.</returns>
        internal string FormatInvariant(object? argument) => string.Format(CultureInfo.InvariantCulture, composite, argument);
    }

    extension(ReadOnlySpan<char> text)
    {
        /// <summary>The number the text holds, read in the invariant culture.</summary>
        /// <param name="styles">The forms the text may take (<see cref="NumberStyles.Float"/> for a document's number).</param>
        /// <param name="value">The number, or the default when the text holds none.</param>
        /// <typeparam name="T">The kind of number.</typeparam>
        /// <returns>Whether the text holds a number in <paramref name="styles"/>.</returns>
        internal bool TryReadInvariant<T>(NumberStyles styles, [MaybeNullWhen(false)] out T value) where T : INumberBase<T> =>
            T.TryParse(text, styles, CultureInfo.InvariantCulture, out value);

        /// <summary>The number the text holds, read in the invariant culture.</summary>
        /// <param name="styles">The forms the text may take.</param>
        /// <typeparam name="T">The kind of number.</typeparam>
        /// <returns>The number.</returns>
        /// <exception cref="FormatException">The text holds no number in <paramref name="styles"/>.</exception>
        /// <exception cref="OverflowException">The number is past what <typeparamref name="T"/> holds.</exception>
        internal T ReadInvariant<T>(NumberStyles styles) where T : INumberBase<T> => T.Parse(text, styles, CultureInfo.InvariantCulture);
    }

    /// <summary>The holes of an interpolated text, written in the invariant culture: what <see cref="InvariantTextExtensions"/> composes
    /// with <c>string.Invariant($"...")</c>. The compiler builds it from the text; nothing else does.</summary>
    [InterpolatedStringHandler]
    internal ref struct InvariantInterpolation
    {
        private DefaultInterpolatedStringHandler _text;

        /// <summary>Room for an interpolated text of <paramref name="literalLength"/> literal characters and
        /// <paramref name="formattedCount"/> holes.</summary>
        /// <param name="literalLength">The number of literal characters.</param>
        /// <param name="formattedCount">The number of holes.</param>
        public InvariantInterpolation(int literalLength, int formattedCount) =>
            _text = new DefaultInterpolatedStringHandler(literalLength, formattedCount, CultureInfo.InvariantCulture);

        /// <summary>Writes a literal part of the text.</summary>
        /// <param name="value">The literal.</param>
        public void AppendLiteral(string value) => _text.AppendLiteral(value);

        /// <summary>Writes a hole in the shortest form.</summary>
        /// <param name="value">The value of the hole.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        public void AppendFormatted<T>(T value) => _text.AppendFormatted(value);

        /// <summary>Writes a hole in the format it names.</summary>
        /// <param name="value">The value of the hole.</param>
        /// <param name="format">The format.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        public void AppendFormatted<T>(T value, string? format) => _text.AppendFormatted(value, format);

        /// <summary>Writes a hole aligned in <paramref name="alignment"/> characters.</summary>
        /// <param name="value">The value of the hole.</param>
        /// <param name="alignment">The width; negative aligns to the left.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        public void AppendFormatted<T>(T value, int alignment) => _text.AppendFormatted(value, alignment);

        /// <summary>Writes a hole in the format it names, aligned in <paramref name="alignment"/> characters.</summary>
        /// <param name="value">The value of the hole.</param>
        /// <param name="alignment">The width; negative aligns to the left.</param>
        /// <param name="format">The format.</param>
        /// <typeparam name="T">The type of the value.</typeparam>
        public void AppendFormatted<T>(T value, int alignment, string? format) => _text.AppendFormatted(value, alignment, format);

        /// <summary>Writes a hole of characters.</summary>
        /// <param name="value">The characters.</param>
        public void AppendFormatted(scoped ReadOnlySpan<char> value) => _text.AppendFormatted(value);

        /// <summary>Writes a hole of text.</summary>
        /// <param name="value">The text.</param>
        public void AppendFormatted(string? value) => _text.AppendFormatted(value);

        /// <summary>The text written, and the room it took handed back.</summary>
        /// <returns>The text.</returns>
        internal string ToStringAndClear() => _text.ToStringAndClear();
    }
}
