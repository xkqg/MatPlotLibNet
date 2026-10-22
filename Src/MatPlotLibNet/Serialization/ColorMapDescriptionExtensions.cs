// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>How a colour map is written to the figure JSON and taken back: the one rule for every series that draws
/// through a colour map, the colour bar and the pair grid, so none of them carries its own copy.
/// <para>A map is written by the means that make it again: by its name in a <c>colorMapName</c> field when it is a map
/// the library ships (every reader knows those names, and that JSON is the bytes it always was), or when it is a map of
/// your own type that is registered under its name (a reader that registered it too makes it again; another reads no
/// map); as a description of what it is made of in a <c>colorMap</c> field when it is a map built in code (its colours,
/// their positions, its under, over and bad colours; a reversed map as the map it reverses); and not at all when it is a
/// map of your own type that nobody registered, which is code and not data.</para></summary>
internal static class ColorMapDescriptionExtensions
{
    /// <summary>The description the JSON carries for <paramref name="map"/>: a <see cref="NamedColorMapDto"/> when the map
    /// travels by its name, the map's own description when it is built in code, or <see langword="null"/> when there is
    /// no map or it cannot be written.</summary>
    /// <param name="map">The map to describe.</param>
    internal static ColorMapDto? Describe(this IColorMap? map) => map switch
    {
        null => null,
        _ when ColorMapRegistry.IsShipped(map) => new NamedColorMapDto(map.Name),
        IJsonDescribed<ColorMapDto> described => described.Describe(),
        _ when ReferenceEquals(ColorMapRegistry.Get(map.Name), map) => new NamedColorMapDto(map.Name),
        _ => null,
    };

    /// <summary>What a <c>colorMapName</c> field holds for <paramref name="map"/>: its name when it travels by its name,
    /// otherwise <see langword="null"/>.</summary>
    /// <param name="map">The map to write.</param>
    internal static string? NameForJson(this IColorMap? map) => map.Describe() is NamedColorMapDto named ? named.Name : null;

    /// <summary>What a <c>colorMap</c> field holds for <paramref name="map"/>: the description of a map built in code,
    /// otherwise <see langword="null"/>.</summary>
    /// <param name="map">The map to write.</param>
    internal static ColorMapDto? DescriptionForJson(this IColorMap? map) => map.Describe() is { } description and not NamedColorMapDto ? description : null;

    /// <summary>The map a <c>colorMap</c> description and a <c>colorMapName</c> written beside it make together: the
    /// description's map when there is one, otherwise the map registered under the name (none when nobody registered
    /// it), otherwise none.</summary>
    /// <param name="description">The description, when the JSON holds one.</param>
    /// <param name="name">The name, when the JSON holds one.</param>
    /// <exception cref="ArgumentException">The description cannot make a map; the exception names the field.</exception>
    internal static IColorMap? ReadColorMap(this ColorMapDto? description, string? name) =>
        (description ?? (name is null ? null : new NamedColorMapDto(name)))?.Restore();

    /// <summary>Gives <paramref name="series"/> the map <paramref name="dto"/> writes. A DTO that writes none leaves the
    /// series on the map it was created with.</summary>
    /// <param name="series">The series being read back.</param>
    /// <param name="dto">The serialization DTO carrying the series' persisted properties.</param>
    /// <exception cref="ArgumentException">The description cannot make a map; the exception names the field.</exception>
    internal static void RestoreColorMap(this IColormappable series, SeriesDto dto)
    {
        if (dto.ColorMap is not null || dto.ColorMapName is not null)
        {
            series.ColorMap = dto.ColorMap.ReadColorMap(dto.ColorMapName);
        }
    }
}
