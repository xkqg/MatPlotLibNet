// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Numerics;

/// <summary>Which levels a contour draws. This is the one place that says: the levels a user names
/// (<c>LevelValues</c> of a contour or a filled contour) and the round levels a contour takes for a count of levels when it names
/// none (<see cref="Auto"/>, matplotlib's), which <see cref="Under"/> hands every kind of contour: a line contour, a filled
/// contour and a triangle contour. A line contour and a triangle contour draw their lines at the levels it hands them, and a
/// filled contour fills the bands between them.</summary>
internal static class ContourLevels
{
    /// <summary>The levels of a contour that names none, as matplotlib's <c>contour(..., levels=count)</c> takes them (3.11.2,
    /// <c>ContourSet._ensure_locator_exists</c> and <c>_autolev</c>): the run the scale of the levels lays over the values
    /// (<see cref="ScaleRule.LevelsOver"/>: the ticks <c>MaxNLocator(count + 1, min_n_ticks=1)</c> lays, or, under a log norm, the decades
    /// <c>LogLocator(numticks=count)</c> lays); of
    /// which at most one below the lowest value and one above the highest are kept, or every one when fewer than three would be left.
    /// Round numbers, so the bands and the colour bar read in round steps; the colours are spread over these levels, not over the
    /// values.</summary>
    /// <param name="lo">The lowest value the grid holds (above 0 on a log scale).</param>
    /// <param name="hi">The highest value the grid holds; above <paramref name="lo"/>.</param>
    /// <param name="count">How many levels are asked for (matplotlib's <c>N</c>); below 1 counts as 1.</param>
    /// <param name="roundNumbers">Whether the theme rounds an axis out to its ticks (matplotlib's classic
    /// <c>axes.autolimit_mode</c>), which the locator reads too: <c>MaxNLocator</c> can take a larger step, and under the classic
    /// style <c>LogLocator</c> takes its older stride.</param>
    /// <param name="levelScale">The scale the contour's levels are laid over (<see cref="ScaleRule.LevelsOver"/>: the decades of a log
    /// scale, the levels ladder of any other); linear when <see langword="null"/>.</param>
    /// <returns>The levels, ascending.</returns>
    internal static double[] Auto(double lo, double hi, int count, bool roundNumbers, ScaleRule? levelScale = null)
    {
        double[] run = (levelScale ?? ScaleRule.Linear).LevelsOver(lo, hi, Math.Max(1, count), roundNumbers);
        int n = run.Length, first = 0, end = n;
        for (int i = 0; i < n; i++)
            if (run[i] < lo) first = i;
        for (int i = n - 1; i >= 0; i--)
            if (run[i] > hi) end = i + 1;
        if (end - first < 3)
        {
            first = 0;
            end = n;
        }

        return run[first..end];
    }

    /// <summary>The levels a contour draws under <paramref name="theme"/>: the ones the user named, else matplotlib's round levels for
    /// <paramref name="count"/> over the values that can be drawn (<see cref="Auto"/>, which reads the theme's autolimit mode as
    /// matplotlib's locator does). The one rule a line contour, a filled contour and a triangle contour read, so a contour laid over a
    /// filled one with the same count draws its lines on the edges of the bands.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <param name="named">The levels the user named, or <see langword="null"/>.</param>
    /// <param name="values">The range of the values that can be drawn, or <see langword="null"/> when there are none.</param>
    /// <param name="count">How many levels are asked for; below 1 counts as 1.</param>
    /// <param name="levelScale">The scale the levels are laid over (the normaliser's <see cref="Styling.ColorMaps.INormalizerRule.LevelScale"/>):
    /// a log scale lays decades, and <paramref name="values"/> are then those above 0; linear when <see langword="null"/>.</param>
    /// <returns>The levels, ascending; <see langword="null"/> when none were named and the values have no two 1e-12 or more apart.</returns>
    internal static double[]? Under(Theme theme, double[]? named, Range1D? values, int count, ScaleRule? levelScale = null) =>
        named.ExplicitLevels()
        ?? (values is { } z && z.Hi - z.Lo >= 1e-12
            ? Auto(z.Lo, z.Hi, count, theme.Looks.TickRule.RoundNumbers, levelScale)
            : null);

    extension(double[]? levelValues)
    {
        /// <summary>The levels a user named, as a contour draws them: ascending, each once, and only those that can be drawn
        /// (a level that is not a number, or infinite, has no place on any grid). Naming no level at all (<see langword="null"/>
        /// or an empty list) means the user left it to the count, which is <see langword="null"/> here; naming levels of which
        /// none can be drawn is an empty list, so the contour draws nothing instead of falling back to a count it was not
        /// asked for. A level outside the range of the grid is kept: it simply crosses no cell.</summary>
        /// <returns>A new array of the levels to draw, or <see langword="null"/> when none were named.</returns>
        internal double[]? ExplicitLevels() =>
            levelValues.NamesLevels
                ? [.. levelValues!.Where(level => level.IsDrawable()).Distinct().Order()]
                : null;

        /// <summary>Whether the user named any level, drawable or not: the list holds a value. A contour that names none leaves its
        /// levels, and the colours over them, to the count.</summary>
        internal bool NamesLevels => levelValues is { Length: > 0 };
    }

    extension(double[]? levels)
    {
        /// <summary>The range the colour bar of a contour of any kind covers, the levels it draws being these: its first level to its
        /// last, as matplotlib's <c>Colorbar</c> takes a <c>ContourSet</c>'s levels as its boundaries; the range of
        /// <paramref name="values"/> when there are not two levels to bound it.</summary>
        /// <param name="values">The range of the values that can be drawn, or <see langword="null"/> when there are none.</param>
        /// <returns>The range; empty (0 to 0) when there are neither two levels nor values, so the bar takes its own limits.</returns>
        internal MinMaxRange BarRangeOver(Range1D? values) =>
            levels is { Length: >= 2 } bounded ? new(bounded[0], bounded[^1])
            : values is { } drawable ? new(drawable.Lo, drawable.Hi)
            : default;

        /// <summary>The normaliser the colour bar of a contour of any kind is drawn with under <paramref name="theme"/>, the levels it
        /// draws being these: under the themes that draw a bar as matplotlib's <c>Colorbar</c> does
        /// (<see cref="ColorBarLook.StepPerColour"/>), a band between each two levels, the levels evenly spaced and the ticks on them
        /// (<see cref="ContourBandNormalizer"/>: <c>Colorbar.__init__</c> takes <c>boundaries = cs._levels</c> and
        /// <c>FixedLocator(cs.levels, nbins=10)</c>, for a contour of lines and a filled one alike); <see langword="null"/> under the
        /// library's own themes, or with fewer than two levels, where the bar is drawn with the normaliser the series colours with.</summary>
        /// <param name="theme">The theme of the figure.</param>
        /// <param name="colours">The normaliser the contour colours its levels with.</param>
        /// <returns>The normaliser, or <see langword="null"/>.</returns>
        internal INormalizer? BarNormalizerUnder(Theme theme, INormalizer colours) =>
            theme.Looks.ColorBar.StepPerColour && levels is { Length: >= 2 } bounded ? new ContourBandNormalizer(bounded, colours) : null;
    }
}
