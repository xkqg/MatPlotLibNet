// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.MathText;

/// <summary>
/// Parses a label string that may contain inline LaTeX math mode (<c>$...$</c>) into a flat
/// list of <see cref="TextSpan"/> objects suitable for rich-text rendering.
/// <para>
/// Supported inside <c>$...$</c>: Greek letters (<c>\alpha</c>…<c>\Omega</c>),
/// math symbols (<c>\pm</c>, <c>\infty</c>, …), superscript <c>^{text}</c> or <c>^x</c>,
/// and subscript <c>_{text}</c> or <c>_x</c>.
/// Outside math mode all characters are emitted as-is.
/// </para>
/// </summary>
public static class MathTextParser
{
    private const double SuperSubScale = 0.70;
    private const double FractionScale = 0.70;

    // Spacing commands → Unicode whitespace
    private static readonly Dictionary<string, string> SpacingCommands = new()
    {
        ["!"]     = "",            // negative thin space (collapse)
        [","]     = "\u2009",      // thin space
        [":"]     = "\u2005",      // medium mathematical space
        [";"]     = "\u2004",      // thick mathematical space
        ["quad"]  = "\u2003",      // em space
        ["qquad"] = "\u2003\u2003", // double em space
    };

    // Accent commands → Unicode accent characters
    private static readonly Dictionary<string, string> AccentCommands = new()
    {
        ["hat"]      = "\u0302", // combining circumflex
        ["bar"]      = "\u0304", // combining macron
        ["overline"] = "\u0305", // combining overline
        ["tilde"]    = "\u0303", // combining tilde
        ["dot"]      = "\u0307", // combining dot above
        ["ddot"]     = "\u0308", // combining diaeresis
        ["vec"]      = "\u20D7", // combining right arrow above
        ["check"]    = "\u030C", // combining caron
        ["breve"]    = "\u0306", // combining breve
    };

    // Font-switch commands → FontVariant
    private static readonly Dictionary<string, FontVariant> FontCommands = new()
    {
        ["mathrm"]  = FontVariant.Roman,
        ["text"]    = FontVariant.Roman,
        ["mathbf"]  = FontVariant.Bold,
        ["mathit"]  = FontVariant.Italic,
        ["mathcal"] = FontVariant.Calligraphic,
        ["mathbb"]  = FontVariant.BlackboardBold,
    };

