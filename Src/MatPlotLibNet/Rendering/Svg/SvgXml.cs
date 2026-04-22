// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>XML escaping extensions for SVG output.</summary>
internal static class SvgXml
{
    /// <summary>Escapes <c>&amp;</c>, <c>&lt;</c>, and <c>&gt;</c> for safe embedding in XML/SVG
    /// attributes and text content. Returns the same reference when no escaping is needed.</summary>
    internal static string EscapeForXml(this string text)
    {
        if (text.AsSpan().IndexOfAny('&', '<', '>') < 0) return text;

        var sb = new StringBuilder(text.Length + 8);
        foreach (var ch in text)
        {
            switch (ch)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                default:  sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }
}
