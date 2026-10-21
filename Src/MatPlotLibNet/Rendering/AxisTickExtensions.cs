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
}

/// <summary>The one drawing of axis ticks. The primary axes, their mirrored side and both secondary axes call it, so
/// every tick honours its <see cref="TickConfig"/> by the same rule: mark length, width, colour and direction, label
/// size, colour, padding and visibility.</summary>
internal static class AxisTickExtensions
{
    /// <summary>The font a tick label is drawn in: <paramref name="baseFont"/> with the config's
    /// <see cref="TickConfig.LabelSize"/> and <see cref="TickConfig.LabelColor"/> applied when set.</summary>
    /// <param name="config">The axis' major tick settings.</param>
    /// <param name="baseFont">The theme's tick font.</param>
    /// <returns>The label font.</returns>
    internal static Font LabelFont(this TickConfig config, Font baseFont)
    {
        var font = baseFont;
        if (config.LabelSize.HasValue) font = font with { Size = config.LabelSize.Value };
        if (config.LabelColor.HasValue) font = font with { Color = config.LabelColor };
        return font;
    }

    /// <summary>From an edge to the far side of the tick labels drawn on it: the mark, the pad and the label extent (the
    /// widest label on a left or right edge, the tallest on a top or bottom edge). The layout reserves this band with
    /// the same rule the drawing places the labels by.</summary>
    /// <param name="config">The axis' major tick settings.</param>
    /// <param name="labelExtent">The widest or tallest label drawn on that edge, 0 when none was drawn.</param>
    /// <returns>The band in pixels; 0 when no label was drawn.</returns>
    internal static double TickLabelBand(this TickConfig config, double labelExtent) =>
        labelExtent > 0 ? config.Length + config.Pad + labelExtent : 0;

    /// <summary>How far an axis title sits from its edge: past the mark, the pad, the widest tick label and a 12 px
    /// gap, or the fixed 45 px when no tick label was drawn. The primary and the secondary Y axis share this rule.</summary>
    /// <param name="config">The axis' major tick settings.</param>
    /// <param name="widestTickLabel">The widest tick label drawn on that edge, 0 when none was drawn.</param>
    /// <returns>The distance from the edge to the centre of the rotated axis title.</returns>
    internal static double AxisLabelOffset(this TickConfig config, double widestTickLabel) =>
        widestTickLabel > 0 ? config.TickLabelBand(widestTickLabel) + 12 : 45;

    /// <summary>The gap between the tick labels on a top or bottom edge and the horizontal axis title beside them.</summary>
    internal const double XLabelGap = 6;

    /// <summary>How far the baseline of a horizontal axis title drawn above a top edge sits from that edge: past the
    /// tick label band (<see cref="TickLabelBand"/>), the <see cref="XLabelGap"/> and the title's descent (a fifth of
    /// its size), so the bottom of the title clears the top of the tick labels. The secondary X axis places its title
    /// here, and its band (what the axes title rises by and the layout reserves) reaches the top of that title.</summary>
    /// <param name="config">The axis' major tick settings.</param>
    /// <param name="tallestTickLabel">The tallest tick label drawn on that edge, 0 when none was drawn.</param>
    /// <param name="labelSize">The font size of the axis title.</param>
    /// <returns>The distance from the edge to the title's baseline.</returns>
    internal static double TopAxisLabelOffset(this TickConfig config, double tallestTickLabel, double labelSize) =>
        config.TickLabelBand(tallestTickLabel) + XLabelGap + 0.2 * labelSize;

    /// <summary>The text of the label of <paramref name="tick"/>: the axis' formatter, or the uniform format.</summary>
    private static string LabelText(this TickDrawContext draw, double tick) =>
        draw.Formatter?.Format(tick) ?? draw.UniformFormat(tick);

    /// <summary>Measures the tick labels <see cref="RenderAxisTicks"/> would draw, without drawing them, so a band can be
    /// reserved before anything is drawn: the same texts, font and visibility rule.</summary>
    /// <param name="ctx">The context text is measured with.</param>
    /// <param name="ticks">The tick values, in data space.</param>
    /// <param name="config">The axis' major tick settings.</param>
    /// <param name="draw">Which edge and how the labels are formatted; positions are not read.</param>
    /// <returns>What <see cref="RenderAxisTicks"/> returns for the same arguments: the tallest label on a top or bottom
    /// edge, the widest on a left or right edge; 0 when <see cref="TickConfig.Visible"/> is false.</returns>
    internal static double MeasureAxisTicks(this IRenderContext ctx, double[] ticks, TickConfig config, TickDrawContext draw)
    {
        if (!config.Visible) return 0;
        bool alongX = draw.Side is TickSide.Bottom or TickSide.Top;
        var font = config.LabelFont(draw.BaseFont);
        double maxDim = 0;
        foreach (var tick in ticks)
        {
            var size = ctx.MeasureText(draw.LabelText(tick), font);
            maxDim = Math.Max(maxDim, alongX ? size.Height : size.Width);
        }
        return maxDim;
    }

