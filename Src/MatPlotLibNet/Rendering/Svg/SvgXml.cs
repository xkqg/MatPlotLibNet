// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>XML escaping extensions for SVG output.</summary>
internal static class SvgXml
{
    /// <summary>Escapes <c>&amp;</c>, <c>&lt;</c>, <c>&gt;</c> and <c>"</c> for safe embedding in XML/SVG text
    /// content AND double-quoted attributes — one escaper for both, because the same label lands in a
    /// <c>&lt;title&gt;</c> and in an <c>aria-label="…"</c>. The apostrophe is left alone: the library writes no
    /// single-quoted attribute, and escaping it would touch every title that has one. Returns the same reference
    /// when no escaping is needed.</summary>
    internal static string EscapeForXml(this string text)
    {
        if (text.AsSpan().IndexOfAny("&<>\"") < 0) return text;

        var sb = new StringBuilder(text.Length + 8);
        foreach (var ch in text)
        {
            switch (ch)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                default:  sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }
}
