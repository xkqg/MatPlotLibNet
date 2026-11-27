// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.MathText;
using MatPlotLibNet.Rendering.TextMeasurement;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>What a colour bar has whatever room it takes (<see cref="ColorBarRoom"/>): which room the figure's bars take
/// (<c>figure.BarRoom</c>), the part of a cell the axes keep when the bar takes its room from it (matplotlib's <c>fraction</c> and
/// <c>pad</c>), how far the bar's text reaches past its body, and how far the figure's own bar stands clear of what the subplots on its
/// side draw. A width or a gap the author gives wins under every room.</summary>
internal static class ColorBarRoomExtensions
{
    /// <summary>The share of the cell the bar's slot takes (matplotlib's <c>fraction</c>).</summary>
    internal const double Fraction = 0.15;

    /// <summary>The share of the cell's width between the axes and a bar beside them (matplotlib's <c>pad</c> for the right).</summary>
    internal const double PadBeside = 0.05;

    /// <summary>The share of the cell's height between the axes and a bar below them (matplotlib's <c>pad</c> for the bottom).</summary>
    internal const double PadBelow = 0.15;

    /// <summary>The share of the axes' size between the axes and the bar on either side under the constrained layout (matplotlib's
    /// <c>pad0 = 0.05 if fig.get_constrained_layout()</c>).</summary>
    internal const double ConstrainedPad = 0.05;

    /// <summary>The gap, in pixels, between the line of a bar's tick labels and the baseline of its label below them.</summary>
    private const double TextGapBelow = 4;

    /// <summary>The gutter, in pixels, between the widest tick label of a bar beside the plot and its label, which the bar turns
    /// upright: the room the layouts keep (<see cref="TextReachBeside"/>) and the place the bar draws its label
    /// (<see cref="VerticalColorBarRenderer"/>) both read it.</summary>
    internal const double TextGutterBeside = 8;

    extension(Axes axes)
    {
        /// <summary>The axes' colour bar when it takes its room from the axes' cell under <paramref name="room"/>
        /// (<see cref="ColorBarRoom.TakesCell"/>); <see langword="null"/> otherwise and for a hidden bar.</summary>
        /// <param name="room">The room of the figure's colour bars.</param>
        internal ColorBar? BarTakingRoom(ColorBarRoom room) =>
            room.TakesCell && axes.ColorBar is { Visible: true } bar ? bar : null;
    }

    extension(Figure figure)
    {
        /// <summary>The room the figure's colour bars take: by its theme and whether it has the constrained layout
        /// (<see cref="ColorBarRoom.Under"/>).</summary>
        internal ColorBarRoom BarRoom => ColorBarRoom.Under(figure.Theme, figure.Spacing.ConstrainedLayout);

        /// <summary>The figure's own colour bar when it takes its room from the subplots (<see cref="ColorBarRoom.TakesCell"/>);
        /// <see langword="null"/> otherwise and for a hidden bar.</summary>
        internal ColorBar? FigureBarTakingRoom() =>
            figure.BarRoom.TakesCell && figure.FigureColorBar is { Visible: true } bar ? bar : null;

        /// <summary>How far past the box of the plot areas the figure's own bar clears what the subplots on its side draw past their
        /// plots: the farthest, over the subplots of the last column (for a bar beside) or the bottom row (for a bar below), of what the
        /// room stands its bar past (<see cref="ColorBarRoom.ClearanceOf"/>), below the box with an axes' own bar below and its stack
        /// (<see cref="ColorBarRoom.ReachBelow"/>); measured from the edge of the box, so the figure's bar stands past them as an
        /// axes' bar stands past its axes. 0 for a bar that takes its room from the cells, and when the figure has no visible bar or no
        /// subplot. The one rule the drawing and both layouts read it by.</summary>
        /// <param name="plots">The plot area of each subplot, in <see cref="Figure.SubPlots"/> order.</param>
        /// <param name="cells">The grid the subplots stand on, which says which stand on the bar's edge.</param>
        /// <param name="bandsOf">What the subplot at an index draws past its spines; asked only for the subplots on the bar's edge, and
        /// asked only for what the room reads.</param>
        /// <typeparam name="TBands">What answers for a subplot: its renderer, the layout's measure of it, or values measured already.</typeparam>
        /// <returns>The clearance, in pixels.</returns>
        internal double FigureBarClearance<TBands>(IReadOnlyList<Rect> plots, FigureGrid cells, Func<int, TBands> bandsOf) where TBands : IAxesBands
        {
            var room = figure.BarRoom;
            if (figure.FigureColorBar is not { Visible: true } bar || room.TakesCell || plots.Count == 0) return 0;
            double edge = 0;
            for (int i = 0; i < plots.Count; i++) edge = Math.Max(edge, plots[i].EndAlong(bar.Beside));
            double farthest = edge;
            for (int i = 0; i < plots.Count && i < figure.SubPlots.Count; i++)
            {
                if (!cells.StandsAtBarEdge(i, bar)) continue;
                double past = room.ClearanceOf(bar, bandsOf(i));
                // An axes' own bar below stands past the same band as the figure's bar below, so it is cleared by the same amount.
                if (!bar.Beside && figure.SubPlots[i].ColorBar is { Visible: true } own)
                    past = Math.Max(past, room.ReachBelow(own, plots[i], figure.Theme, past));
                farthest = Math.Max(farthest, plots[i].EndAlong(bar.Beside) + past);
            }

            return farthest - edge;
        }
    }

