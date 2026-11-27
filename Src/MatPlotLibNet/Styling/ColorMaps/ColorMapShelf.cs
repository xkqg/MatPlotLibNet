// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>The colour maps one class declares, in the order it declares them. A map property adds the map it makes as it makes it
/// (<c>Viridis { get; } = Shelf.Add(...)</c>), so the list of maps the library ships is the list of those properties and nothing else;
/// <see cref="ColorMapRegistry"/> names the classes and reads their shelves. It is filled by a static initialiser only, before the
/// class is first used, and only read after.</summary>
internal sealed class ColorMapShelf
{
    private readonly List<IColorMap> _maps = [];

    /// <summary>The maps put on the shelf, in the order they were put there.</summary>
    internal IReadOnlyList<IColorMap> Maps => _maps;

    /// <summary>Puts a map on the shelf.</summary>
    /// <param name="map">The map a property of the class makes.</param>
    /// <returns>The same map, so a property reads as its initialiser.</returns>
    internal IColorMap Add(IColorMap map)
    {
        _maps.Add(map);
        return map;
    }
}
