// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one drawing of axis ticks on a surface. The primary axes, their mirrored side, the top X axis, the linked X axes below
/// the plot and every Y axis slot call it, so every tick honours its <see cref="TickConfig"/> by the same rule: mark length, width,
/// colour and direction, label size, colour, padding and visibility. What a label reads and how far it reaches is its
/// <see cref="TickDrawContext"/>'s; where a tick stands is its <see cref="TickRun"/>'s.</summary>
internal static class AxisTickExtensions
{
    /// <summary>How far an axis' offset text stands from the labels or the plot it is written beside, in points (matplotlib's
    /// <c>Axis.OFFSETTEXTPAD</c>).</summary>
    private const double OffsetTextPad = 3;

    extension(IRenderContext ctx)
    {
        /// <summary>Measures the tick labels <see cref="RenderAxisTicks"/> would draw, without drawing them, so a band can be
        /// reserved before anything is drawn: the same texts, font and visibility rule, and only the ticks the plot holds
        /// (<see cref="TickRun.PlaceOnPlot"/>).</summary>
        /// <param name="run">The tick values, where each stands and the edge they lie on.</param>
        /// <param name="config">The axis' major tick settings.</param>
        /// <param name="draw">Which edge, how the labels are formatted and the rotation they are drawn at.</param>
        /// <returns>What <see cref="RenderAxisTicks"/> returns for the same arguments: the farthest any label reaches past the
        /// mark and the pad (<see cref="TickDrawContext.LabelExtent"/>); 0 when <see cref="TickConfig.Visible"/> is false or the plot
        /// holds no tick.</returns>
        internal double MeasureAxisTicks(TickRun run, TickConfig config, TickDrawContext draw)
        {
            if (!config.Visible) return 0;
            var font = draw.LabelFont(config);
            var ticks = run.Ticks;
            double reach = config.OutsideLength + config.Pad;
            double maxDim = 0;
            for (int i = 0; i < ticks.Length; i++)
                if (run.PlaceOnPlot(ticks[i]) is not null)
                    maxDim = Math.Max(maxDim, draw.LabelExtent(draw.LabelSize(ctx, ticks, i, font)) - draw.Nearer(i, config, reach));
            return maxDim;
        }

