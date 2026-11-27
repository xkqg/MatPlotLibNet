// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.ColorBarRendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Models.Series;

/// <summary>A colour bar's data provider whose bar depends on the theme it is drawn under: a contour of any kind that names no
/// levels takes them from a locator that reads the theme's autolimit mode, and its colours and bar run over those levels; under
/// the matplotlib themes its bar has a band between each two levels, and a contour of lines draws a line at each level in place
/// of a fill. A colour bar reads it through <c>ResolvedColorBarExtensions.ColorBarRangeUnder</c>, <c>ColorBarNormalizerUnder</c> and
/// <c>ColorBarLinesUnder</c> on any <see cref="IColorBarDataProvider"/>, where a provider without it gives
/// <see cref="IColorBarDataProvider.GetColorBarRange"/>, the normaliser it colours its values with, and a fill.</summary>
/// <remarks>A separate internal interface, not a member of the public <see cref="IColorBarDataProvider"/>, so that an application
/// can still implement, proxy or mock the public interface.</remarks>
internal interface IThemedColorBar : IColorBarDataProvider
{
    /// <summary>The range the colour bar covers when the series is drawn under <paramref name="theme"/>.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The range.</returns>
    MinMaxRange ColorBarRangeUnder(Theme theme);

    /// <summary>The normaliser the colour bar is drawn with when the series is drawn under <paramref name="theme"/>, where it differs
    /// from the one the series colours its values with: one that makes the bar bands (<c>IDiscreteNormalizer</c>).</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The normaliser, or <see langword="null"/> when the bar is drawn with the series' own.</returns>
    INormalizer? ColorBarNormalizerUnder(Theme theme);

    /// <summary>The lines the colour bar draws in place of a fill when the series is drawn under <paramref name="theme"/>: those of a
    /// contour of lines, one at each level in the colour and width of its line.</summary>
    /// <param name="theme">The theme of the figure.</param>
    /// <returns>The lines, or <see langword="null"/> for a bar that is filled.</returns>
    ColorBarLines? ColorBarLinesUnder(Theme theme);
}
