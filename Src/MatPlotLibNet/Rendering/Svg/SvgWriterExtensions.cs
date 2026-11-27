// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Models;
using System.Text;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.Svg;

/// <summary>
/// Extension methods for writing SVG-compatible primitives without introducing a static
/// "helper class". Each method is a member of an <c>extension(Type self)</c> block, so it reads
/// fluently at the call site and belongs to the type it operates on.
/// </summary>
/// <remarks>
/// Holds <c>AppendSvgNumber</c> (a number of the drawing, to two decimals), <c>AppendExactSvgNumber</c> (every digit: a view
/// attribute a script reads back, an opacity), <c>AppendHex</c>, <c>AppendSvgPath</c>, <c>AppendFillStroke</c>, <c>AppendStroke</c>,
/// <c>AppendAlpha</c> and <c>AppendDashArray</c>, which the writers of <see cref="SvgRenderContext"/> and the other SVG writers share. Every colour the SVG
/// writes carries its alpha through <c>AppendAlpha</c>, and every stroke drawn from a colour is written by
/// <c>AppendStroke</c>.
/// </remarks>
internal static class SvgWriterExtensions
{
    /// <summary>One Y axis stacked past the primary, as the attribute names say it: the side it stands on and its outward index
    /// within the side.</summary>
    internal readonly record struct StackedSlot(AxisSide Side, int Index);

    extension(double value)
    {
        /// <summary>Writes the number with every digit, in the general (<c>"G"</c>) form of the invariant culture, into
        /// <paramref name="digits"/>: the text <see cref="AppendExactSvgNumber"/> appends, for a writer that puts one number in
        /// more than one place and formats it once.</summary>
        /// <param name="digits">At least 32 characters: the longest "G" form of a double is 24 ("-1.7976931348623157E+308"), so
        /// the format cannot run short; the tests pin every edge of the format against <c>double.ToString("G")</c>.</param>
        /// <returns>How many characters were written.</returns>
        public int FormatExactSvgNumber(Span<char> digits) => value.FormatInvariant(digits);

        /// <summary>The number as the SVG writes a number of the drawing (<see cref="AppendSvgNumber"/>), as a string: for a
        /// writer that builds its attributes as values, not into a builder.</summary>
        /// <returns>The number's text.</returns>
        public string ToSvgNumber() => new StringBuilder(16).AppendSvgNumber(value).ToString();
    }

    extension(StringBuilder sb)
    {
        /// <summary>Appends <paramref name="value"/> with every digit, in the general (<c>"G"</c>) form of the invariant
        /// culture, formatted straight into the builder. For the numbers a script reads back (the plot area and data range of a view,
        /// a camera) and for an opacity, which is not a pixel: the alpha the browser paints is the alpha of the colour.</summary>
        /// <param name="value">The number to write.</param>
        /// <returns><paramref name="sb"/>, for chaining.</returns>
        public StringBuilder AppendExactSvgNumber(double value)
        {
            Span<char> digits = stackalloc char[32];
            return sb.Append(digits[..value.FormatExactSvgNumber(digits)]);
        }

        /// <summary>Appends <paramref name="value"/> as the attribute <paramref name="name"/> (<c> name="value"</c>), with every
        /// digit (<see cref="AppendExactSvgNumber"/>): the one writer of a number a script reads back off the chart.</summary>
        /// <param name="name">The attribute's name.</param>
        /// <param name="value">Its value.</param>
        /// <returns><paramref name="sb"/>, for chaining.</returns>
        public StringBuilder AppendExactAttribute(string name, double value) =>
            sb.Append(' ').Append(name).Append("=\"").AppendExactSvgNumber(value).Append('"');

