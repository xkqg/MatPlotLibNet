// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Thread-safe registry for looking up colormaps by name. All built-in colormaps
/// and their reversed variants are registered automatically.</summary>
public static class ColorMapRegistry
{
    private static readonly ConcurrentDictionary<string, IColorMap> Maps = new(StringComparer.OrdinalIgnoreCase);

    static ColorMapRegistry()
    {
        // Perceptually-uniform (ColorMaps)
        RegisterBuiltIn(ColorMaps.Viridis);
        RegisterBuiltIn(ColorMaps.Plasma);
        RegisterBuiltIn(ColorMaps.Inferno);
        RegisterBuiltIn(ColorMaps.Magma);
        RegisterBuiltIn(ColorMaps.Coolwarm);
        RegisterBuiltIn(ColorMaps.Blues);
        RegisterBuiltIn(ColorMaps.Reds);
        RegisterBuiltIn(ColorMaps.Turbo);
        RegisterBuiltIn(ColorMaps.Jet);

        // Sequential
        RegisterBuiltIn(SequentialColorMaps.Cividis);
        RegisterBuiltIn(SequentialColorMaps.Greens);
        RegisterBuiltIn(SequentialColorMaps.Oranges);
        RegisterBuiltIn(SequentialColorMaps.Purples);
        RegisterBuiltIn(SequentialColorMaps.Greys);
        RegisterBuiltIn(SequentialColorMaps.YlOrBr);
        RegisterBuiltIn(SequentialColorMaps.YlOrRd);
        RegisterBuiltIn(SequentialColorMaps.OrRd);
        RegisterBuiltIn(SequentialColorMaps.PuBu);
        RegisterBuiltIn(SequentialColorMaps.YlGn);
        RegisterBuiltIn(SequentialColorMaps.BuGn);
        RegisterBuiltIn(SequentialColorMaps.Hot);
        RegisterBuiltIn(SequentialColorMaps.Copper);
        RegisterBuiltIn(SequentialColorMaps.Bone);
        RegisterBuiltIn(SequentialColorMaps.BuPu);
        RegisterBuiltIn(SequentialColorMaps.GnBu);
        RegisterBuiltIn(SequentialColorMaps.PuRd);
        RegisterBuiltIn(SequentialColorMaps.RdPu);
        RegisterBuiltIn(SequentialColorMaps.YlGnBu);
        RegisterBuiltIn(SequentialColorMaps.PuBuGn);
        RegisterBuiltIn(SequentialColorMaps.Cubehelix);

        // Diverging
        RegisterBuiltIn(DivergingColorMaps.RdBu);
        RegisterBuiltIn(DivergingColorMaps.RdYlGn);
        RegisterBuiltIn(DivergingColorMaps.RdYlBu);
        RegisterBuiltIn(DivergingColorMaps.BrBG);
        RegisterBuiltIn(DivergingColorMaps.PiYG);
        RegisterBuiltIn(DivergingColorMaps.Spectral);
        RegisterBuiltIn(DivergingColorMaps.PuOr);
        RegisterBuiltIn(DivergingColorMaps.Seismic);
        RegisterBuiltIn(DivergingColorMaps.Bwr);

        // Cyclic
        RegisterBuiltIn(CyclicColorMaps.Twilight);
        RegisterBuiltIn(CyclicColorMaps.TwilightShifted);
        RegisterBuiltIn(CyclicColorMaps.Hsv);

        // Qualitative
        RegisterBuiltIn(QualitativeColorMaps.Tab10);
        RegisterBuiltIn(QualitativeColorMaps.Tab20);
        RegisterBuiltIn(QualitativeColorMaps.Set1);
        RegisterBuiltIn(QualitativeColorMaps.Set2);
        RegisterBuiltIn(QualitativeColorMaps.Set3);
        RegisterBuiltIn(QualitativeColorMaps.Pastel1);
        RegisterBuiltIn(QualitativeColorMaps.Pastel2);
        RegisterBuiltIn(QualitativeColorMaps.Dark2);
        RegisterBuiltIn(QualitativeColorMaps.Accent);
        RegisterBuiltIn(QualitativeColorMaps.Paired);
    }

    /// <summary>Registers a colormap and its reversed variant.</summary>
    internal static void RegisterBuiltIn(IColorMap map)
    {
        Maps[map.Name] = map;
        var reversed = new ReversedColorMap(map);
        Maps[reversed.Name] = reversed;
    }

    /// <summary>Registers a custom colormap by name.</summary>
    public static void Register(string name, IColorMap map) => Maps[name] = map;

    /// <summary>Gets a colormap by name (case-insensitive), or null if not found.</summary>
    public static IColorMap? Get(string name) => Maps.TryGetValue(name, out var map) ? map : null;

    /// <summary>Gets all registered colormap names.</summary>
    public static IEnumerable<string> Names => Maps.Keys;
}
