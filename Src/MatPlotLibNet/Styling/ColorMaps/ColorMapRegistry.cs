// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Thread-safe registry for looking up colormaps by name. All built-in colormaps
/// and their reversed variants are registered automatically.</summary>
public static class ColorMapRegistry
{
    private static readonly NamedTable<string, IColorMap> Maps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The maps the library ships and the reverses the registry holds of them, by reference: the maps every
    /// reader knows by name. Filled by the static constructor only, and only read after it.</summary>
    private static readonly HashSet<IColorMap> Shipped = new(ReferenceEqualityComparer.Instance);

    static ColorMapRegistry()
    {
        // The classes that declare maps, in the order their maps are listed; each class says which maps it holds.
        ColorMapShelf[] shelves =
        [
            ColorMaps.Shelf, SequentialColorMaps.Shelf, DivergingColorMaps.Shelf, CyclicColorMaps.Shelf,
            AdditionalColorMaps.Shelf, PerceptualColorMaps2.Shelf, QualitativeColorMaps.Shelf,
        ];

        foreach (var shelf in shelves)
            foreach (var map in shelf.Maps)
                Ship(map);
    }

    /// <summary>Registers a colormap and its reversed variant (<c>{name}_r</c>).</summary>
    /// <param name="map">The map to register.</param>
    /// <returns>The reversed variant, as registered.</returns>
    internal static IColorMap RegisterWithItsReverse(IColorMap map)
    {
        Maps.Set(map.Name, map);
        var reversed = new ReversedColorMap(map);
        Maps.Set(reversed.Name, reversed);
        return reversed;
    }

    /// <summary>Registers a map the library ships, and its reverse, as maps every reader knows by name.</summary>
    private static void Ship(IColorMap map)
    {
        Shipped.Add(map);
        Shipped.Add(RegisterWithItsReverse(map));
    }

    /// <summary>Whether <paramref name="map"/> is one of the maps the library ships, or the reverse the registry holds of
    /// one: a map every reader knows by its name, so the figure JSON writes that name and nothing else.</summary>
    /// <param name="map">The map asked about, compared by reference.</param>
    internal static bool IsShipped(IColorMap map) => Shipped.Contains(map);

    /// <summary>Registers a custom colormap by name.</summary>
    public static void Register(string name, IColorMap map) => Maps.Set(name, map);

    /// <summary>Gets a colormap by name (case-insensitive), or null if not found.</summary>
    public static IColorMap? Get(string name) => Maps.Get(name);

    /// <summary>Gets all registered colormap names.</summary>
    public static IEnumerable<string> Names => Maps.Keys;
}