        /// <summary>Draws the mark and the label of every tick of <paramref name="run"/> the plot holds on one edge, or the one of the
        /// two <paramref name="ink"/> names: the label reads the value, and the run's placement decides where it stands (the axis'
        /// data-to-pixel transform, a tick at its own value, or the placement of a linked axis, <see cref="LinkedTickPlacement"/>). A
        /// tick the plot does not hold (<see cref="TickRun.PlaceOnPlot"/>: outside the plot, or where the scale has no place) draws
        /// neither, by the rule its grid line and the minor ticks follow, as matplotlib draws only the ticks its view holds; a locator
        /// may return values past the ends of the range. A labelled minor tick is drawn in its look (<see cref="TickDrawContext.LookOf"/>),
        /// its mark beside its label. The marks alone are what a theme that marks every side draws on the top and right edges
        /// (matplotlib's <c>xtick.top</c> and <c>ytick.right</c> on, their labels off), and what a theme that places the marks among the
        /// data draws at their step.</summary>
        /// <param name="run">The tick values, in the axis' own unit, where each stands and the edge they lie on.</param>
        /// <param name="config">The axis' major tick settings.</param>
        /// <param name="draw">Which edge, where it is and how the labels are formatted.</param>
        /// <param name="ink">What is drawn: the labels, the marks, or both, each mark beside its label.</param>
        /// <returns>The farthest any label reaches past the mark and the pad (<see cref="TickDrawContext.LabelExtent"/>): on a
        /// <see cref="TickSide.Bottom"/>/<see cref="TickSide.Top"/> edge the tallest label, or the farthest-reaching turned
        /// one; on a <see cref="TickSide.Left"/>/<see cref="TickSide.Right"/> edge the widest; 0 when
        /// <see cref="TickConfig.Visible"/> is false or no label is drawn.</returns>
        internal double RenderAxisTicks(TickRun run, TickConfig config, TickDrawContext draw, TickInk ink = TickInk.Both)
        {
            if (!config.Visible) return 0;
            var edge = draw.Edge;
            bool labels = ink.DrawsLabels, marks = ink.DrawsMarks;
            var font = labels ? draw.LabelFont(config) : draw.BaseFont;
            double reach = config.OutsideLength + config.Pad;
            // The mark of each look, worked out once: the major ticks' and, when some ticks are labelled minor ones, theirs.
            var majorMark = marks ? TickMark.Of(config, draw.Foreground, draw.SpineHalf) : default;
            var minorMark = marks && draw.MinorLook is { } minorLook ? TickMark.Of(minorLook, draw.Foreground, draw.SpineHalf) : majorMark;
            var ticks = run.Ticks;
            double maxDim = 0;
            for (int i = 0; i < ticks.Length; i++)
            {
                double tick = ticks[i];
                if (run.PlaceOnPlot(tick) is not { } pos) continue;
                if (marks)
                    ctx.DrawTickMark(pos, draw.AxisEdge, edge, draw.IsMinorNamed(i) ? minorMark : majorMark);
                if (!labels) continue;

                var text = draw.LabelText(tick);
                double nearer = draw.Nearer(i, config, reach), at = reach - nearer;
                var size = draw.LabelSize(ctx, ticks, i, font, text);
                maxDim = Math.Max(maxDim, draw.LabelExtent(size) - nearer);
                ctx.Draw(TextLabel.Of(text), font, new TextPlacement(edge.LabelAnchor(pos, draw.AxisEdge, at), draw.Alignment)
                    { Vertical = edge.LabelVertical, Rotation = draw.LabelRotation }, size);
            }
            return maxDim;
        }

        /// <summary>Draws the minor tick marks of one axis, no labels, on the edge <paramref name="draw"/> hangs from, by its
        /// <see cref="TickConfig"/>: on the minor values between the major ticks of <paramref name="run"/> (the theme's
        /// <see cref="AxisTickLook.MinorValuesBetween"/>: its own spacing when it set one, else the minor ticks the axis' locator places
        /// (<see cref="TickDrawContext.ScaleMinors"/>, a logit axis'), else the split of the major step, no position a major tick stands
        /// on), each where the plot holds it (<see cref="TickRun.PlaceOnPlot"/>: never outside the plot, never where the scale has no
        /// place, as a log scale has none at or below 0 and the split runs a step below the first major tick). The split runs in the unit
        /// of the major values, and each minor stands where the run's placement puts its value, so a linked axis splits in its own unit
        /// and stands each minor where the primary shows its inverse. Every axis draws its minor ticks here, the X axis, the primary Y
        /// axis and every Y slot alike, and the minor grid stands on the same places.</summary>
        /// <param name="run">The major ticks, where they stand and the edge they lie on.</param>
        /// <param name="minor">The axis' minor tick settings; nothing is drawn when they are not visible.</param>
        /// <param name="draw">The edge, its side, half its spine and the foreground a mark without a colour takes.</param>
        internal void RenderMinorTicks(TickRun run, TickConfig minor, TickDrawContext draw)
        {
            if (!minor.Visible) return;
            var edge = draw.Edge;
            var mark = TickMark.Of(minor, draw.Foreground, draw.SpineHalf);
            foreach (double value in draw.Theme.Looks.Ticks.MinorValuesBetween(minor, run.Ticks, draw.ScaleMinors))
                if (run.PlaceOnPlot(value) is { } pos)
                    ctx.DrawTickMark(pos, draw.AxisEdge, edge, mark);
        }

