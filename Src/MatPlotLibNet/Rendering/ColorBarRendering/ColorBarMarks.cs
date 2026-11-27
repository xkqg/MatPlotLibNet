// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.ColorBarRendering;

/// <summary>The tick marks of a colour bar as a theme draws them, and how far the bar's tick labels stand from it: the one rule the
/// bar of an axes either way round, the bar of the figure and the room the layout keeps read. Under a theme that marks its bars
/// (<see cref="ColorBarLook.TickMarks"/>, the matplotlib themes) a mark stands on the bar's outer side at each tick, a major one as
/// the theme draws an axis' major ticks (<c>ytick.major.size</c>, <c>width</c>, <c>direction</c>) and a minor one as its minor ticks,
/// and a label stands the part of its mark outside the bar and the pad from the bar, as matplotlib's colour-bar axis draws them;
/// under every other theme there is no mark and a label stands 4 pixels from the bar.</summary>
/// <param name="Major">How a major mark is drawn, <see langword="null"/> when the theme draws none.</param>
/// <param name="Minor">How a minor mark is drawn, <see langword="null"/> when the theme draws none.</param>
internal readonly record struct ColorBarMarks(TickConfig? Major, TickConfig? Minor)
{
    /// <summary>How far a tick label stands from the bar under a theme that draws no marks, in pixels.</summary>
    internal const double RestingLabelGap = 4;

    private static readonly TickConfig RestingMajor = new();
    private static readonly TickConfig RestingMinor = new() { RestsAsMinor = true };

    /// <summary>The marks a bar has under <paramref name="theme"/>: the theme's ticks (<see cref="TickConfig.DrawnUnder"/>) where it
    /// marks its bars, none otherwise.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The marks.</returns>
    internal static ColorBarMarks Under(Theme theme) => theme.Looks.ColorBar.TickMarks
        ? new(RestingMajor.DrawnUnder(theme, major: true), RestingMinor.DrawnUnder(theme, major: false))
        : default;

    /// <summary>How far a tick label stands from the bar's outer edge: the part of its mark outside the bar and the pad
    /// (matplotlib's <c>Tick.get_tick_padding</c> and <c>ytick.major.pad</c>; 7 pt under the default style, 4 pt under the classic one,
    /// whose marks point in), <see cref="RestingLabelGap"/> without marks.</summary>
    /// <param name="minor">Whether the label is a minor tick's (a named multiple on a bar of matplotlib's log norm).</param>
    /// <returns>The gap, in pixels.</returns>
    internal double LabelGap(bool minor = false) =>
        (minor ? Minor : Major) is { } mark ? mark.OutsideLength + mark.Pad : RestingLabelGap;

    /// <summary>Draws the marks along the outer edge of <paramref name="gradient"/> (the right edge of a vertical bar, the foot of a
    /// horizontal one) where <paramref name="scale"/> puts them: a major mark on each of its major places, a minor one on each of its
    /// minor places, in <paramref name="ink"/> unless the theme's ticks name a colour. Nothing without marks.</summary>
    /// <param name="ctx">The surface it is drawn on.</param>
    /// <param name="scale">The bar's scale, which says where the marks stand.</param>
    /// <param name="gradient">The part of the bar the gradient fills, along which the marks stand.</param>
    /// <param name="vertical">Whether the bar runs upward.</param>
    /// <param name="ink">The colour of a mark that names none.</param>
    /// <param name="frameWidth">The width of the bar's frame, half of which a mark reaches across it.</param>
    internal void Draw(IRenderContext ctx, ColorBarScale scale, Rect gradient, bool vertical, Color ink, double frameWidth)
    {
        if (Major is not { } major || Minor is not { } minor) return;

        // A vertical bar runs along Y; its marks hang from the edge past it, at the fraction of the way up or across their value is.
        bool alongX = !vertical;
        var outer = EdgeTerms.Past(alongX);
        double edge = outer.EdgeAt(gradient, 0);
        var majorMark = TickMark.Of(major, ink, frameWidth.Reach);
        foreach (double along in scale.MajorMarks)
            ctx.DrawTickMark(gradient.FractionAlong(alongX, along), edge, outer, majorMark);
        var minorMark = TickMark.Of(minor, ink, frameWidth.Reach);
        foreach (double along in scale.MinorMarks)
            ctx.DrawTickMark(gradient.FractionAlong(alongX, along), edge, outer, minorMark);
    }
}