    extension(ColorBar bar)
    {
        /// <summary>Whether the bar runs vertically, beside the axes.</summary>
        internal bool Beside => !bar.Orientation.RunsAlongX;

        /// <summary>The gap between the axes and the bar when it takes its room from the cell, in pixels: the one the author gave, else
        /// the pad's share of the cell.</summary>
        private double GapIn(Rect cell) => bar.GivenPadding ?? (bar.Beside ? PadBeside : PadBelow) * cell.LengthAlong(bar.Beside);

        /// <summary>The part of <paramref name="cell"/> the axes keep when the bar takes its room from it: all of it less the slot and
        /// the gap, on the bar's side.</summary>
        /// <param name="cell">The cell the layout gives the axes.</param>
        /// <returns>The axes' part.</returns>
        internal Rect KeptOf(Rect cell) =>
            cell.WithLengthAlong(bar.Beside, cell.LengthAlong(bar.Beside) * (1 - Fraction) - bar.GapIn(cell));

        /// <summary>How far below the bottom edge of the body of a bar below its tick labels hang (their box's top, as matplotlib hangs
        /// the tick labels of a horizontal bar, <c>va='top'</c>): the gap a label keeps from the bar (<see cref="ColorBarMarks.LabelGap"/>:
        /// past a mark and its pad under a theme that marks its bars, 4 pixels otherwise).</summary>
        /// <param name="theme">The theme of the figure.</param>
        /// <param name="minor">Whether the label is a minor tick's, which stands from a minor mark.</param>
        /// <returns>The offset, in pixels.</returns>
        internal double TickTopBelow(Theme theme, bool minor = false) => ColorBarMarks.Under(theme).LabelGap(minor);

        /// <summary>How far below the bottom edge of the body of a bar below its label hangs: <see cref="TextGapBelow"/> under the line of
        /// its tick labels, a line the layout reckons by the tick font (<see cref="TextExtent.OfLine"/>).</summary>
        /// <param name="theme">The theme of the figure.</param>
        /// <returns>The offset, in pixels.</returns>
        internal double LabelTopBelow(Theme theme) => bar.TickBaselineBelow(theme) + TextGapBelow;

        /// <summary>The baseline of the tick labels of a bar below as the layout reckons it: hung from <see cref="TickTopBelow"/> by the
        /// ascent of a line of the tick font (<see cref="TextExtent.OfLine"/>).</summary>
        private double TickBaselineBelow(Theme theme) => bar.TickTopBelow(theme) + TextExtent.OfLine(0, theme.TickFont()).Ascent;

        /// <summary>The baseline of the label of a bar below as the layout reckons it: the line of the tick labels, then the gap and the
        /// ascent of a line of the label font.</summary>
        private double LabelBaselineBelow(Theme theme) => bar.TickBaselineBelow(theme) + TextExtent.OfLine(0, theme.LabelFont()).Ascent + TextGapBelow;

        /// <summary>How far below the bottom edge of the body of a bar below its text reaches: the foot of the lowest line, the
        /// label when it has one, else the tick labels, with the descent the library keeps below a baseline.</summary>
        /// <param name="theme">The theme of the figure.</param>
        /// <returns>The reach, in pixels.</returns>
        internal double TextReachBelow(Theme theme) => bar.Label is null
            ? bar.TickBaselineBelow(theme) + FacingInk.Descent * theme.TickFont().Size
            : bar.LabelBaselineBelow(theme) + FacingInk.Descent * theme.LabelFont().Size;

        /// <summary>How far past the outer edge of the body of a bar beside the plot its text reaches: the gap a label keeps from the
        /// bar (<see cref="ColorBarMarks.LabelGap"/>), the widest label the bar's ticks can have (at least a six-character number, the
        /// estimate the bar is laid out by: the layout does not know how long the bar turns out to be, so it takes the widest over
        /// every number of ticks the bar can ask for), then the gutter (<see cref="TextGutterBeside"/>) and the height of the label,
        /// which the vertical bar turns upright.</summary>
        /// <param name="series">The series that back the bar: the axes' own, or every subplot's for the bar of the figure.</param>
        /// <param name="ctx">The context text is measured with.</param>
        /// <param name="theme">The theme of the figure.</param>
        /// <returns>The reach, in pixels.</returns>
        internal double TextReachBeside(IEnumerable<ISeries> series, IRenderContext ctx, Theme theme)
        {
            var resolved = bar.Resolve(series, theme);
            var tickFont = theme.TickFont();
            double tickLabelWidth = Math.Max(ctx.ExtentOf(TextLabel.Of("0.0000"), tickFont).Width,
                ColorBarScale.WidestLabel(resolved.Range.Min, resolved.Range.Max, ctx, tickFont, resolved.Normalizer, theme, bar.Locator));
            double labelHeight = bar.Label is not null ? ctx.ExtentOf(TextLabel.Of(bar.Label), theme.LabelFont()).Height : 0;
            return ColorBarMarks.Under(theme).LabelGap() + tickLabelWidth + TextGutterBeside + labelHeight;
        }
    }
}
