// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>Maps LaTeX Greek letter command names to their Unicode equivalents: the names of the math grammar's one symbol table
/// (matplotlib's own, the table <see cref="MathSymbols"/> reads too) whose character is a letter of the Greek block, such as
/// <c>\alpha</c>, <c>\Omega</c> and <c>\varphi</c>.</summary>
public static class GreekLetters
{
    /// <summary>Returns the Unicode string for the given command name, or <see langword="null"/> if not found.</summary>
    public static string? TryGet(string name) =>
        MathGrammar.Symbols.TryGetValue(name, out var symbol) && IsGreekLetter(symbol.Glyph[0]) ? symbol.Glyph : null;

    /// <summary>Whether <paramref name="c"/> is a letter of the Unicode block Greek and Coptic (U+0370 to U+03FF).</summary>
    private static bool IsGreekLetter(char c) =>
        c is >= 'Ͱ' and <= 'Ͽ' && CharUnicodeInfo.GetUnicodeCategory(c) is UnicodeCategory.LowercaseLetter or UnicodeCategory.UppercaseLetter;
}
