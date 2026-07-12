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
    /// then <c>fill-opacity</c> if alpha &lt; 255, then stroke attributes only if the stroke is
    /// visible. Fill visibility is <see cref="ShapeStyle.HasVisibleFill"/>; stroke visibility is
    /// <see cref="ShapeStyle.HasVisibleStroke"/> — the centralized guard now also skips the stroke
    /// when <see cref="ShapeStyle.StrokeThickness"/> is not positive (previously the SVG backend
    /// emitted <c>stroke-width="0"</c> in that case, which browsers render as no stroke anyway).
    /// </remarks>
    /// <param name="sb">The output buffer.</param>
    /// <param name="shape">The fill/stroke bundle to emit.</param>
    /// <param name="patternRef">A hatch-pattern id (without <c>#</c>) resolved by the render context's hatch
    /// registry, or <see langword="null"/> for a plain fill. When present it replaces the flat fill colour with a
    /// <c>url(#id)</c> reference — the pattern tile already carries the base fill, so the two never drift apart.</param>
    public static StringBuilder AppendFillStroke(this StringBuilder sb, ShapeStyle shape, string? patternRef = null)
    {
        if (patternRef is not null)
        {
            sb.Append(" fill=\"url(#").Append(patternRef).Append(")\"");
        }
        else if (shape.HasVisibleFill)
        {
            Color fill = shape.Fill!.Value;
            sb.Append(" fill=\"").Append(fill.ToHex()).Append('"');
            if (fill.A < 255)
            {
                sb.Append(" fill-opacity=\"").Append((fill.A / 255.0).ToSvgNumber()).Append('"');
            }
        }
        else
        {
            sb.Append(" fill=\"none\"");
        }

        if (shape.HasVisibleStroke)
        {
            sb.Append(" stroke=\"").Append(shape.Stroke!.Value.ToHex()).Append("\" stroke-width=\"").Append(shape.StrokeThickness.ToSvgNumber()).Append('"');
        }

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
