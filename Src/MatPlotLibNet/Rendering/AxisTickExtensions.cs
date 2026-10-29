// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The edge of the plot area an axis hangs its ticks from.</summary>
internal enum TickSide
{
    /// <summary>The primary X axis: marks run along the bottom edge, labels sit below it.</summary>
    Bottom,

    /// <summary>The primary Y axis: marks run along the left edge, labels sit to its left.</summary>
    Left,

    /// <summary>The secondary Y axis or the mirrored primary Y ticks: labels sit to the right of the right edge.</summary>
    Right,

    /// <summary>The secondary X axis or the mirrored primary X ticks: labels sit above the top edge.</summary>
    Top,
}

/// <summary>Where and how one axis' major ticks are drawn. The <see cref="TickConfig"/> carries what the caller asked
/// for; this carries what the drawing site knows (which edge, where it is, how its labels are formatted).</summary>
internal readonly struct TickDrawContext
{
    /// <summary>The edge the ticks hang from; decides which way "out" points and where the labels go.</summary>
    public TickSide Side { get; init; }

    /// <summary>The pixel coordinate of that edge: a Y for <see cref="TickSide.Bottom"/>/<see cref="TickSide.Top"/>,
    /// an X for <see cref="TickSide.Left"/>/<see cref="TickSide.Right"/>.</summary>
    public double AxisEdge { get; init; }

    /// <summary>Half the spine stroke on that edge. The end of a mark that meets the spine reaches this far past the
    /// edge, so the mark touches the stroke whatever the subpixel rounding (matplotlib draws ticks through the spine).</summary>
    public double SpineHalf { get; init; }

    /// <summary>The theme's tick font, before <see cref="TickConfig.LabelSize"/> and <see cref="TickConfig.LabelColor"/>.</summary>
    public Font BaseFont { get; init; }

    /// <summary>The axis' own formatter; when <see langword="null"/> <see cref="UniformFormat"/> formats the label.</summary>
    public ITickFormatter? Formatter { get; init; }

    /// <summary>The formatter shared by all ticks of the axis, used when <see cref="Formatter"/> is null.</summary>
    public Func<double, string> UniformFormat { get; init; }

    /// <summary>The colour a mark takes when <see cref="TickConfig.Color"/> is not set: the theme's foreground.</summary>
    public Color Foreground { get; init; }

    /// <summary>The rotation the labels are drawn at, in degrees (the caller may have resolved an automatic one).</summary>
    public double LabelRotation { get; init; }

    /// <summary>How a label is anchored at its point.</summary>
    public TextAlignment Alignment { get; init; }

    /// <summary>The upright size of each tick's label in the font it is drawn in, in the order of the ticks this context
    /// is used with; <see langword="null"/> to measure each label while drawing or measuring. A site that measured the
    /// labels to decide how to draw them (the X labels' rotation) hands the sizes on, so they are measured once. Read only
    /// when it holds one size per tick.</summary>
    public IReadOnlyList<Size>? LabelSizes { get; init; }
}

/// <summary>How far one tick label reaches once turned: away from the edge it hangs from, and before and after its anchor
/// along the edge (<see cref="AxisTickExtensions"/>' <c>Reach</c>).</summary>
/// <param name="Away">From the line past the mark and the pad to the far side of the label, away from the edge.</param>
/// <param name="Before">How far the label stands before its anchor along the edge: to the left on a top or bottom edge, above
/// on a left or right one; never negative.</param>
/// <param name="After">How far it stands after its anchor along the edge; never negative.</param>
internal readonly record struct LabelReach(double Away, double Before, double After);

/// <summary>The major ticks of one axis as they stand on the plot: what the minor ticks between them are placed by.</summary>
/// <param name="Ticks">The major tick values, in data space.</param>
/// <param name="FixedCoord">The data value on the other axis the ticks are mapped at; only the coordinate along the edge
/// is used.</param>
/// <param name="Transform">The data-to-pixel transform of the axis; its plot bounds clip the minor ticks.</param>
internal readonly record struct TickRun(double[] Ticks, double FixedCoord, DataTransform Transform);

/// <summary>The one drawing of axis ticks. The primary axes, their mirrored side and every Y axis slot call it, so
/// every tick honours its <see cref="TickConfig"/> by the same rule: mark length, width, colour and direction, label
/// size, colour, padding and visibility.</summary>
internal static class AxisTickExtensions
{
    extension(TickConfig config)
    {
        /// <summary>The font a tick label is drawn in: <paramref name="baseFont"/> with the config's
        /// <see cref="TickConfig.LabelSize"/> and <see cref="TickConfig.LabelColor"/> applied when set.</summary>
        /// <param name="baseFont">The theme's tick font.</param>
        /// <returns>The label font.</returns>
        internal Font LabelFont(Font baseFont)
        {
            var font = baseFont;
            if (config.LabelSize.HasValue) font = font with { Size = config.LabelSize.Value };
            if (config.LabelColor.HasValue) font = font with { Color = config.LabelColor };
            return font;
        }

