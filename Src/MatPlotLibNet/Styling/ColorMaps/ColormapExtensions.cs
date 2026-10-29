// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Convenience extensions over <see cref="IColormappable"/> and <see cref="IColorMap"/>
/// that centralise the two patterns previously duplicated across every grid / hierarchical /
/// 3D renderer: the null-fallback for an unset colormap and the even-fraction sample formula
/// used to colour N items.</summary>
public static class ColormapExtensions
{
    extension(IColormappable source)
    {
        /// <summary>Returns <c>source.ColorMap</c> when set, otherwise the supplied
        /// <paramref name="fallback"/>. Replaces the inline <c>series.ColorMap ?? ColorMaps.Viridis</c>
        /// pattern with a single call site so future logic (theme-driven defaults, accessibility
        /// overrides, telemetry) lands in one place.</summary>
        /// <param name="fallback">Map to use when <paramref name="source"/>'s map is <see langword="null"/>.</param>
        /// <returns>The resolved <see cref="IColorMap"/> — never <see langword="null"/>.</returns>
        public IColorMap GetColorMapOrDefault(IColorMap fallback)
            => source.ColorMap ?? fallback;
    }

    extension(INormalizable source)
    {
        /// <summary>The normaliser <paramref name="source"/> maps its values to colours with: the one it names, otherwise the
        /// linear one. The one place "no normaliser" means "linear", so a renderer and a colour bar that read the same series
        /// cannot read it differently.</summary>
        /// <returns>The series' <see cref="INormalizable.Normalizer"/>, or <see cref="LinearNormalizer.Instance"/>.</returns>
        internal INormalizer NormalizerOrLinear() => source.Normalizer ?? LinearNormalizer.Instance;
    }

    extension(int index)
    {
        /// <summary>Maps an index in <c>[0, count)</c> to a colormap fraction in <c>[0, 1]</c> using
        /// even spacing. Returns <paramref name="singletonT"/> when <paramref name="count"/> is
        /// 1 or less so callers don't divide by zero in the degenerate single-cluster / single-sibling
        /// case.</summary>
        /// <param name="count">Total item count over which the colormap is sampled.</param>
        /// <param name="singletonT">Fraction returned when <paramref name="count"/> ≤ 1. Defaults to
        /// <c>0.5</c> (centre of the colormap); pass <c>0.0</c> when callers want the first colour
        /// for the singleton case (e.g. cluster index 0).</param>
        /// <returns>The fraction <c>index / (count - 1)</c> for normal inputs, or
        /// <paramref name="singletonT"/> when <paramref name="count"/> ≤ 1.</returns>
        public double ColormapFraction(int count, double singletonT = 0.5)
            => count <= 1 ? singletonT : index / (double)(count - 1);
    }

    extension(IColorMap map)
    {
        /// <summary>The colour <paramref name="map"/> draws a missing value (NaN) in: its bad colour
        /// (<see cref="IColorMap.GetBadColor"/>), transparent when it names none, which is matplotlib's default bad colour.
        /// The one rule a map's own lookup and every renderer share.</summary>
        /// <returns>The bad colour, or <see cref="Colors.Transparent"/>.</returns>
        internal Color MissingValueColor() => map.GetBadColor() ?? Colors.Transparent;

        /// <summary>The colour a data value is drawn in: the one place a renderer colours a value through a colormap.
        /// A missing value (NaN) is <see cref="MissingValueColor"/> and reaches neither the normalizer nor the lookup:
        /// <see cref="BoundaryNormalizer"/> would put it in its last bin and <see cref="SymLogNormalizer"/> would throw on it.
        /// Every other value, infinity included, is normalised over [<paramref name="min"/>, <paramref name="max"/>] and
        /// looked up, so an infinite value takes the end colour the normalizer clamps it to.</summary>
        /// <param name="value">The data value, not yet normalised.</param>
        /// <param name="normalizer">Maps the value into [0, 1].</param>
        /// <param name="min">The low end of the value range.</param>
        /// <param name="max">The high end of the value range.</param>
        /// <returns>The colour to draw the value in.</returns>
        internal Color ColorOf(double value, INormalizer normalizer, double min, double max)
            => value.IsMissing() ? map.MissingValueColor() : map.GetColor(normalizer.Normalize(value, min, max));
    }
}
