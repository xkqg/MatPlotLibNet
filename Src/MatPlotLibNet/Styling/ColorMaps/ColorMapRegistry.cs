// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Thread-safe registry for looking up colormaps by name. All built-in colormaps
/// and their reversed variants are registered automatically.</summary>
public static class ColorMapRegistry
{
    private static readonly ConcurrentDictionary<string, IColorMap> Maps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The maps the library ships and the reverses the registry holds of them, by reference: the maps every
    /// reader knows by name. Filled by the static constructor only, and only read after it.</summary>
    private static readonly HashSet<IColorMap> Shipped = new(ReferenceEqualityComparer.Instance);

    static ColorMapRegistry()
    {
        // Perceptually-uniform (ColorMaps)
        Ship(ColorMaps.Viridis);
        Ship(ColorMaps.Plasma);
        Ship(ColorMaps.Inferno);
        Ship(ColorMaps.Magma);
        Ship(ColorMaps.Coolwarm);
        Ship(ColorMaps.Blues);
        Ship(ColorMaps.Reds);
        Ship(ColorMaps.Turbo);
        Ship(ColorMaps.Jet);

        // Sequential
        Ship(SequentialColorMaps.Cividis);
        Ship(SequentialColorMaps.Greens);
        Ship(SequentialColorMaps.Oranges);
        Ship(SequentialColorMaps.Purples);
        Ship(SequentialColorMaps.Greys);
        Ship(SequentialColorMaps.YlOrBr);
        Ship(SequentialColorMaps.YlOrRd);
        Ship(SequentialColorMaps.OrRd);
        Ship(SequentialColorMaps.PuBu);
        Ship(SequentialColorMaps.YlGn);
        Ship(SequentialColorMaps.BuGn);
        Ship(SequentialColorMaps.Hot);
        Ship(SequentialColorMaps.Copper);
        Ship(SequentialColorMaps.Bone);
        Ship(SequentialColorMaps.BuPu);
        Ship(SequentialColorMaps.GnBu);
        Ship(SequentialColorMaps.PuRd);
        Ship(SequentialColorMaps.RdPu);
        Ship(SequentialColorMaps.YlGnBu);
        Ship(SequentialColorMaps.PuBuGn);
        Ship(SequentialColorMaps.Cubehelix);

        // Diverging
        Ship(DivergingColorMaps.RdBu);
        Ship(DivergingColorMaps.RdYlGn);
        Ship(DivergingColorMaps.RdYlBu);
        Ship(DivergingColorMaps.BrBG);
        Ship(DivergingColorMaps.PiYG);
        Ship(DivergingColorMaps.Spectral);
        Ship(DivergingColorMaps.PuOr);
        Ship(DivergingColorMaps.Seismic);
        Ship(DivergingColorMaps.Bwr);

        // Cyclic
        Ship(CyclicColorMaps.Twilight);
        Ship(CyclicColorMaps.TwilightShifted);
        Ship(CyclicColorMaps.Hsv);

        // Additional
        Ship(AdditionalColorMaps.Gray);
        Ship(AdditionalColorMaps.Spring);
        Ship(AdditionalColorMaps.Summer);
        Ship(AdditionalColorMaps.Autumn);
        Ship(AdditionalColorMaps.Winter);
        Ship(AdditionalColorMaps.Cool);
        Ship(AdditionalColorMaps.AfmHot);
        Ship(AdditionalColorMaps.PRGn);
        Ship(AdditionalColorMaps.RdGy);
        Ship(AdditionalColorMaps.Rainbow);
        Ship(AdditionalColorMaps.Ocean);
        Ship(AdditionalColorMaps.Terrain);
        Ship(AdditionalColorMaps.CMRmap);

        // Seaborn perceptually-uniform
        Ship(PerceptualColorMaps2.Rocket);
        Ship(PerceptualColorMaps2.Mako);
        Ship(PerceptualColorMaps2.Crest);
        Ship(PerceptualColorMaps2.Flare);
        Ship(PerceptualColorMaps2.Icefire);

        // Qualitative
        Ship(QualitativeColorMaps.Tab10);
        Ship(QualitativeColorMaps.Tab20);
        Ship(QualitativeColorMaps.Set1);
        Ship(QualitativeColorMaps.Set2);
        Ship(QualitativeColorMaps.Set3);
        Ship(QualitativeColorMaps.Pastel1);
        Ship(QualitativeColorMaps.Pastel2);
        Ship(QualitativeColorMaps.Dark2);
        Ship(QualitativeColorMaps.Accent);
        Ship(QualitativeColorMaps.Paired);
        Ship(QualitativeColorMaps.OkabeIto);
        Ship(QualitativeColorMaps.Petroff6);
        Ship(QualitativeColorMaps.Petroff8);
        Ship(QualitativeColorMaps.Petroff10);
    }

    /// <summary>Registers a colormap and its reversed variant (<c>{name}_r</c>).</summary>
    /// <param name="map">The map to register.</param>
    /// <returns>The reversed variant, as registered.</returns>
    internal static IColorMap RegisterWithItsReverse(IColorMap map)
    {
        Maps[map.Name] = map;
        var reversed = new ReversedColorMap(map);
        Maps[reversed.Name] = reversed;
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
    public static void Register(string name, IColorMap map) => Maps[name] = map;

    /// <summary>Gets a colormap by name (case-insensitive), or null if not found.</summary>
    public static IColorMap? Get(string name) => Maps.TryGetValue(name, out var map) ? map : null;

    /// <summary>Gets all registered colormap names.</summary>
    public static IEnumerable<string> Names => Maps.Keys;
}
