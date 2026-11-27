// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>The one owner of matplotlib's own colour tables: every colour map the library ships under a name matplotlib has
/// (<c>viridis</c>, <c>plasma</c>, <c>Blues</c>, <c>jet</c>, ...) is made here from the data matplotlib 3.11.2 holds for it, never typed
/// by hand from a handful of stops. A map matplotlib lists (<c>viridis</c>, <c>plasma</c>, <c>inferno</c>, <c>magma</c>,
/// <c>cividis</c>, <c>turbo</c>, <c>twilight</c>) is its entries; a map it makes from segment data whose segments meet at equal
/// distances (the ColorBrewer maps, <c>coolwarm</c>) is the colours where they meet, which a linear blend between them reproduces; any
/// other (<c>jet</c>, <c>hot</c>, <c>bone</c>, <c>afmhot</c>, <c>ocean</c>, <c>rainbow</c>, <c>cubehelix</c>) is the 256 entries of its
/// table. Every map is therefore colours at equal distances, found in constant time: a lookup between colours at unequal distances is
/// a search, which measured twice the time of this one. The data is <c>MatplotlibColorTables.Data.cs</c>, written by
/// <c>tools/mpl_reference/generate_colormaps.py</c>; each colour is a byte as matplotlib's renderers write it, which is the level a
/// picture drawn with the map carries.</summary>
internal static partial class MatplotlibColorTables
{
    /// <summary>The library's colour map of the given name, made of matplotlib's table for it.</summary>
    /// <param name="name">The library's name of the map: matplotlib's, in lower case (<c>blues</c>, <c>twilight_shifted</c>).</param>
    /// <returns>The map, blending linearly between neighbouring colours of the table.</returns>
    /// <exception cref="ArgumentException">matplotlib has no table for the name here.</exception>
    internal static LinearColorMap Of(string name) =>
        MakeFromTable(name) ?? throw new ArgumentException($"No matplotlib table for the colour map '{name}'.", nameof(name));

    /// <summary>A map whose colours stand at equal distances from the first to the last.</summary>
    private static LinearColorMap Evenly(string name, ReadOnlySpan<uint> rgb) => new(name, ColoursOf(rgb));

    /// <summary>The colours of a table written as 0xRRGGBB.</summary>
    private static Color[] ColoursOf(ReadOnlySpan<uint> rgb)
    {
        var colours = new Color[rgb.Length];
        for (int i = 0; i < colours.Length; i++)
            colours[i] = new Color((byte)(rgb[i] >> 16), (byte)(rgb[i] >> 8), (byte)rgb[i]);
        return colours;
    }
}