        /// <summary>Appends a number of the drawing (a coordinate, a length, a width, a font size, an angle) to two
        /// decimals: a hundredth of a pixel, far below what a screen or a print shows. Trailing zeros and a bare point are
        /// left out, and a number that rounds to zero is written <c>0</c>, never <c>-0</c>.</summary>
        /// <remarks>The number is scaled to a whole number of hundredths, rounded half away from zero, and its digits are
        /// written as an integer's, with the point put before the last two: no floating-point formatting at all, which was
        /// the largest single cost of writing a drawing. A number past what a whole number of hundredths holds, and NaN or
        /// an infinity, is written by <see cref="AppendExactSvgNumber"/>, as it always was.</remarks>
        /// <param name="value">The number to write.</param>
        /// <returns><paramref name="sb"/>, for chaining.</returns>
        public StringBuilder AppendSvgNumber(double value)
        {
            double hundredths = Math.Round(value * 100, MidpointRounding.AwayFromZero);
            // 1e18 is well inside a long; the comparison is false for NaN as well.
            if (!(Math.Abs(hundredths) < 1e18)) return sb.AppendExactSvgNumber(value);

            long scaled = (long)hundredths;
            if (scaled == 0) return sb.Append('0');

            // 18 digits at most, a point and a sign.
            Span<char> digits = stackalloc char[24];
            int at = digits.Length;
            ulong rest = (ulong)Math.Abs(scaled);
            int fraction = (int)(rest % 100);
            rest /= 100;
            if (fraction != 0)
            {
                if (fraction % 10 != 0) digits[--at] = (char)('0' + fraction % 10);
                digits[--at] = (char)('0' + fraction / 10);
                digits[--at] = '.';
            }

            do
            {
                digits[--at] = (char)('0' + (int)(rest % 10));
                rest /= 10;
            }
            while (rest != 0);

            if (scaled < 0) digits[--at] = '-';
            return sb.Append(digits[at..]);
        }

        /// <summary>Appends a colour as SVG writes it in an attribute: <c>#RRGGBB</c> in upper-case digits, the bytes
        /// <see cref="Color.ToHex"/> returns, written straight into the builder. A hex colour carries no alpha; the alpha is
        /// written beside it by <see cref="AppendAlpha"/>.</summary>
        /// <param name="color">The colour to write.</param>
        /// <returns><paramref name="sb"/>, for chaining.</returns>
        public StringBuilder AppendHex(Color color)
        {
            const string digits = "0123456789ABCDEF";
            return sb.Append('#')
                .Append(digits[color.R >> 4]).Append(digits[color.R & 15])
                .Append(digits[color.G >> 4]).Append(digits[color.G & 15])
                .Append(digits[color.B >> 4]).Append(digits[color.B & 15]);
        }

        /// <summary>Appends the path data of <paramref name="segments"/> one after the other, each written by its own
        /// <c>AppendSvgPathData</c>: the one loop for every <c>&lt;path d="..."&gt;</c> the SVG writes.</summary>
        /// <param name="segments">The segments, in drawing order.</param>
        /// <returns><paramref name="sb"/>, for chaining.</returns>
        public StringBuilder AppendSvgPath(IReadOnlyList<PathSegment> segments)
        {
            for (int i = 0; i < segments.Count; i++) segments[i].AppendSvgPathData(sb);
            return sb;
        }

        /// <summary>Appends a point as SVG writes it in a list of points: <c>x,y</c>.</summary>
        /// <param name="point">The point to write.</param>
        /// <returns><paramref name="sb"/>, for chaining.</returns>
        public StringBuilder AppendSvgPoint(Point point) => sb.AppendSvgNumber(point.X).Append(',').AppendSvgNumber(point.Y);

