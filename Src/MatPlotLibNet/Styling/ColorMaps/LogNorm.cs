// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>matplotlib's <c>LogNorm</c> (3.11.2, <c>colors.py</c>, <c>_make_norm_from_scale</c> over <c>LogScale</c>): a value maps to
/// <c>log(value / vmin) / log(vmax / vmin)</c>, and a range of no width maps every value to 0. The public
/// <see cref="LogNormalizer"/> keeps the formula its documentation states, <c>log(1 + value - min) / log(1 + max - min)</c>, which agrees
/// with this one only for a minimum of 1; the themes that colour as matplotlib does read a <see cref="LogNormalizer"/> as this one
/// (<see cref="INormalizerRule.ReadUnder"/>, asked by <see cref="NormalizerRuleExtensions.NormalizerUnder"/>). Internal: no series names it.
/// <para>A range that reaches 0 or below has no logarithm: matplotlib raises <c>Invalid vmin or vmax</c> for it, and takes the smallest
/// positive value as <c>vmin</c> when it finds the range itself, which the renderers and the colour bar do too
/// (<see cref="INormalizerRule.RangeScale"/>). A range an author sets that still reaches 0 is drawn as the <see cref="LogNormalizer"/>
/// draws it rather than not at all.</para></summary>
internal sealed class LogNorm : INormalizer, INormalizerRule
{
    /// <summary>The shared instance; the normaliser holds no state.</summary>
    public static LogNorm Instance { get; } = new();

    private static readonly LogNormalizer Documented = new();

    /// <inheritdoc />
    public double Normalize(double value, double min, double max)
    {
        if (min <= 0) return Documented.Normalize(value, min, max);
        if (min == max) return 0;
        return (Math.Log(value / min) / Math.Log(max / min)).Saturate();
    }

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>LogNorm</c> masks 0 and below (<c>nonpositive='mask'</c>), over a range that has a logarithm.</remarks>
    bool INormalizerRule.MasksOver(double min) => min > 0;

    /// <inheritdoc />
    bool INormalizerRule.Masks(double value, double min) => min > 0 && value <= 0;

    /// <inheritdoc />
    /// <remarks>The geometric mean (matplotlib's <c>_process_layers</c>: <c>sqrt(lower) * sqrt(upper)</c> for a log-scaled filled
    /// contour) where both levels are above 0.</remarks>
    double INormalizerRule.MiddleOf(double low, double high) =>
        low > 0 && high > 0 ? Math.Sqrt(low) * Math.Sqrt(high) : INormalizerRule.LinearMiddleOf(low, high);

    /// <inheritdoc />
    /// <remarks>The values above 0, as matplotlib's <c>LogNorm.autoscale_None</c> takes the smallest positive value as <c>vmin</c> and
    /// leaves a value of 0 or below out (the <c>DrawableRange</c> rule of a log axis).</remarks>
    ScaleRule INormalizerRule.RangeScale => ScaleRule.Log;

    /// <inheritdoc />
    ScaleRule INormalizerRule.LevelScale => ScaleRule.Log;

    /// <inheritdoc />
    /// <remarks>matplotlib's <c>Colorbar</c> takes the scale of its norm: under its log norm a <c>LogLocator</c> and a
    /// <c>LogFormatterSciNotation</c>, as a log axis has, over a range above 0.</remarks>
    bool INormalizerRule.NamesDecadesOver(double lo) => lo > 0;
}

/// <summary>What a normaliser comes to under a theme and what the library asks of it beyond its <see cref="INormalizer.Normalize"/>: the
/// one door to <see cref="INormalizerRule"/>, which answers for a normaliser the library does not know as for a linear one.</summary>
internal static class NormalizerRuleExtensions
{
    /// <summary>The rule of a normaliser that states none: every member at its default, a linear normaliser's answer.</summary>
    private sealed class PlainRule : INormalizerRule
    {
        internal static readonly PlainRule Instance = new();
    }

    extension(INormalizable source)
    {
        /// <summary>The normaliser <paramref name="source"/> colours its values with under <paramref name="theme"/>: the one it names (the
        /// linear one when it names none, <c>NormalizerOrLinear</c>), or the one the theme reads it as (<see cref="INormalizerRule.ReadUnder"/>:
        /// a <see cref="LogNormalizer"/> is matplotlib's <see cref="LogNorm"/> under the themes that colour so,
        /// <see cref="ColorMapLook.LikeLogNorm"/>). The one place a renderer, a colour bar and a colour-bar range read it, so they cannot read
        /// a log-normalised series differently.</summary>
        /// <param name="theme">The theme the series is drawn under.</param>
        /// <returns>The normaliser to colour with.</returns>
        internal INormalizer NormalizerUnder(Theme theme)
        {
            var named = source.NormalizerOrLinear();
            return named.Rule.ReadUnder(theme.Looks.ColorMap) ?? named;
        }
    }

    extension(double[,] grid)
    {
        /// <summary>The grid with every value <paramref name="normalizer"/> masks (<see cref="INormalizerRule.Masks"/>) made missing (NaN),
        /// as matplotlib's contour generator leaves out the cells of a masked value; the grid itself when nothing is masked, so a chart that
        /// masks nothing copies nothing.</summary>
        /// <param name="normalizer">The normaliser the grid is coloured with.</param>
        /// <param name="min">The low end of the range the colours run over.</param>
        /// <returns>The grid, or a copy of it with the masked values missing.</returns>
        internal double[,] MaskedBy(INormalizer normalizer, double min)
        {
            var rule = normalizer.Rule;
            if (!rule.MasksOver(min)) return grid;

            double[,]? masked = null;
            for (int row = 0; row < grid.GetLength(0); row++)
            {
                for (int col = 0; col < grid.GetLength(1); col++)
                {
                    if (!rule.Masks(grid[row, col], min)) continue;

                    masked ??= (double[,])grid.Clone();
                    masked[row, col] = double.NaN;
                }
            }

            return masked ?? grid;
        }
    }

    extension(INormalizer normalizer)
    {
        /// <summary>What the library asks of this normaliser beyond its <see cref="INormalizer.Normalize"/>: its own
        /// <see cref="INormalizerRule"/>, or a linear normaliser's answers for one that states none (a normaliser of the caller's own).</summary>
        internal INormalizerRule Rule => normalizer as INormalizerRule ?? PlainRule.Instance;

        /// <summary>Whether a value is left out of the colouring (<see cref="INormalizerRule.Masks"/>): drawn in the colour map's missing
        /// colour.</summary>
        /// <param name="value">The value to colour.</param>
        /// <param name="min">The low end of the range the colours run over.</param>
        /// <returns><see langword="true"/> when the value takes the missing colour.</returns>
        internal bool Masks(double value, double min) => normalizer.Rule.Masks(value, min);

        /// <summary>The value a band between two levels is coloured at (<see cref="INormalizerRule.MiddleOf"/>).</summary>
        /// <param name="low">The band's lower level.</param>
        /// <param name="high">The band's upper level.</param>
        /// <returns>The value to look the band's colour up at.</returns>
        internal double MiddleOf(double low, double high) => normalizer.Rule.MiddleOf(low, high);

        /// <summary>The scale the colour range of a series is taken over (<see cref="INormalizerRule.RangeScale"/>).</summary>
        internal ScaleRule RangeScale => normalizer.Rule.RangeScale;
    }
}