    /// <summary>Parses the input string into a <see cref="RichText"/> span list.</summary>
    public static RichText Parse(string text)
    {
        var spans = new List<TextSpan>();
        if (string.IsNullOrEmpty(text)) return new RichText(spans);

        var buf = new StringBuilder();
        bool inMath = false;
        int i = 0;

        while (i < text.Length)
        {
            char c = text[i];

            // Dollar sign toggles math mode
            if (c == '$')
            {
                Flush(spans, buf);
                inMath = !inMath;
                i++;
                continue;
            }

            // Outside math mode: emit literally
            if (!inMath)
            {
                buf.Append(c);
                i++;
                continue;
            }

            // Inside math mode ----------------------------------------

            // Backslash: parse command name
            if (c == '\\')
            {
                Flush(spans, buf);
                i++;

                // Single-character spacing commands: \, \: \; \!
                if (i < text.Length && SpacingCommands.TryGetValue(text[i].ToString(), out var sp))
                {
                    i++;
                    if (sp.Length > 0) spans.Add(new TextSpan(sp));
                    continue;
                }

                int start = i;
                while (i < text.Length && char.IsLetter(text[i])) i++;
                string cmd = text[start..i];

                // Spacing commands (word-form): \quad, \qquad
                if (SpacingCommands.TryGetValue(cmd, out var spacing))
                {
                    spans.Add(new TextSpan(spacing));
                    continue;
                }

                // Fraction: \frac{num}{den}
                if (cmd == "frac")
                {
                    string num = ReadBraceGroup(text, ref i);
                    string den = ReadBraceGroup(text, ref i);
                    num = SubstituteCommands(num);
                    den = SubstituteCommands(den);
                    spans.Add(new TextSpan(num, TextSpanKind.FractionNumerator, FractionScale));
                    spans.Add(new TextSpan(den, TextSpanKind.FractionDenominator, FractionScale));
                    continue;
                }

                // Square root: \sqrt{content} or \sqrt[n]{content}
                if (cmd == "sqrt")
                {
                    // Optional index: \sqrt[n]
                    if (i < text.Length && text[i] == '[')
                    {
                        i++; // skip '['
                        int end = text.IndexOf(']', i);
                        if (end < 0) end = text.Length;
                        string idx = text[i..end];
                        i = end < text.Length ? end + 1 : text.Length;
                        // Emit index as superscript-sized prefix
                        spans.Add(new TextSpan(idx, TextSpanKind.Superscript, SuperSubScale));
                    }

                    string content = ReadBraceGroup(text, ref i);
                    content = SubstituteCommands(content);
                    spans.Add(new TextSpan(content, TextSpanKind.Radical));
                    continue;
                }

                // Accent commands: \hat{x}, \bar{y}, etc.
                if (AccentCommands.TryGetValue(cmd, out var accent))
                {
                    string content = ReadBraceGroup(text, ref i);
                    content = SubstituteCommands(content);
                    // Emit the base content + combining accent character
                    spans.Add(new TextSpan(content + accent, TextSpanKind.Accent));
                    continue;
                }

                // Font-switch commands: \mathrm{text}, \mathbf{text}, \text{text}, etc.
                if (FontCommands.TryGetValue(cmd, out var variant))
                {
                    string content = ReadBraceGroup(text, ref i);
                    content = SubstituteCommands(content);
                    spans.Add(new TextSpan(content, TextSpanKind.Normal, 1.0, variant));
                    continue;
                }

                // Scaling delimiters: \left( ... \right)
                if (cmd == "left")
                {
                    if (i < text.Length)
                    {
                        spans.Add(new TextSpan(text[i].ToString()));
                        i++;
                    }
                    continue;
                }
                if (cmd == "right")
                {
                    if (i < text.Length)
                    {
                        spans.Add(new TextSpan(text[i].ToString()));
                        i++;
                    }
                    continue;
                }

                // Matrix environments: \begin{pmatrix} a & b \\ c & d \end{pmatrix}
                if (cmd == "begin")
                {
                    string envName = ReadBraceGroup(text, ref i);
                    spans.Add(new TextSpan(envName, TextSpanKind.MatrixStart));

                    // Find matching \end{envName}
                    string endMarker = $"\\end{{{envName}}}";
                    int endPos = text.IndexOf(endMarker, i, StringComparison.Ordinal);
                    if (endPos < 0) endPos = text.Length;
                    string body = text[i..endPos];
                    i = endPos + endMarker.Length;

                    // Parse rows (separated by \\) and cells (separated by &)
                    var rows = body.Split(@"\\");
                    foreach (var row in rows)
                    {
                        var cells = row.Split('&');
                        foreach (var cell in cells)
                        {
                            string cellContent = SubstituteCommands(cell.Trim());
                            spans.Add(new TextSpan(cellContent, TextSpanKind.MatrixCell, FractionScale));
                            spans.Add(new TextSpan("", TextSpanKind.MatrixCellSeparator));
                        }
                        spans.Add(new TextSpan("", TextSpanKind.MatrixRowSeparator));
                    }

                    spans.Add(new TextSpan(envName, TextSpanKind.MatrixEnd));
                    continue;
                }
                if (cmd == "end")
                {
                    ReadBraceGroup(text, ref i); // consume the {envName} — already handled by \begin
                    continue;
                }

                // Text-form operators: \lim, \max, \min, \sup, \inf
                if (cmd is "lim" or "max" or "min" or "sup" or "inf" or "log" or "ln" or "sin" or "cos" or "tan")
                {
                    spans.Add(new TextSpan(cmd, TextSpanKind.LargeOperator, 1.0, FontVariant.Roman));
                    continue;
                }

                // Standard substitution: Greek letters and math symbols
                string? substitute = GreekLetters.TryGet(cmd) ?? MathSymbols.TryGet(cmd);
                // Large operators: ∫, Σ, Π and variants — render at increased size
                if (cmd is "int" or "iint" or "iiint" or "oint" or "sum" or "prod")
                {
                    spans.Add(new TextSpan(substitute ?? $"\\{cmd}", TextSpanKind.LargeOperator, 1.4));
                    continue;
                }
                spans.Add(new TextSpan(substitute ?? $"\\{cmd}"));
                continue;
            }

            // Superscript / subscript
            if (c == '^' || c == '_')
            {
                Flush(spans, buf);
                var kind = c == '^' ? TextSpanKind.Superscript : TextSpanKind.Subscript;
                i++;

                string content;
                if (i < text.Length && text[i] == '{')
                {
                    content = ReadBraceGroup(text, ref i);
                }
                else if (i < text.Length && text[i] != '$')
                {
                    content = text[i].ToString();
                    i++;
                }
                else
                {
                    content = string.Empty;
                }

                // Content of super/subscript may itself contain \command substitutions
                content = SubstituteCommands(content);

                // If a recent span is a large operator (may have sub already between), emit as operator limit
                bool hasRecentOperator = spans.Count > 0 &&
                    (spans[^1].Kind == TextSpanKind.LargeOperator ||
                     spans[^1].Kind == TextSpanKind.OperatorSubscript ||
                     spans[^1].Kind == TextSpanKind.OperatorSuperscript);
                if (hasRecentOperator)
                {
                    var opKind = c == '^'
                        ? TextSpanKind.OperatorSuperscript
                        : TextSpanKind.OperatorSubscript;
                    spans.Add(new TextSpan(content, opKind, SuperSubScale));
                }
                else
                {
                    spans.Add(new TextSpan(content, kind, SuperSubScale));
                }
                continue;
            }

            // Any other character inside math mode: emit literally
            buf.Append(c);
            i++;
        }

        Flush(spans, buf);
        return new RichText(spans);
    }

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