        /// <summary>
        /// Appends the <c>fill</c>, optional <c>fill-opacity</c>, and (if stroke set)
        /// <c>stroke</c> + <c>stroke-width</c> attributes of a shape to <paramref name="sb"/> (<see cref="Rendering.Svg.SvgRenderContext.SvgMarkWriter"/> writes every
        /// shape through it). Returns <paramref name="sb"/> for fluent chaining.
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
        /// <see cref="AppendAlpha"/>, the same rule. An aliased fill (<see cref="ShapeStyle.Aliased"/>) adds
        /// <c>shape-rendering="crispEdges"</c> last.
        /// </remarks>
        /// <param name="shape">The fill/stroke bundle to emit.</param>
        /// <param name="dashes">The dash look of the theme the shape is drawn under: the lengths its dashed stroke is written with.</param>
        /// <param name="patternRef">The id (without <c>#</c>) of the paint server the element buffer defined for the shape
        /// (<see cref="SvgElementBuffer.PaintServerOf"/>: its gradient or its hatch pattern), or <see langword="null"/> for a plain fill. When
        /// present it replaces the flat fill colour with a <c>url(#id)</c> reference — the pattern tile already carries the base fill, so the
        /// two never drift apart.</param>
        public StringBuilder AppendFillStroke(in ShapeStyle shape, DashLook dashes, string? patternRef = null)
        {
            if (patternRef is not null)
            {
                sb.Append(" fill=\"url(#").Append(patternRef).Append(")\"");
            }
            else if (shape.HasVisibleFill)
            {
                Color fill = shape.Fill!.Value;
                sb.Append(" fill=\"").AppendHex(fill).Append('"').AppendAlpha("fill-opacity", fill);
            }
            else
            {
                sb.Append(" fill=\"none\"");
            }

            if (shape.HasVisibleStroke)
            {
                sb.AppendStroke(shape.OutlineStroke(), dashes);
            }

            // An aliased fill (ShapeStyle.Aliased) asks the viewer for no antialiasing; every other shape writes nothing here.
            if (shape.Aliased)
            {
                sb.Append(" shape-rendering=\"crispEdges\"");
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
            return color.A < 255 ? sb.AppendExactAttribute(attribute, color.A.Share) : sb;
        }

        /// <summary>
        /// Appends the attributes of a stroke drawn from a colour: <c>stroke</c>, <c>stroke-width</c>, <c>stroke-opacity</c>
        /// when the colour is translucent (<see cref="AppendAlpha"/>) and <c>stroke-dasharray</c> when the style is dashed
        /// (<see cref="AppendDashArray"/>) and <c>stroke-linejoin</c> when the join is not the mitre every viewer draws by default, in that
        /// order. Returns <paramref name="sb"/> for fluent chaining.
        /// </summary>
        /// <remarks>
        /// The one writer of such a stroke: a line, a polyline, a shape's outline and a hatch line. It writes what it is given
        /// and decides nothing about visibility; a caller that skips an invisible stroke does so before calling it. An opaque
        /// solid stroke writes <c>stroke</c> and <c>stroke-width</c> alone, the bytes a line wrote before its alpha was carried.
        /// </remarks>
        /// <param name="stroke">The colour, width and dash style of the stroke.</param>
        /// <param name="dashes">The dash look of the theme the stroke is drawn under.</param>
        public StringBuilder AppendStroke(StrokeStyle stroke, DashLook dashes)
        {
            sb.Append(" stroke=\"").AppendHex(stroke.Color)
              .Append("\" stroke-width=\"").AppendSvgNumber(stroke.Thickness).Append('"');
            sb.AppendAlpha("stroke-opacity", stroke.Color).AppendDashArray(stroke.Style, stroke.Thickness, dashes);
            if (stroke.Join?.ToSvgName() is { } join)
            {
                sb.Append(" stroke-linejoin=\"").Append(join).Append('"');
            }

            return sb;
        }

        /// <summary>
        /// Appends the SVG <c>stroke-dasharray</c> attribute of a line of <paramref name="style"/> and <paramref name="thickness"/>.
        /// Emits nothing for <see cref="LineStyle.Solid"/> and <see cref="LineStyle.None"/>. Returns <paramref name="sb"/> for fluent
        /// chaining.
        /// </summary>
        /// <remarks>
        /// The lengths are those of <see cref="DashPatterns.Lengths"/> under the theme's look, the same every backend strokes with:
        /// the library's own look writes its pattern as it stands (5,2), the matplotlib themes matplotlib's.
        /// </remarks>
        /// <param name="style">The dash style.</param>
        /// <param name="thickness">The width of the stroke, in pixels.</param>
        /// <param name="look">The dash look of the theme the stroke is drawn under.</param>
        public StringBuilder AppendDashArray(LineStyle style, double thickness, DashLook look)
        {
            var dashes = DashPatterns.Lengths(style, thickness, look);
            if (dashes.Count == 0) return sb;
            sb.Append(" stroke-dasharray=\"");
            for (int i = 0; i < dashes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.AppendSvgNumber(dashes[i]);
            }
            sb.Append('"');
            return sb;
        }

        // The view of an axes as the browser scripts read it: the plot area in pixels, the data range, the scale and the breaks of each
        // axis, and the range of every Y axis stacked past the primary. The members below write all of it, so the writer that opens a
        // series group, an axes view or a 3-D scene writes the same attributes by the same code, and a script in an open browser tab
        // reads the same names whichever of them it finds. The numbers keep every digit (AppendExactSvgNumber), because a script
        // inverts the pointer with them.



        /// <summary>The one writer of the plot area the browser scripts read: <c>data-plot-x/y/w/h</c>, with every digit. A view
        /// (<see cref="AppendView"/>) and a 3-D scene group both carry it.</summary>
        /// <param name="plot">The plot area in pixels.</param>
        /// <returns><paramref name="sb"/>, for chaining.</returns>
        internal StringBuilder AppendPlotArea(Rect plot) =>
            sb.AppendExactAttribute(SvgAttributes.PlotX, plot.X).AppendExactAttribute(SvgAttributes.PlotY, plot.Y)
              .AppendExactAttribute(SvgAttributes.PlotW, plot.Width).AppendExactAttribute(SvgAttributes.PlotH, plot.Height);

        /// <summary>The one writer of a view: the plot area, then the data range of <paramref name="range"/> when given. The
        /// range is in the space the axis is drawn in; a log or symlog axis also writes its scale (<c>data-xscale</c> /
        /// <c>data-yscale</c>, and the symlog threshold as <c>data-xlinthresh</c> / <c>data-ylinthresh</c>), and an axis with
        /// breaks writes them as they are cut out of that space (<c>data-xbreaks</c> / <c>data-ybreaks</c>), so the script
        /// can invert it. The inverse of the whole is <see cref="DataTransform.PixelToData"/>, the rule the script converts
        /// the pointer by. A linear axis without breaks writes neither, so its bytes are unchanged.</summary>
        /// <param name="plot">The plot area in pixels.</param>
        /// <param name="range">The transform the axes is drawn with, or <see langword="null"/> for an axes without a Cartesian
        /// range.</param>
        /// <returns><paramref name="sb"/>, for chaining.</returns>
        internal StringBuilder AppendView(Rect plot, DataTransform? range)
        {
            sb.AppendPlotArea(plot);
            if (range is null) return sb;
            sb.AppendExactAttribute(SvgAttributes.XMin, range.DataXMin).AppendExactAttribute(SvgAttributes.XMax, range.DataXMax)
              .AppendExactAttribute(SvgAttributes.YMin, range.DataYMin).AppendExactAttribute(SvgAttributes.YMax, range.DataYMax);
            sb.AppendViewScale('x', range.XRule, null);
            sb.AppendViewScale('y', range.YRule, null);
            sb.AppendViewBreaks('x', range.XBreaks);
            return sb.AppendViewBreaks('y', range.YBreaks);
        }

        /// <summary>Writes the own range (and scale, when not linear) of every Y axis stacked past the primary as attributes
        /// prefixed by its side and slot — <c>data-sec-right-0-ymin</c>, <c>data-sec-left-1-yscale</c>, and so on — plus
        /// a count per side (<c>data-sec-right-count</c>/<c>data-sec-left-count</c>) so the script knows how many to
        /// read without having to probe. The script converts a gesture through a specific slot's own range the same way
        /// <see cref="AppendView"/>'s primary range is converted. The numbering is OUTWARD only (the primary Y axis has no
        /// index here) and stays so: a browser tab that holds an older drawing sends it back.</summary>
        /// <param name="secondary">The own transform of every Y axis stacked past the primary, or <see langword="null"/> or
        /// empty for none, which writes nothing.</param>
        /// <returns><paramref name="sb"/>, for chaining.</returns>
        internal StringBuilder AppendSecondaryViews(IReadOnlyList<SecondaryAxisTransform>? secondary)
        {
            if (secondary is not { Count: > 0 }) return sb;

            int rightCount = 0, leftCount = 0;
            foreach (var s in secondary)
            {
                var slot = new StackedSlot(s.Side, s.SlotIndex);
                if (s.Side == AxisSide.Right) rightCount++; else leftCount++;
                sb.Append(' ').AppendViewPrefix(slot).Append(SvgAttributes.YMinStem).Append("=\"").AppendExactSvgNumber(s.Transform.DataYMin)
                  .Append("\" ").AppendViewPrefix(slot).Append(SvgAttributes.YMaxStem).Append("=\"").AppendExactSvgNumber(s.Transform.DataYMax).Append('"');
                sb.AppendViewScale('y', s.Transform.YRule, slot);
            }

            if (rightCount > 0) sb.Append(' ').Append(SvgAttributes.SecondaryRightCount).Append("=\"").AppendInvariant(rightCount).Append('"');
            if (leftCount > 0) sb.Append(' ').Append(SvgAttributes.SecondaryLeftCount).Append("=\"").AppendInvariant(leftCount).Append('"');
            return sb;
        }

        /// <summary>What an attribute name of a view starts with: <c>data-</c> for the primary axes, and
        /// <c>data-sec-{side}-{slot}-</c> for a Y axis stacked past it. Written in parts, never built as a string.</summary>
        internal StringBuilder AppendViewPrefix(StackedSlot? slot)
        {
            return slot is { } s
                ? sb.Append(SvgAttributes.SecondaryView).Append(s.Side == AxisSide.Right ? SvgAttributes.RightSide : SvgAttributes.LeftSide)
                    .Append('-').AppendInvariant(s.Index).Append('-')
                : sb.Append(SvgAttributes.View);
        }

        /// <summary>The one writer of an axis' scale in a view, when it is drawn in a space other than the data's own: the scale
        /// name and, for a symlog axis, the width of its linear stretch. The primary axes write them as <c>data-xscale</c> /
        /// <c>data-yscale</c>, a Y axis stacked past it with the side and slot in front (<c>data-sec-right-0-yscale</c>).</summary>
        internal StringBuilder AppendViewScale(char axis, ScaleRule scale, StackedSlot? slot)
        {
            if (scale.ViewName is not { } name) return sb;
            sb.Append(' ').AppendViewPrefix(slot).Append(axis).Append(SvgAttributes.ScaleStem).Append("=\"").Append(name).Append('"');
            if (scale.LinearThreshold is { } threshold)
                sb.Append(' ').AppendViewPrefix(slot).Append(axis).Append(SvgAttributes.LinThreshStem).Append("=\"").AppendExactSvgNumber(threshold).Append('"');
            return sb;
        }

        /// <summary>The one writer of an axis' breaks in a view: four numbers per break, in the order they begin — where it begins
        /// and ends in data units, the place it is cut in the drawn space, and the gap it removes there
        /// (<see cref="AxisBreakMap.Cuts"/>, <see cref="AxisBreakMap.Edges"/>) — what the script needs to read a pointer back
        /// past it and to place a value; an axis drawn in parts writes the room each break between two parts leaves as well
        /// (<c>data-xroom</c> / <c>data-yroom</c>, <see cref="AxisBreakMap.Room"/>). Nothing is written for an axis without breaks.</summary>
        internal StringBuilder AppendViewBreaks(char axis, AxisBreakMap? breaks)
        {
            if (breaks is null) return sb;
            sb.Append(' ').Append(SvgAttributes.View).Append(axis).Append(SvgAttributes.BreaksStem).Append("=\"");
            var cuts = breaks.Cuts;
            var edges = breaks.Edges;
            for (int i = 0; i < cuts.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.AppendExactSvgNumber(cuts[i].From).Append(' ').AppendExactSvgNumber(cuts[i].To).Append(' ')
                  .AppendExactSvgNumber(edges[i]).Append(' ').AppendExactSvgNumber(cuts[i].Gap);
            }
            sb.Append('"');

            // An axis drawn in parts writes the room each break between two parts leaves, so the script zooms in the space of the parts.
            if (breaks.IsDrawnInParts)
                sb.Append(' ').Append(SvgAttributes.View).Append(axis).Append(SvgAttributes.RoomStem).Append("=\"").AppendExactSvgNumber(breaks.Room).Append('"');
            return sb;
        }
    }

    extension(JoinStyle join)
    {
        /// <summary>The value of <c>stroke-linejoin</c> for the join: <c>round</c> or <c>bevel</c>; <see langword="null"/> for a mitre,
        /// which a browser draws when the attribute is left out, so no attribute is written.</summary>
        /// <returns>The name, or <see langword="null"/> when none is written.</returns>
        internal string? ToSvgName() => join switch
        {
            JoinStyle.Round => "round",
            JoinStyle.Bevel => "bevel",
            _ => null,
        };
    }
}
