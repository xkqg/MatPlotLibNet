// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Styling;

/// <summary>The colour map a series that names none draws with, and how a log normaliser colours.</summary>
internal sealed record ColorMapLook : IThemeKeys<ColorMapLook>
{
    /// <summary>The library's own colour mapping: viridis, and the documented formula of the log normaliser.</summary>
    internal static ColorMapLook Library { get; } = new();

    /// <inheritdoc />
    public void WriteKeys(ThemeDto keys)
    {
        keys.DefaultMapOnTheWire = Default.ColorMapOnTheWire();
        keys.ColoursLikeLogNorm = LikeLogNorm.OrNullAtRest(false);
        keys.CountsFromTheLeastDrawn = CountsFromTheLeastDrawn.OrNullAtRest(false);
    }

    /// <inheritdoc />
    public static ColorMapLook ReadKeys(ThemeDto keys) => new()
    {
        Default = keys.DefaultMapOnTheWire.Read(),
        LikeLogNorm = keys.ColoursLikeLogNorm ?? false,
        CountsFromTheLeastDrawn = keys.CountsFromTheLeastDrawn ?? false,
    };

    /// <summary>The colour map of a series that names none (matplotlib's <c>image.cmap</c>): <c>jet</c> under the classic style.
    /// Unset: <see cref="ColorMaps.ColorMaps.Viridis"/>. Read through <see cref="Of"/>.</summary>
    public IColorMap? Default { get; init; }

    /// <summary>Whether a series normalised by a <see cref="LogNormalizer"/> is coloured as matplotlib's <c>LogNorm</c> colours it:
    /// <c>log(value / vmin) / log(vmax / vmin)</c> over the smallest positive value to the largest, a value of 0 or below masked, and
    /// a filled contour's band coloured at the geometric mean of its levels. Otherwise the documented formula of the
    /// <see cref="LogNormalizer"/>. <c>NormalizerUnder</c> holds the rule.</summary>
    public bool LikeLogNorm { get; init; }

    /// <summary>Whether a mesh of counts (a hexbin's hexagons) is coloured from the least count it draws, as matplotlib's norm autoscales to
    /// the drawn cells (printed with 3.11.2: <c>hexbin(mincnt=1)</c> over counts 5 and 7 colours over (5, 7)). Otherwise the library's rule:
    /// half a count under the least count asked for, so it is not the darkest colour of the map. A range of no width is opened by the one
    /// rule of every colour range (<see cref="ColorMaps.ColorMapping.Opened"/>).</summary>
    public bool CountsFromTheLeastDrawn { get; init; }

    /// <summary>The colour map a series draws with: the one it names, else <see cref="Default"/>, else viridis. The one rule every
    /// colour-mapped series, colour bar and figure image reads.</summary>
    /// <param name="given">The map the series, bar or image names, <see langword="null"/> when none.</param>
    /// <returns>The map.</returns>
    internal IColorMap Of(IColorMap? given) => given ?? Default ?? ColorMaps.ColorMaps.Viridis;

}
