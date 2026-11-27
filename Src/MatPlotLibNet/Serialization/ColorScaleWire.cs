// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>What the figure JSON carries of the colour scale of a series or an image: its colour map (<see cref="ColorMapWire"/>) and
/// the description of its normaliser. The one value that writes a scale (<see cref="Of"/>) and reads it back (<see cref="ReadMap"/>,
/// <see cref="ReadNormalizer"/>), so no owner of a scale lists the halves. A series that maps its values to colours writes it through
/// <see cref="SeriesDto.ColorScale"/> and reads it back through
/// <see cref="ColorScaleExtensions.RestoreColorScale(IColormappable, SeriesDto)"/>.</summary>
/// <param name="Map">The colour map, by name or by description.</param>
/// <param name="Normalizer">The description of the normaliser; absent for the linear mapping and for a normaliser written outside the
/// library.</param>
internal readonly record struct ColorScaleWire(ColorMapWire Map, NormalizerDto? Normalizer)
{
    /// <summary>What the document carries for a colour scale made of <paramref name="map"/> and <paramref name="normalizer"/>: the
    /// map by its name or its description, and the normaliser as the description it writes of itself, or nothing when there is none
    /// (it maps linearly) or it was written outside the library, which is code and not data.</summary>
    /// <param name="map">The colour map; <see langword="null"/> for none.</param>
    /// <param name="normalizer">The normaliser; <see langword="null"/> for none.</param>
    /// <returns>The scale as the document carries it.</returns>
    internal static ColorScaleWire Of(IColorMap? map, INormalizer? normalizer) =>
        new(map.ColorMapOnTheWire(), normalizer.DescribeForJson());

    /// <summary>The colour map the document carries; <see langword="null"/> when it carries none.</summary>
    /// <exception cref="ArgumentException">The description cannot make a map; the exception names the field.</exception>
    internal IColorMap? ReadMap() => Map.Read();

    /// <summary>The normaliser the document carries; <see langword="null"/> when it carries none, and for a kind this build does not
    /// know, which is reported through the chart diagnostics.</summary>
    internal INormalizer? ReadNormalizer() => Normalizer?.Restore();
}
