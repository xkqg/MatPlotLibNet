// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Styling;

/// <summary>The theme's rule for the ticks of an axis that names no locator or spacing of its own: the ladder that lays them and whether
/// the theme rounds numbers. Read once off the theme's looks (<see cref="ThemeLooks.TickRule"/>) by every kind of axis
/// (<see cref="TickRequest"/>), by a log and a logit scale and by the levels of a contour.</summary>
/// <param name="Ladder">The ladder that lays the ticks (<see cref="AxisTickLook.Ladder"/>: matplotlib's <c>AutoLocator</c> under the
/// matplotlib themes); <see langword="null"/> for the library's own placement.</param>
/// <param name="RoundNumbers">Whether the theme ends an axis at round numbers (<see cref="AxisRangeLook.Autolimit"/>), matplotlib's classic
/// style: its <c>axes.autolimit_mode = 'round_numbers'</c>, which <c>MaxNLocator._raw_ticks</c> reads (a step that reaches the end of the
/// view from a multiple of itself), and the classic mode that comes with it, which <c>LogLocator</c> reads (fifteen ticks whatever the room,
/// and the older stride over decades).</param>
internal readonly record struct AxisTickRule(TickLadder? Ladder, bool RoundNumbers)
{
    /// <summary>The rule of the library's own themes: no ladder, no round numbers.</summary>
    internal static AxisTickRule Library => default;

    /// <summary>The ladder a log or logit scale lays its ticks by where it falls back to evenly spaced ones (a view that holds one power of
    /// ten or none): the theme's, or the default style's <c>AutoLocator</c> under a theme that names none.</summary>
    internal TickLadder FallbackLadder => Ladder ?? TickLadder.DefaultStyle;
}
