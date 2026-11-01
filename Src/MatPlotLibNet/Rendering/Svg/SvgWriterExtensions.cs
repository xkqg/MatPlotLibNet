// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>
/// Extension methods for writing SVG-compatible primitives without introducing a static
/// "helper class". Each method is a <c>public static T M(this Type self, ...)</c>
/// so it reads fluently at the call site and belongs to the type it operates on.
/// </summary>
/// <remarks>
/// Holds <c>ToSvgNumber</c>, <c>AppendFillStroke</c>, <c>AppendStroke</c>, <c>AppendAlpha</c> and
/// <c>AppendDashArray</c>, which <see cref="SvgRenderContext"/> and the other SVG writers share. Every colour the SVG
/// writes carries its alpha through <c>AppendAlpha</c>, and every stroke drawn from a colour is written by
/// <c>AppendStroke</c>.
/// </remarks>
internal static class SvgWriterExtensions
{
    extension(double value)
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
        public string ToSvgNumber()
            => value.ToString("G", CultureInfo.InvariantCulture);
    }

    extension(StringBuilder sb)
    {
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
        /// emitted <c>stroke-width="0"</c> in that case, which browsers render as no stroke anyway), or
        /// when <see cref="ShapeStyle.StrokeLineStyle"/> is <see cref="LineStyle.None"/>. A visible stroke
        /// is written by <see cref="AppendStroke"/>, the writer a line's stroke uses: its colour, its width,
        /// <c>stroke-opacity</c> if its colour's alpha &lt; 255 and <c>stroke-dasharray</c> if it is dashed, so an
        /// opaque solid stroke (the default) writes the bytes it always wrote. The fill's alpha is written by
        /// <see cref="AppendAlpha"/>, the same rule.
        /// </remarks>
        /// <param name="shape">The fill/stroke bundle to emit.</param>
        /// <param name="patternRef">A hatch-pattern id (without <c>#</c>) resolved by the render context's hatch
        /// registry, or <see langword="null"/> for a plain fill. When present it replaces the flat fill colour with a
        /// <c>url(#id)</c> reference — the pattern tile already carries the base fill, so the two never drift apart.</param>
        public StringBuilder AppendFillStroke(ShapeStyle shape, string? patternRef = null)
        {
            if (patternRef is not null)
            {
                sb.Append(" fill=\"url(#").Append(patternRef).Append(")\"");
            }
            else if (shape.HasVisibleFill)
            {
                Color fill = shape.Fill!.Value;
                sb.Append(" fill=\"").Append(fill.ToHex()).Append('"').AppendAlpha("fill-opacity", fill);
            }
            else
            {
                sb.Append(" fill=\"none\"");
            }

            if (shape.HasVisibleStroke)
            {
                sb.AppendStroke(new StrokeStyle(shape.Stroke!.Value, shape.StrokeThickness, shape.StrokeLineStyle));
            }

            return sb;
        }

        /// <summary>
        /// Appends the alpha of <paramref name="color"/> as <paramref name="attribute"/> when the colour is translucent, and
        /// nothing when it is opaque. Returns <paramref name="sb"/> for fluent chaining.
        /// </summary>
        /// <remarks>
        /// The one rule for the alpha of a colour in the SVG. A hex colour carries no alpha, and Skia and MAUI paint every
        /// colour in its alpha, so a translucent colour writes it beside the colour, in the attribute of the paint it colours:
        /// <c>fill-opacity</c> for a fill, <c>stroke-opacity</c> for a stroke, <c>stop-opacity</c> for a gradient stop,
        /// <c>opacity</c> for a hatch dot filled and stroked in one colour. An opaque colour writes nothing, so it keeps the
        /// bytes it always had.
        /// </remarks>
        /// <param name="attribute">The attribute the alpha is written as.</param>
        /// <param name="color">The colour whose alpha is written.</param>
        public StringBuilder AppendAlpha(string attribute, Color color)
        {
            if (color.A < 255)
            {
                sb.Append(' ').Append(attribute).Append("=\"").Append((color.A / 255.0).ToSvgNumber()).Append('"');
            }

            return sb;
        }

        /// <summary>
        /// Appends the attributes of a stroke drawn from a colour: <c>stroke</c>, <c>stroke-width</c>, <c>stroke-opacity</c>
        /// when the colour is translucent (<see cref="AppendAlpha"/>) and <c>stroke-dasharray</c> when the style is dashed
        /// (<see cref="AppendDashArray"/>), in that order. Returns <paramref name="sb"/> for fluent chaining.
        /// </summary>
        /// <remarks>
        /// The one writer of such a stroke: a line, a polyline, a shape's outline and a hatch line. It writes what it is given
        /// and decides nothing about visibility; a caller that skips an invisible stroke does so before calling it. An opaque
        /// solid stroke writes <c>stroke</c> and <c>stroke-width</c> alone, the bytes a line wrote before its alpha was carried.
        /// </remarks>
        /// <param name="stroke">The colour, width and dash style of the stroke.</param>
        public StringBuilder AppendStroke(StrokeStyle stroke)
        {
            sb.Append(" stroke=\"").Append(stroke.Color.ToHex())
              .Append("\" stroke-width=\"").Append(stroke.Thickness.ToSvgNumber()).Append('"');
            return sb.AppendAlpha("stroke-opacity", stroke.Color).AppendDashArray(stroke.Style);
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
        public StringBuilder AppendDashArray(LineStyle style)
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
}
