// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides built-in color maps for mapping scalar values to colors.
/// </summary>
public static class ColorMaps
{
    internal static ColorMapShelf Shelf { get; } = new();

    /// <summary>Perceptually uniform map from dark purple through teal and green to yellow: matplotlib's own table of 256 colours
    /// for <c>viridis</c>, interpolated between its entries. A sound default for continuous data.</summary>
    public static IColorMap Viridis { get; } = Shelf.Add(MatplotlibColorTables.Of("viridis"));

    /// <summary>Perceptually uniform map from deep blue-violet through magenta and orange to bright yellow, matplotlib's own table for <c>plasma</c>.</summary>
    public static IColorMap Plasma { get; } = Shelf.Add(MatplotlibColorTables.Of("plasma"));

    /// <summary>Perceptually uniform map from black through purple, red and orange to pale yellow, matplotlib's own table for <c>inferno</c>.</summary>
    public static IColorMap Inferno { get; } = Shelf.Add(MatplotlibColorTables.Of("inferno"));

    /// <summary>Perceptually uniform map from black through purple and pink to pale cream, matplotlib's own table for <c>magma</c>.</summary>
    public static IColorMap Magma { get; } = Shelf.Add(MatplotlibColorTables.Of("magma"));

    /// <summary>Diverging map from a cool blue through a pale grey centre to a warm red: blue for low values, red for high ones (matplotlib's own table for <c>coolwarm</c>).</summary>
    public static IColorMap Coolwarm { get; } = Shelf.Add(MatplotlibColorTables.Of("coolwarm"));

    /// <summary>Sequential map from near-white to dark blue, so larger values read darker (matplotlib's own table for <c>Blues</c>).</summary>
    public static IColorMap Blues { get; } = Shelf.Add(MatplotlibColorTables.Of("blues"));

    /// <summary>Sequential map from near-white to dark red, so larger values read darker (matplotlib's own table for <c>Reds</c>).</summary>
    public static IColorMap Reds { get; } = Shelf.Add(MatplotlibColorTables.Of("reds"));

    /// <summary>Smoothed rainbow from dark blue through cyan, green and yellow to dark red, made as a better-behaved replacement for <see cref="Jet"/> (matplotlib's own table for <c>turbo</c>).</summary>
    public static IColorMap Turbo { get; } = Shelf.Add(MatplotlibColorTables.Of("turbo"));

    /// <summary>The classic rainbow from dark blue through cyan and yellow to dark red (matplotlib's <c>jet</c>). Its brightness is uneven along the scale, so <see cref="Viridis"/> is usually the better choice for quantitative data.</summary>
    public static IColorMap Jet { get; } = Shelf.Add(MatplotlibColorTables.Of("jet"));

    /// <summary>Gets a colormap by name (case-insensitive), or null if not found.</summary>
    public static IColorMap? Get(string name) => ColorMapRegistry.Get(name);

    /// <summary>Gets all registered colormaps (including reversed variants).</summary>
    public static IEnumerable<IColorMap> All =>
        ColorMapRegistry.Names.Select(n => ColorMapRegistry.Get(n)!);
}