    /// <summary>Draws the mark and the label of every tick in <paramref name="ticks"/> on one edge.</summary>
    /// <param name="ctx">The context to draw into.</param>
    /// <param name="ticks">The tick values, in data space.</param>
    /// <param name="fixedCoord">The data value on the other axis the ticks are mapped at; only the coordinate along the
    /// edge is used.</param>
    /// <param name="transform">The data-to-pixel transform of the axis.</param>
    /// <param name="config">The axis' major tick settings.</param>
    /// <param name="draw">Which edge, where it is and how the labels are formatted.</param>
    /// <returns>The tallest label (<see cref="TickSide.Bottom"/>/<see cref="TickSide.Top"/>) or the widest
    /// (<see cref="TickSide.Left"/>/<see cref="TickSide.Right"/>); 0 when <see cref="TickConfig.Visible"/> is false.</returns>
    internal static double RenderAxisTicks(this IRenderContext ctx, double[] ticks, double fixedCoord,
        DataTransform transform, TickConfig config, TickDrawContext draw)
    {
        if (!config.Visible) return 0;
        bool alongX = draw.Side is TickSide.Bottom or TickSide.Top;
        var font = config.LabelFont(draw.BaseFont);
        var color = config.Color ?? draw.Foreground;
        double reach = config.Length + config.Pad;
        double maxDim = 0;
        foreach (var tick in ticks)
        {
            var text = draw.LabelText(tick);
            var size = ctx.MeasureText(text, font);
            if (alongX)
            {
                double x = transform.DataToPixel(tick, fixedCoord).X;
                ctx.DrawTickMark(x, draw.AxisEdge, draw.Side, config.Length, color, config.Width, config.Direction, draw.SpineHalf);
                double labelY = draw.Side == TickSide.Bottom
                    ? draw.AxisEdge + reach + font.Size
                    : draw.AxisEdge - reach;
                if (size.Height > maxDim) maxDim = size.Height;
                ctx.DrawText(text, new Point(x, labelY), font, draw.Alignment, draw.LabelRotation);
            }
            else
            {
                double y = transform.DataToPixel(fixedCoord, tick).Y;
                ctx.DrawTickMark(y, draw.AxisEdge, draw.Side, config.Length, color, config.Width, config.Direction, draw.SpineHalf);
                double labelX = draw.Side == TickSide.Right
                    ? draw.AxisEdge + reach
                    : draw.AxisEdge - reach;
                if (size.Width > maxDim) maxDim = size.Width;
                ctx.DrawText(text, new Point(labelX, y + 4), font, draw.Alignment, draw.LabelRotation);
            }
        }
        return maxDim;
    }

    /// <summary>Draws one tick mark on <paramref name="side"/>. <see cref="TickDirection.Out"/> points away from the
    /// plot, <see cref="TickDirection.In"/> into it, <see cref="TickDirection.InOut"/> both ways. The end that meets the
    /// spine reaches <paramref name="spineHalfWidth"/> past the edge (into the plot for an Out mark, beyond the edge for
    /// an In mark), so the mark touches the spine stroke. A mark shorter than a tenth of a pixel is not drawn.</summary>
    /// <param name="ctx">The context to draw into.</param>
    /// <param name="tickPos">The pixel coordinate along the edge.</param>
    /// <param name="axisEdge">The pixel coordinate of the edge.</param>
    /// <param name="side">The edge.</param>
    /// <param name="length">The mark length in pixels.</param>
    /// <param name="color">The mark colour.</param>
    /// <param name="width">The stroke width.</param>
    /// <param name="direction">Which way the mark points.</param>
    /// <param name="spineHalfWidth">Half the spine stroke on that edge.</param>
    internal static void DrawTickMark(this IRenderContext ctx, double tickPos, double axisEdge, TickSide side,
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
