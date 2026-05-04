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
    /// <summary>Returns <c>source.ColorMap</c> when set, otherwise the supplied
    /// <paramref name="fallback"/>. Replaces the inline <c>series.ColorMap ?? ColorMaps.Viridis</c>
    /// pattern with a single call site so future logic (theme-driven defaults, accessibility
    /// overrides, telemetry) lands in one place.</summary>
    /// <param name="source">The colormappable series or component supplying the optional map.</param>
    /// <param name="fallback">Map to use when <paramref name="source"/>'s map is <see langword="null"/>.</param>
    /// <returns>The resolved <see cref="IColorMap"/> — never <see langword="null"/>.</returns>
    public static IColorMap GetColorMapOrDefault(this IColormappable source, IColorMap fallback)
        => source.ColorMap ?? fallback;

    /// <summary>Maps an index in <c>[0, count)</c> to a colormap fraction in <c>[0, 1]</c> using
    /// even spacing. Returns <paramref name="singletonT"/> when <paramref name="count"/> is
    /// 1 or less so callers don't divide by zero in the degenerate single-cluster / single-sibling
    /// case.</summary>
    /// <param name="index">Zero-based index of the item to colour.</param>
    /// <param name="count">Total item count over which the colormap is sampled.</param>
    /// <param name="singletonT">Fraction returned when <paramref name="count"/> ≤ 1. Defaults to
    /// <c>0.5</c> (centre of the colormap); pass <c>0.0</c> when callers want the first colour
    /// for the singleton case (e.g. cluster index 0).</param>
    /// <returns>The fraction <c>index / (count - 1)</c> for normal inputs, or
    /// <paramref name="singletonT"/> when <paramref name="count"/> ≤ 1.</returns>
    public static double ColormapFraction(this int index, int count, double singletonT = 0.5)
        => count <= 1 ? singletonT : index / (double)(count - 1);
}
