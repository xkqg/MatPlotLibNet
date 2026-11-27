// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides diverging color maps that emphasize deviation from a central value,
/// using two contrasting hues that meet at a neutral midpoint.
/// </summary>
public static class DivergingColorMaps
{
    internal static ColorMapShelf Shelf { get; } = new();

    /// <summary>Dark red for low values, a pale centre, dark blue for high ones (matplotlib's own table for <c>RdBu</c>).</summary>
    public static IColorMap RdBu { get; } = Shelf.Add(MatplotlibColorTables.Of("rdbu"));

    /// <summary>Red for low values, yellow in the middle, green for high ones; reads as bad to good (matplotlib's own table for <c>RdYlGn</c>).</summary>
    public static IColorMap RdYlGn { get; } = Shelf.Add(MatplotlibColorTables.Of("rdylgn"));

    /// <summary>Red for low values, yellow in the middle, blue for high ones (matplotlib's own table for <c>RdYlBu</c>).</summary>
    public static IColorMap RdYlBu { get; } = Shelf.Add(MatplotlibColorTables.Of("rdylbu"));

    /// <summary>Brown for low values, a pale centre, blue-green for high ones (matplotlib's own table for <c>BrBG</c>).</summary>
    public static IColorMap BrBG { get; } = Shelf.Add(MatplotlibColorTables.Of("brbg"));

    /// <summary>Magenta-pink for low values, a pale centre, green for high ones (matplotlib's own table for <c>PiYG</c>).</summary>
    public static IColorMap PiYG { get; } = Shelf.Add(MatplotlibColorTables.Of("piyg"));

    /// <summary>Red for low values, then orange, yellow and green, ending in blue and violet for high ones (matplotlib's own table for <c>Spectral</c>).</summary>
    public static IColorMap Spectral { get; } = Shelf.Add(MatplotlibColorTables.Of("spectral"));

    /// <summary>Dark purple for low values, a pale centre, dark orange-brown for high ones.</summary>
    public static IColorMap PuOr { get; } = Shelf.Add(MatplotlibColorTables.Of("puor"));

    /// <summary>Dark blue for low values, white in the middle, dark red for high ones; suits data centred on zero (matplotlib's own table for <c>seismic</c>).</summary>
    public static IColorMap Seismic { get; } = Shelf.Add(MatplotlibColorTables.Of("seismic"));

    /// <summary>Saturated blue for low values, white in the middle, saturated red for high ones (matplotlib's own table for <c>bwr</c>).</summary>
    public static IColorMap Bwr { get; } = Shelf.Add(MatplotlibColorTables.Of("bwr"));
}