        /// <summary>From an edge to the far side of the tick labels drawn on it: the mark, the pad and the label extent (the
        /// widest label on a left or right edge, the tallest on a top or bottom edge). The layout reserves this band with
        /// the same rule the drawing places the labels by.</summary>
        /// <param name="labelExtent">The widest or tallest label drawn on that edge, 0 when none was drawn.</param>
        /// <returns>The band in pixels; 0 when no label was drawn.</returns>
        internal double TickLabelBand(double labelExtent) =>
            labelExtent > 0 ? config.Length + config.Pad + labelExtent : 0;

        /// <summary>How far the baseline of a horizontal axis title drawn above a top edge sits from that edge: the one
        /// facing-edge rule (<see cref="TitleOffset"/>) with the <see cref="XLabelGap"/>, so the title's descent (a fifth of
        /// its size, the ink that faces the labels) stands the gap past the tick label band (<see cref="TickLabelBand"/>).
        /// The secondary X axis places its title here, and its band (what the axes title rises by and the layout reserves)
        /// reaches the top of that title.</summary>
        /// <param name="tallestTickLabel">The tallest tick label drawn on that edge, 0 when none was drawn.</param>
        /// <param name="labelSize">The font size of the axis title.</param>
        /// <returns>The distance from the edge to the title's baseline.</returns>
        internal double TopAxisLabelOffset(double tallestTickLabel, double labelSize) =>
            new TitleClearance(config.TickLabelBand(tallestTickLabel), XLabelGap, labelSize, FacingInk.Of(TickSide.Top))
                .TitleOffset();

        /// <summary>How far the baseline of a horizontal axis title drawn below a bottom edge sits from that edge: the one
        /// facing-edge rule (<see cref="TitleOffset"/>) with the <see cref="XLabelGap"/>, so the title's ascent (four fifths
        /// of its size, the ink that faces the labels) stands the gap past the tick label band (<see cref="TickLabelBand"/>),
        /// whether the labels read numbers or category names; the fixed <see cref="BottomAxisLabelFallback"/> when no tick
        /// label was drawn. The mirror of <see cref="TopAxisLabelOffset"/>: the primary X axis places its title here from the
        /// band it measured while drawing its tick labels, and the layout reserves the band, the gap and the title's height
        /// below it.</summary>
        /// <param name="tallestTickLabel">The tallest tick label drawn on that edge, 0 when none was drawn.</param>
        /// <param name="labelSize">The font size of the axis title.</param>
        /// <returns>The distance from the edge to the title's baseline.</returns>
        internal double BottomAxisLabelOffset(double tallestTickLabel, double labelSize) =>
            new TitleClearance(config.TickLabelBand(tallestTickLabel), XLabelGap, labelSize, FacingInk.Of(TickSide.Bottom))
            {
                WithoutTickLabels = BottomAxisLabelFallback,
            }.TitleOffset();

        /// <summary>From a bottom edge to the far side of the horizontal axis title drawn below it, the room the layout keeps
        /// for it: the title's top stands where <see cref="BottomAxisLabelOffset"/> puts its baseline less its ascent (four
        /// fifths of its size), so past the tick label band and the <see cref="XLabelGap"/>, or at the fixed offset less the
        /// ascent when no tick label was drawn; then the pad of the title's style and the title's full height (the safe
        /// direction). The renderer that draws the title measures it with this rule, and the layout reserves what it
        /// measured.</summary>
        /// <param name="tallestTickLabel">The tallest tick label drawn on that edge, 0 when none was drawn.</param>
        /// <param name="labelSize">The font size of the axis title.</param>
        /// <param name="titleHeight">The measured height of the axis title in its font.</param>
        /// <param name="pad">The pad of the title's style (<see cref="TextStyle.Pad"/>), 0 when none.</param>
        /// <returns>The distance from the edge to the far side of the title.</returns>
        internal double BottomAxisTitleExtent(double tallestTickLabel, double labelSize,
            double titleHeight, double pad) =>
            tallestTickLabel > 0
                ? config.TickLabelBand(tallestTickLabel) + XLabelGap + pad + titleHeight
                : BottomAxisLabelFallback - FacingInk.Of(TickSide.Bottom).Facing * labelSize + pad + titleHeight;
    }

