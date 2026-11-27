// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>How the values of a colour-mapped series become colours under a theme, before the range they run over is known: the map it
/// draws with, the normaliser the theme reads it as, and the theme (whose colour-map look says where a mesh of counts starts and whose
/// surface strides say which means a surface colours). Made by <c>ColorScaledExtensions.ColorMappingUnder</c>; <see cref="Over"/> gives the
/// colour scale once the range is known.</summary>
/// <param name="Map">The colour map the values are drawn through.</param>
/// <param name="Normalizer">The normaliser that places a value on the map.</param>
/// <param name="Theme">The theme the values are drawn under.</param>
internal readonly record struct ColorMapping(IColorMap Map, INormalizer Normalizer, Theme Theme)
{
    /// <summary>The theme's colour-map look.</summary>
    internal ColorMapLook Look => Theme.Looks.ColorMap;

    /// <summary>The mapping of a part that names a colour map and is coloured linearly (a pair grid's density panel): its map, else the
    /// theme's.</summary>
    /// <param name="named">The map the part names, <see langword="null"/> when none.</param>
    /// <param name="theme">The theme it is drawn under.</param>
    /// <returns>The mapping.</returns>
    internal static ColorMapping Linear(IColorMap? named, Theme theme) =>
        new(theme.Looks.ColorMap.Of(named), LinearNormalizer.Instance, theme);

    /// <summary>The scale the range of the values is taken over (<c>RangeScale</c>): the values above 0 only under matplotlib's log norm.</summary>
    internal ScaleRule RangeScale => Normalizer.RangeScale;

    /// <summary>The range the colours run over (<see cref="ColourRangeOf"/>): the given ends, else the drawable values.</summary>
    /// <param name="values">The smallest and largest value the scale can place; <see langword="null"/> when there is none.</param>
    /// <param name="vMin">The low end an author gave, or <see langword="null"/>.</param>
    /// <param name="vMax">The high end an author gave, or <see langword="null"/>.</param>
    /// <returns>The range.</returns>
    internal MinMaxRange RangeOf(Range1D? values, double? vMin = null, double? vMax = null) => ColourRangeOf(values, vMin, vMax);

    /// <summary>The range the colours of a scale run over: the ends given (<paramref name="vMin"/>, <paramref name="vMax"/>) where they are
    /// given, else the smallest and largest of the values the scale can place (<paramref name="values"/>), 0 to 1 with no value (as
    /// matplotlib's colour bar takes a norm that has none), and a range of no width opened by <see cref="Opened"/>. The one rule every
    /// colour-mapped series, image and colour bar reads.</summary>
    /// <param name="values">The smallest and largest value the scale can place; <see langword="null"/> when there is none.</param>
    /// <param name="vMin">The low end an author gave, or <see langword="null"/>.</param>
    /// <param name="vMax">The high end an author gave, or <see langword="null"/>.</param>
    /// <returns>The range, with width.</returns>
    internal static MinMaxRange ColourRangeOf(Range1D? values, double? vMin, double? vMax) =>
        Opened(vMin ?? values?.Lo ?? 0, vMax ?? values?.Hi ?? 1);

    /// <summary>A colour range from <paramref name="lo"/> to <paramref name="hi"/>, opened one wide from its low end when it has no width
    /// (or its ends are the wrong way round, which matplotlib refuses), under every theme: every value then takes the map's first colour,
    /// as matplotlib draws a norm of no width (<c>Normalize.__call__</c>: <c>if vmin == vmax: result.fill(0)</c>; python, 3.11.2: an
    /// <c>imshow</c> of zeros, a <c>hexbin</c> of one count and a <c>scatter</c> of one value all take the map's first colour). Only the
    /// colour bar of matplotlib widens the norm it reads, by a tenth (<c>Colorbar._process_values</c>), a change a bar makes to its series
    /// as it is drawn; the library's scale is the same for the cells, the bar and the legend, so they cannot disagree.</summary>
    /// <param name="lo">The low end.</param>
    /// <param name="hi">The high end.</param>
    /// <returns>The range, with width.</returns>
    internal static MinMaxRange Opened(double lo, double hi) => lo < hi ? new(lo, hi) : new(lo, lo + 1);

    /// <summary>The range the colours of <paramref name="values"/> run over: those the scale can place (<see cref="RangeScale"/>).</summary>
    /// <inheritdoc cref="RangeOf(Range1D?, double?, double?)"/>
    internal MinMaxRange RangeOf(double[,] values, double? vMin = null, double? vMax = null) =>
        RangeOf(values.DrawableRange(RangeScale), vMin, vMax);

    /// <inheritdoc cref="RangeOf(double[,], double?, double?)"/>
    internal MinMaxRange RangeOf(double[]? values, double? vMin = null, double? vMax = null) =>
        RangeOf(values?.DrawableRange(RangeScale), vMin, vMax);

    /// <summary>The range the colours of a grid of counts run over: those the scale can place (<see cref="RangeScale"/>).</summary>
    /// <param name="counts">The counts.</param>
    /// <returns>The range.</returns>
    internal MinMaxRange RangeOf(int[,] counts) => RangeOf(counts.DrawableRange(RangeScale));

    /// <summary>The colour scale of this mapping over <paramref name="range"/>.</summary>
    /// <param name="range">The range the colours run over.</param>
    /// <returns>The colour scale.</returns>
    internal ColormapContext Over(MinMaxRange range) => new(Map, Normalizer, range.Min, range.Max);
}

/// <summary>The colour scale a colour-mapped series is drawn with: its map, its normaliser and the range its colours run over, the value
/// every renderer, image and model that colours a value reads (<c>ColorScaledExtensions.ColorScaleUnder</c>).</summary>
/// <param name="ColorMap">The colour map.</param>
/// <param name="Normalizer">The normaliser that places a value on the map.</param>
/// <param name="Min">The value at the low end of the map.</param>
/// <param name="Max">The value at the high end of the map.</param>
internal readonly record struct ColormapContext(IColorMap ColorMap, INormalizer Normalizer, double Min, double Max)
{
    /// <summary>The colour of <paramref name="value"/> (<see cref="ColormapExtensions"/>' <c>ColorOf</c>: a missing or masked value takes
    /// the map's missing colour).</summary>
    /// <param name="value">The value.</param>
    /// <returns>Its colour.</returns>
    internal Color ColorOf(double value) => ColorMap.ColorOf(value, Normalizer, Min, Max);
}
