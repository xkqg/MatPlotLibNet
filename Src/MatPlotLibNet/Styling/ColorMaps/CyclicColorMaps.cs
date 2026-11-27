// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides cyclic color maps where the start and end colors match,
/// suitable for periodic data such as phase angles or time of day.
/// </summary>
public static class CyclicColorMaps
{
    internal static ColorMapShelf Shelf { get; } = new();

    /// <summary>Pale lavender through blue-purple to a near-black purple, then brown and tan back to the pale start colour (matplotlib's own table for <c>twilight</c>).</summary>
    public static IColorMap Twilight { get; } = Shelf.Add(MatplotlibColorTables.Of("twilight"));

    /// <summary>The <see cref="Twilight"/> cycle rotated by half a turn: it starts and ends at the dark purple and is palest in the middle (matplotlib's own table for <c>twilight_shifted</c>).</summary>
    public static IColorMap TwilightShifted { get; } = Shelf.Add(MatplotlibColorTables.Of("twilight_shifted"));

    /// <summary>The hue wheel at full saturation: red, yellow, green, cyan, blue and magenta, then back to red (matplotlib's own table for <c>hsv</c>).</summary>
    public static IColorMap Hsv { get; } = Shelf.Add(MatplotlibColorTables.Of("hsv"));
}
