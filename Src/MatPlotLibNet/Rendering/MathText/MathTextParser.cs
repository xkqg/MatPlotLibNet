// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>
/// Parses a label string that may contain inline LaTeX math mode (<c>$...$</c>) into a flat
/// list of <see cref="TextSpan"/> objects suitable for rich-text rendering.
/// <para>
/// Supported inside <c>$...$</c>: the grammar of matplotlib's mathtext (every symbol name, Greek
/// letters included, the function names, the accents and the spaces), superscript <c>^{text}</c> or <c>^x</c>, subscript
/// <c>_{text}</c> or <c>_x</c>, fractions, radicals, font switches, delimiters and matrices; math is spaced as matplotlib
/// spaces it.
/// Outside math mode all characters are emitted as-is.
/// </para>
/// </summary>
public static class MathTextParser
{
    /// <summary>Parses the input string into a <see cref="RichText"/> span list.</summary>
    /// <remarks>Outside <c>$...$</c> the text is kept as it is. Inside, a <c>\command</c> is looked up in one table that names
    /// the family reading it (a space, a fraction, a radical, an accent, a font switch, a delimiter, a matrix, an operator), whose
    /// arguments are read as math in turn; a name the table does not hold is a Greek letter or a math sign, or is shown as written
    /// when it is neither. The spans are the flat view of the label's layout: one span for each run of one kind, size and font
    /// switch, a nested script, a fraction's part and a limit marked by its kind.</remarks>
    public static RichText Parse(string text) =>
        MathSpanWriter.Write(MathTextCursor.Parse(text ?? string.Empty));

    /// <summary>
    /// Returns <see langword="true"/> when the string contains at least two <c>$</c> delimiters,
    /// indicating a properly-delimited math expression like <c>$\alpha$</c>.
    /// A single <c>$</c> (e.g. as a currency symbol in "Revenue ($)") is treated as a literal.
    /// </summary>
    public static bool ContainsMath(string? text)
    {
        if (text is null) return false;
        int first = text.IndexOf('$');
        return first >= 0 && text.IndexOf('$', first + 1) >= 0;
    }
}