    // --- Helpers ---

    /// <summary>Reads a <c>{...}</c> brace group starting at position <paramref name="i"/>,
    /// advancing <paramref name="i"/> past the closing brace. Handles nested braces.</summary>
    private static string ReadBraceGroup(string text, ref int i)
    {
        if (i >= text.Length || text[i] != '{')
            return i < text.Length ? text[i++].ToString() : string.Empty;

        i++; // skip opening '{'
        int depth = 1;
        int start = i;
        while (i < text.Length && depth > 0)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') depth--;
            if (depth > 0) i++;
        }
        string content = text[start..i];
        if (i < text.Length) i++; // skip closing '}'
        return content;
    }

    private static void Flush(List<TextSpan> spans, StringBuilder buf)
    {
        if (buf.Length > 0)
        {
            spans.Add(new TextSpan(buf.ToString()));
            buf.Clear();
        }
    }

    /// <summary>Applies <c>\command</c> substitutions within a short content string (e.g., super/subscript body).</summary>
    private static string SubstituteCommands(string content)
    {
        if (!content.Contains('\\')) return content;

        var sb = new StringBuilder();
        int i = 0;
        while (i < content.Length)
        {
            if (content[i] == '\\')
            {
                i++;
                int start = i;
                while (i < content.Length && char.IsLetter(content[i])) i++;
                string cmd = content[start..i];
                sb.Append(GreekLetters.TryGet(cmd) ?? MathSymbols.TryGet(cmd) ?? $"\\{cmd}");
            }
            else
            {
                sb.Append(content[i]);
                i++;
            }
        }
        return sb.ToString();
    }
}