    extension(TitleClearance clearance)
    {
        /// <summary>The one rule for where an axis title stands beside the tick labels of its edge, on all four edges: its
        /// placing line (the baseline of a horizontal title, the anchor line of a turned Y title) stands past the tick label
        /// band by the gap and the part of the title's ink that faces the labels (<see cref="FacingInk.Facing"/>), so that
        /// ink edge stands exactly the gap past the band whichever way the title faces. When no tick label is drawn and the
        /// edge has a fixed place (<see cref="TitleClearance.WithoutTickLabels"/>), the title stands there.</summary>
        /// <returns>The distance from the edge to the title's placing line.</returns>
        internal double TitleOffset() =>
            clearance.TickLabelBand > 0 || clearance.WithoutTickLabels is not { } fixedOffset
                ? clearance.TickLabelBand + clearance.Gap + clearance.Ink.Facing * clearance.LabelSize
                : fixedOffset;
    }

    /// <summary>The gap between the tick labels on a top or bottom edge and the horizontal axis title beside them.</summary>
    internal const double XLabelGap = 6;

    /// <summary>Where the baseline of a horizontal axis title below a bottom edge stands when no tick label was drawn there
    /// (hidden ticks, axis-less series): this far from the edge.</summary>
    internal const double BottomAxisLabelFallback = 35;

    extension(TickDrawContext draw)
    {
        /// <summary>The text of the label of <paramref name="tick"/>: the axis' formatter, or the uniform format.</summary>
        internal string LabelText(double tick) =>
            draw.Formatter?.Format(tick) ?? draw.UniformFormat(tick);

        /// <summary>The upright size of the label of tick <paramref name="index"/> of <paramref name="ticks"/> in
        /// <paramref name="font"/>: the size the context carries (<see cref="TickDrawContext.LabelSizes"/>) when it holds
        /// one per tick, otherwise measured.</summary>
        private Size LabelSize(IRenderContext ctx, double[] ticks, int index, Font font) =>
            draw.LabelSizes is { } sizes && sizes.Count == ticks.Length
                ? sizes[index]
                : ctx.MeasureText(draw.LabelText(ticks[index]), font);

        /// <summary>How far one tick label reaches, as it is drawn, by the one rule for all four edges and every rotation: the
        /// label's box (<see cref="TextBox"/>, the font size above the baseline, the rest of the measured height below it, in
        /// the label's alignment) turned about its anchor by the label's rotation. <see cref="LabelReach.Away"/> is how far it
        /// stands from the edge it hangs from, past the mark and the pad; <see cref="LabelReach.Before"/> and
        /// <see cref="LabelReach.After"/> how far it stands before and after its anchor along the edge. Upright, a label on a
        /// top or bottom edge reaches exactly its height and one on a left or right edge exactly its width, the band every
        /// upright label is reserved, so a figure whose labels are not turned is laid out to the bit as before.</summary>
        /// <param name="size">The label's measured size, upright.</param>
        /// <param name="fontSize">The label's font size: how far above its baseline the letters rise.</param>
        /// <returns>The label's reach, in pixels.</returns>
        internal LabelReach Reach(Size size, double fontSize)
        {
            var box = draw.TurnedBox(size, fontSize, draw.LabelRotation);
            bool alongX = draw.Side is TickSide.Bottom or TickSide.Top;
            double before = Math.Max(0, alongX ? -box.X : -box.Y);
            double after = Math.Max(0, alongX ? box.Right : box.Bottom);
            if (draw.LabelRotation == 0)
                return new LabelReach(alongX ? size.Height : size.Width, before, after);

            double away = draw.Side switch
            {
                TickSide.Bottom => size.Height + (box.Bottom - draw.TurnedBox(size, fontSize, 0).Bottom),
                TickSide.Top    => size.Height + (-box.Y + draw.TurnedBox(size, fontSize, 0).Y),
                TickSide.Left   => -box.X,
                _               => box.Right,
            };
            return new LabelReach(away, before, after);
        }

        /// <summary>The box of a label of <paramref name="size"/> turned by <paramref name="degrees"/> about its anchor, at the
        /// anchor (the origin), in the label's alignment: the one box every reach is read off.</summary>
        private Rect TurnedBox(Size size, double fontSize, double degrees) =>
            new TextBox(new Point(0, 0), size, fontSize, draw.Alignment, TextVerticalAlignment.Baseline, degrees).Bounds();

