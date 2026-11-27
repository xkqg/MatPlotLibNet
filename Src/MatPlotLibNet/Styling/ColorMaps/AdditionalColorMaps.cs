// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Provides additional color maps extending the matplotlib palette.</summary>
public static class AdditionalColorMaps
{
    internal static ColorMapShelf Shelf { get; } = new();

    /// <summary>Linear ramp from black to white (the matplotlib <c>gray</c> map).</summary>
    public static IColorMap Gray { get; } = Shelf.Add(MatplotlibColorTables.Of("gray"));

    /// <summary>Linear ramp from magenta to yellow (the matplotlib <c>spring</c> map).</summary>
    public static IColorMap Spring { get; } = Shelf.Add(MatplotlibColorTables.Of("spring"));

    /// <summary>Linear ramp from a dark sea green to a pale yellow (the matplotlib <c>summer</c> map).</summary>
    public static IColorMap Summer { get; } = Shelf.Add(MatplotlibColorTables.Of("summer"));

    /// <summary>Linear ramp from red to yellow (the matplotlib <c>autumn</c> map).</summary>
    public static IColorMap Autumn { get; } = Shelf.Add(MatplotlibColorTables.Of("autumn"));

    /// <summary>Linear ramp from blue to a sea green (the matplotlib <c>winter</c> map).</summary>
    public static IColorMap Winter { get; } = Shelf.Add(MatplotlibColorTables.Of("winter"));

    /// <summary>Linear ramp from cyan to magenta (the matplotlib <c>cool</c> map).</summary>
    public static IColorMap Cool { get; } = Shelf.Add(MatplotlibColorTables.Of("cool"));

    /// <summary>Black through dark red and orange to yellow and white, matplotlib's own table for <c>afmhot</c>.</summary>
    public static IColorMap AfmHot { get; } = Shelf.Add(MatplotlibColorTables.Of("afmhot"));

    /// <summary>Diverging map from dark purple through a near-white centre to dark green: purple for low values, green for high ones (matplotlib's own table for <c>PRGn</c>).</summary>
    public static IColorMap PRGn { get; } = Shelf.Add(MatplotlibColorTables.Of("prgn"));

    /// <summary>Diverging map from dark red through white to a near-black grey: red for low values, grey for high ones (matplotlib's own table for <c>RdGy</c>).</summary>
    public static IColorMap RdGy { get; } = Shelf.Add(MatplotlibColorTables.Of("rdgy"));

    /// <summary>The spectrum from blue through cyan, green and yellow to red, matplotlib's own table for <c>rainbow</c>.</summary>
    public static IColorMap Rainbow { get; } = Shelf.Add(MatplotlibColorTables.Of("rainbow"));

    /// <summary>Dark blue through teal and green to white, matplotlib's own table for <c>ocean</c>.</summary>
    public static IColorMap Ocean { get; } = Shelf.Add(MatplotlibColorTables.Of("ocean"));

    /// <summary>Deep blue for the lowest values, then light blue, green lowlands and tan up to white peaks, like a relief map (matplotlib's own table for <c>terrain</c>).</summary>
    public static IColorMap Terrain { get; } = Shelf.Add(MatplotlibColorTables.Of("terrain"));

    /// <summary>Black through blue, teal, green and orange to red and white, with brightness rising steadily so the map also reads in grayscale (matplotlib's own table for <c>CMRmap</c>).</summary>
    public static IColorMap CMRmap { get; } = Shelf.Add(MatplotlibColorTables.Of("cmrmap"));
}
