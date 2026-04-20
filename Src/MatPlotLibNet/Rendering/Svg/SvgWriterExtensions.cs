// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>
/// Extension methods for writing SVG-compatible primitives without introducing a static
/// "helper class" (per user convention: <c>feedback_no_magic_strings</c> + the
/// only-permitted-extension-shape rule). Each method is a <c>public static T M(this Type self, ...)</c>
/// so it reads fluently at the call site and belongs to the type it operates on.
/// </summary>
/// <remarks>
/// Phase F.2 of the strict-90 floor plan (2026-04-20). Extracted from private statics
/// inside <see cref="SvgRenderContext"/> — <c>F(double)</c>, <c>AppendFillStroke</c>,
/// <c>AppendDashArray</c>. Byte-equivalent to the old private implementations; verified
/// by the fidelity suite (146 matplotlib pixel tests) and the SvgRenderContext test suite.
/// </remarks>
internal static class SvgWriterExtensions
{
    /// <summary>
    /// Formats a double in the exact shape the SVG emitter requires: invariant culture
    /// (so nl-NL hosts still emit <c>1.5</c> not <c>1,5</c>) with the <c>"G"</c> general
    /// format specifier.
    /// </summary>
    /// <remarks>
    /// Byte-equivalent to the private static <c>F(double)</c> that used to live inside
    /// <see cref="SvgRenderContext"/>. Pinning that byte-equivalence matters because
    /// SVG path-data strings feed directly into matplotlib's pixel-parity diff tests.
    /// </remarks>
    public static string ToSvgNumber(this double value)
        => value.ToString("G", CultureInfo.InvariantCulture);

    /// <summary>
    /// Appends the <c>fill</c>, optional <c>fill-opacity</c>, and (if stroke set)
    /// <c>stroke</c> + <c>stroke-width</c> attributes to <paramref name="sb"/> using the
    /// exact same byte layout as the private method this replaces in
    /// <see cref="SvgRenderContext"/>. Returns <paramref name="sb"/> for fluent chaining.
    /// </summary>
    /// <remarks>
    /// Byte-contract: <c>fill=</c> is emitted first (either the hex value or <c>"none"</c>),
    /// then <c>fill-opacity</c> if alpha &lt; 255, then stroke attributes only if stroke is
    /// non-null. Matches <see cref="SvgRenderContext"/>.<c>AppendFillStroke</c> pre-F.2.b.
    /// </remarks>
    public static StringBuilder AppendFillStroke(this StringBuilder sb,
        Color? fill, Color? stroke, double strokeThickness)
    {
        if (fill.HasValue)
        {
            sb.Append(" fill=\"").Append(fill.Value.ToHex()).Append('"');
            if (fill.Value.A < 255)
                sb.Append(" fill-opacity=\"").Append((fill.Value.A / 255.0).ToSvgNumber()).Append('"');
        }
        else
            sb.Append(" fill=\"none\"");

        if (stroke.HasValue)
            sb.Append(" stroke=\"").Append(stroke.Value.ToHex()).Append("\" stroke-width=\"").Append(strokeThickness.ToSvgNumber()).Append('"');

        return sb;
    }

    /// <summary>
    /// Appends the SVG <c>stroke-dasharray</c> attribute corresponding to
    /// <paramref name="style"/>. Emits nothing for <see cref="LineStyle.Solid"/> and
    /// <see cref="LineStyle.None"/>. Returns <paramref name="sb"/> for fluent chaining.
    /// </summary>
    /// <remarks>
    /// Pattern values are sourced from <see cref="DashPatterns.GetPattern"/>, keeping the
    /// dash/gap ratios matplotlib-calibrated (see comment in DashPatterns.cs for pt→px
    /// conversions at 96 dpi).
    /// </remarks>
    public static StringBuilder AppendDashArray(this StringBuilder sb, LineStyle style)
    {
        var pattern = DashPatterns.GetPattern(style);
        if (pattern.Length == 0) return sb;
        sb.Append(" stroke-dasharray=\"");
        for (int i = 0; i < pattern.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(pattern[i].ToSvgNumber());
        }
        sb.Append('"');
        return sb;
    }
}