        /// <summary>How far one tick label reaches past the mark and the pad, away from the edge it hangs from, as it is drawn
        /// (<see cref="Reach"/>): the tallest label's height or the widest label's width upright, and what the turn carries it
        /// to when it is turned. The drawing (<see cref="RenderAxisTicks"/>) and the measuring (<see cref="MeasureAxisTicks"/>)
        /// take it from here, so the band the layout reserves for turned labels and the room the axis title clears are the
        /// drawn ones.</summary>
        /// <param name="size">The label's measured size, upright.</param>
        /// <param name="fontSize">The label's font size: how far below the reach line a bottom label's baseline stands.</param>
        /// <returns>The label's extent away from the edge, in pixels.</returns>
        internal double LabelExtent(Size size, double fontSize) => draw.Reach(size, fontSize).Away;
    }

    extension(IRenderContext ctx)
    {
        /// <summary>Measures the tick labels <see cref="RenderAxisTicks"/> would draw, without drawing them, so a band can be
        /// reserved before anything is drawn: the same texts, font and visibility rule.</summary>
        /// <param name="ticks">The tick values, in data space.</param>
        /// <param name="config">The axis' major tick settings.</param>
        /// <param name="draw">Which edge, how the labels are formatted and the rotation they are drawn at; positions are not
        /// read.</param>
        /// <returns>What <see cref="RenderAxisTicks"/> returns for the same arguments: the farthest any label reaches past the
        /// mark and the pad (<see cref="LabelExtent"/>); 0 when <see cref="TickConfig.Visible"/> is false.</returns>
        internal double MeasureAxisTicks(double[] ticks, TickConfig config, TickDrawContext draw)
        {
            if (!config.Visible) return 0;
            var font = config.LabelFont(draw.BaseFont);
            double maxDim = 0;
            for (int i = 0; i < ticks.Length; i++)
                maxDim = Math.Max(maxDim, draw.LabelExtent(draw.LabelSize(ctx, ticks, i, font), font.Size));
            return maxDim;
        }

        /// <summary>Draws the mark and the label of every tick in <paramref name="ticks"/> on one edge.</summary>
        /// <param name="ticks">The tick values, in data space.</param>
        /// <param name="fixedCoord">The data value on the other axis the ticks are mapped at; only the coordinate along the
        /// edge is used.</param>
        /// <param name="transform">The data-to-pixel transform of the axis.</param>
        /// <param name="config">The axis' major tick settings.</param>
        /// <param name="draw">Which edge, where it is and how the labels are formatted.</param>
        /// <returns>The farthest any label reaches past the mark and the pad (<see cref="LabelExtent"/>): on a
        /// <see cref="TickSide.Bottom"/>/<see cref="TickSide.Top"/> edge the tallest label, or the farthest-reaching turned
        /// one; on a <see cref="TickSide.Left"/>/<see cref="TickSide.Right"/> edge the widest; 0 when
        /// <see cref="TickConfig.Visible"/> is false.</returns>
        internal double RenderAxisTicks(double[] ticks, double fixedCoord,
            DataTransform transform, TickConfig config, TickDrawContext draw)
        {
            if (!config.Visible) return 0;
            bool alongX = draw.Side is TickSide.Bottom or TickSide.Top;
            var font = config.LabelFont(draw.BaseFont);
            var color = config.Color ?? draw.Foreground;
            double reach = config.Length + config.Pad;
            double maxDim = 0;
            for (int i = 0; i < ticks.Length; i++)
            {
                double tick = ticks[i];
                var text = draw.LabelText(tick);
                maxDim = Math.Max(maxDim, draw.LabelExtent(draw.LabelSize(ctx, ticks, i, font), font.Size));
                if (alongX)
                {
                    double x = transform.DataToPixel(tick, fixedCoord).X;
                    ctx.DrawTickMark(x, draw.AxisEdge, draw.Side, config.Length, color, config.Width, config.Direction, draw.SpineHalf);
                    double labelY = draw.Side == TickSide.Bottom
                        ? draw.AxisEdge + reach + font.Size
                        : draw.AxisEdge - reach;
                    ctx.DrawText(text, new Point(x, labelY), font, draw.Alignment, draw.LabelRotation);
                }
                else
                {
                    double y = transform.DataToPixel(fixedCoord, tick).Y;
                    ctx.DrawTickMark(y, draw.AxisEdge, draw.Side, config.Length, color, config.Width, config.Direction, draw.SpineHalf);
                    double labelX = draw.Side == TickSide.Right
                        ? draw.AxisEdge + reach
                        : draw.AxisEdge - reach;
                    ctx.DrawText(text, new Point(labelX, y + 4), font, draw.Alignment, draw.LabelRotation);
                }
            }
            return maxDim;
        }

