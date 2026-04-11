// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Text;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>Shared XML escaping utility for SVG output, used by both <see cref="SvgRenderContext"/> and <see cref="MatPlotLibNet.Transforms.SvgTransform"/>.</summary>
internal static class SvgXmlHelper
{
    /// <summary>Escapes <c>&amp;</c>, <c>&lt;</c>, and <c>&gt;</c> for safe embedding in XML/SVG attributes and text content.</summary>
    internal static string EscapeXml(string text)
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
