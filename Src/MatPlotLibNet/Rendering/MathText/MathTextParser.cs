// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
                int start = i;
                while (i < text.Length && char.IsLetter(text[i])) i++;
                string cmd = text[start..i];

                string? substitute = GreekLetters.TryGet(cmd) ?? MathSymbols.TryGet(cmd);
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
                    i++; // skip '{'
                    int end = text.IndexOf('}', i);
                    if (end < 0) end = text.Length;
                    content = text[i..end];
                    i = end < text.Length ? end + 1 : text.Length;
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
                spans.Add(new TextSpan(content, kind, SuperSubScale));
                continue;
            }

            // Any other character inside math mode: emit literally
            buf.Append(c);
            i++;
        }

        Flush(spans, buf);
        return new RichText(spans);
    }

    /// <summary>Returns <see langword="true"/> when the string contains at least one <c>$</c> delimiter.</summary>
    public static bool ContainsMath(string? text) => text is not null && text.Contains('$');

    // --- Helpers ---

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