        /// <summary>Draws the minor tick marks of one axis, no labels, on the edge <paramref name="draw"/> hangs from, by its
        /// <see cref="TickConfig"/>: between the major ticks of <paramref name="run"/>, its own spacing when it set one and
        /// matplotlib's five-way subdivision otherwise, skipping a position a major tick stands on and one outside the plot.
        /// Every axis draws its minor ticks here, the X axis, the primary Y axis and every Y slot alike.</summary>
        /// <param name="run">The major ticks, the coordinate they are mapped at and the transform.</param>
        /// <param name="minor">The axis' minor tick settings; nothing is drawn when they are not visible.</param>
        /// <param name="draw">The edge, its side, half its spine and the foreground a mark without a colour takes.</param>
        internal void RenderMinorTicks(TickRun run, TickConfig minor, TickDrawContext draw)
        {
            var majorTicks = run.Ticks;
            if (!minor.Visible || majorTicks.Length < 2) return;
            bool alongX = draw.Side is TickSide.Bottom or TickSide.Top;
            var color = minor.Color ?? draw.Foreground;
            double majorStep = majorTicks[1] - majorTicks[0];
            // The caller's own spacing wins when it set one; otherwise matplotlib's five-way subdivision, which is what
            // every axis that has not asked for anything still gets. A non-positive value is a computed spacing that came
            // out wrong, never an instruction — it falls back rather than dividing by zero.
            double minorStep = minor.Spacing is { } spacing && spacing > 0 ? spacing : majorStep / 5;
            double rangeStart = majorTicks[0] - majorStep;
            double rangeEnd   = majorTicks[^1] + majorStep;
            var bounds = run.Transform.PlotBounds;
            double areaMin = alongX ? bounds.X : bounds.Y;
            double areaMax = alongX ? bounds.X + bounds.Width : bounds.Y + bounds.Height;
            for (double mt = rangeStart; mt <= rangeEnd + minorStep * 0.01; mt += minorStep)
            {
                mt = Math.Round(mt, 10);
                if (Array.Exists(majorTicks, t => Math.Abs(t - mt) < minorStep * 0.01)) continue;
                var pt = alongX ? run.Transform.DataToPixel(mt, run.FixedCoord) : run.Transform.DataToPixel(run.FixedCoord, mt);
                double pos = alongX ? pt.X : pt.Y;
                if (pos < areaMin || pos > areaMax) continue;
                ctx.DrawTickMark(pos, draw.AxisEdge, draw.Side, minor.Length, color, minor.Width, minor.Direction, draw.SpineHalf);
            }
        }

        /// <summary>Draws one tick mark on <paramref name="side"/>. <see cref="TickDirection.Out"/> points away from the
        /// plot, <see cref="TickDirection.In"/> into it, <see cref="TickDirection.InOut"/> both ways. The end that meets the
        /// spine reaches <paramref name="spineHalfWidth"/> past the edge (into the plot for an Out mark, beyond the edge for
        /// an In mark), so the mark touches the spine stroke. A mark shorter than a tenth of a pixel is not drawn.</summary>
        /// <param name="tickPos">The pixel coordinate along the edge.</param>
        /// <param name="axisEdge">The pixel coordinate of the edge.</param>
        /// <param name="side">The edge.</param>
        /// <param name="length">The mark length in pixels.</param>
        /// <param name="color">The mark colour.</param>
        /// <param name="width">The stroke width.</param>
        /// <param name="direction">Which way the mark points.</param>
        /// <param name="spineHalfWidth">Half the spine stroke on that edge.</param>
        internal void DrawTickMark(double tickPos, double axisEdge, TickSide side,
            double length, Color color, double width, TickDirection direction, double spineHalfWidth = 0.0)
        {
            // +1 when "away from the plot" is the growing pixel coordinate (bottom: down, right: rightward).
            double outward = side is TickSide.Bottom or TickSide.Right ? 1 : -1;
            double outer = direction == TickDirection.In ? spineHalfWidth : length;
            double inner = direction == TickDirection.Out ? spineHalfWidth : length;
            double a = axisEdge + outward * outer;
            double b = axisEdge - outward * inner;
            // Drawn from the smaller coordinate to the larger, the order every mark has always been emitted in.
            double from = Math.Min(a, b), to = Math.Max(a, b);
            if (to - from <= 0.1) return;
            var stroke = new StrokeStyle(color, width, LineStyle.Solid);
            if (side is TickSide.Bottom or TickSide.Top)
                ctx.DrawLine(new Point(tickPos, from), new Point(tickPos, to), stroke);
            else
                ctx.DrawLine(new Point(from, tickPos), new Point(to, tickPos), stroke);
        }
    }
}
