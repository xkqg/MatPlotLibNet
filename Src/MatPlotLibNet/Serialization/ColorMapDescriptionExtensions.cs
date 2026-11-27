// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Serialization;

/// <summary>How a colour map is written to the figure JSON: the one rule for every series that draws through a colour map, the colour
/// bar, the theme, the pair grid and an image on the figure, so none of them carries its own copy. What the document holds is a
/// <see cref="ColorMapWire"/>, which also reads it back.
/// <para>A map is written by the means that make it again: by its name in a <c>colorMapName</c> field when it is a map
/// the library ships (every reader knows those names, and that JSON is the bytes it always was), or when it is a map of
/// your own type that is registered under its name (a reader that registered it too makes it again; another reads no
/// map); as a description of what it is made of in a <c>colorMap</c> field when it is a map built in code (its colours,
/// their positions, its under, over and bad colours; a reversed map as the map it reverses); and not at all when it is a
/// map of your own type that nobody registered, which is code and not data.</para></summary>
internal static class ColorMapDescriptionExtensions
{
    extension(IColorMap? map)
    {
        /// <summary>The description the JSON carries for <paramref name="map"/>: a <see cref="NamedColorMapDto"/> when the map
        /// travels by its name, the map's own description when it is built in code, or <see langword="null"/> when there is
        /// no map or it cannot be written.</summary>
        internal ColorMapDto? Describe() => map switch
        {
            null => null,
            _ when ColorMapRegistry.IsShipped(map) => new NamedColorMapDto(map.Name),
            IJsonDescribed<ColorMapDto> described => described.Describe(),
            _ when ReferenceEquals(ColorMapRegistry.Get(map.Name), map) => new NamedColorMapDto(map.Name),
            _ => null,
        };

        /// <summary>What the document carries for <paramref name="map"/>: its name when it travels by its name, its description when it
        /// is built in code, nothing when there is no map or it cannot be written. The map is described once.</summary>
        /// <returns>The two fields of the document, one of them at most written.</returns>
        internal ColorMapWire ColorMapOnTheWire() => map.Describe() switch
        {
            NamedColorMapDto named => new ColorMapWire(named.Name, null),
            { } description => new ColorMapWire(null, description),
            _ => default,
        };
    }
}