        /// <summary>Writes the offset <paramref name="draw"/> takes off its labels once, at the end of its axis, as matplotlib
        /// places a <c>ScalarFormatter</c>'s offset text (<c>OFFSETTEXTPAD</c>, 3 points): below the labels of a bottom edge
        /// and above those of a top edge, ending at the plot's right; above the plot's top for a left or right edge, starting
        /// at its left or ending at its right. In the tick labels' font. Nothing when there is no offset.</summary>
        /// <param name="draw">The edge's draw context, which holds the offset.</param>
        /// <param name="config">The axis' major tick settings as drawn (the label font).</param>
        /// <param name="plot">The plot area.</param>
        /// <param name="tallestLabel">On a top or bottom edge, the tallest label drawn there.</param>
        internal void RenderTickOffset(TickDrawContext draw, TickConfig config, Rect plot, double tallestLabel)
        {
            if ((draw.Offset == 0 && draw.OrderOfMagnitude == 0) || !config.Visible) return;
            var font = draw.LabelFont(config);
            string text = draw.Theme.Looks.TickLabels.Signed(TickLabelExtensions.ScaleText(draw.OrderOfMagnitude, draw.Offset));
            var at = draw.Edge.OffsetTextAt(plot, draw.AxisEdge, config.TickLabelBand(tallestLabel), OffsetTextPad.PointsInPixels);
            ctx.Draw(TextLabel.Of(text), font, at);
        }

        /// <summary>Draws one tick <paramref name="mark"/> on <paramref name="edge"/>, as far out of the plot and into it as its look's
        /// direction says (<see cref="TickMark.Drawn"/>). The end that meets the spine reaches half the spine past the edge (into the plot
        /// for an Out mark, beyond the edge for an In mark), so the mark touches the spine stroke. A mark shorter than a tenth of a pixel
        /// is not drawn.
        /// <para>On a surface with a pixel grid that was told looks that ask (the matplotlib themes), the mark is drawn as matplotlib draws
        /// a tick: a marker hung from the point where the tick meets the edge, as long as it is given (it does not reach into the spine),
        /// with that point rounded to a whole pixel and the marker's stroke snapped by its width
        /// (<see cref="RenderContextPixelGridExtensions"/>' <c>MarkerOnPixelGrid</c>, the surface the one gate).</para></summary>
        /// <param name="tickPos">The pixel coordinate along the edge.</param>
        /// <param name="axisEdge">The pixel coordinate of the edge.</param>
        /// <param name="edge">The edge: which way is out from it, and whether it runs along X.</param>
        /// <param name="mark">The mark of the tick's look (<see cref="TickMark.Of"/>): its stroke and how far it reaches.</param>
        internal void DrawTickMark(double tickPos, double axisEdge, EdgeTerms edge, in TickMark mark)
        {
            // +1 when "away from the plot" is the growing pixel coordinate (bottom: down, right: rightward).
            double outward = edge.Outward;
            double a = axisEdge + outward * mark.Drawn.Outer;
            double b = axisEdge - outward * mark.Drawn.Inner;
            // Drawn from the smaller coordinate to the larger, the order every mark has always been emitted in.
            double from = Math.Min(a, b), to = Math.Max(a, b);
            if (to - from <= 0.1) return;
            bool alongX = edge.AlongX;
            // matplotlib's tick marker (TICKDOWN and the rest, '|' across the edge) runs from the edge, not from inside the spine.
            double markerA = axisEdge + outward * mark.Marker.Outer, markerB = axisEdge - outward * mark.Marker.Inner;
            var anchor = Point.Along(alongX, tickPos, axisEdge);
            var line = ctx.MarkerOnPixelGrid(anchor, Across(Math.Min(markerA, markerB), Math.Max(markerA, markerB)), Across(from, to),
                mark.Stroke.Thickness);
            ctx.DrawLine(line.From, line.To, mark.Stroke);

            SeriesRenderers.PixelSegment Across(double start, double end) =>
                new(Point.Along(alongX, tickPos, start), Point.Along(alongX, tickPos, end));
        }
    }
}
