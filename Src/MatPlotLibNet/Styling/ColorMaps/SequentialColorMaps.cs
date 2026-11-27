// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides sequential color maps that progress from low to high intensity through a single hue.
/// </summary>
public static class SequentialColorMaps
{
    internal static ColorMapShelf Shelf { get; } = new();

    /// <summary>Dark blue to yellow through grey, designed to look the same under the common forms of colour vision deficiency (matplotlib's own table for <c>cividis</c>).</summary>
    public static IColorMap Cividis { get; } = Shelf.Add(MatplotlibColorTables.Of("cividis"));

    /// <summary>Near-white to dark green, so larger values read darker (matplotlib's own table for <c>Greens</c>).</summary>
    public static IColorMap Greens { get; } = Shelf.Add(MatplotlibColorTables.Of("greens"));

    /// <summary>Near-white to dark orange-brown, so larger values read darker (matplotlib's own table for <c>Oranges</c>).</summary>
    public static IColorMap Oranges { get; } = Shelf.Add(MatplotlibColorTables.Of("oranges"));

    /// <summary>Near-white to dark purple, so larger values read darker (matplotlib's own table for <c>Purples</c>).</summary>
    public static IColorMap Purples { get; } = Shelf.Add(MatplotlibColorTables.Of("purples"));

    /// <summary>White to near-black, so larger values read darker (matplotlib's own table for <c>Greys</c>).</summary>
    public static IColorMap Greys { get; } = Shelf.Add(MatplotlibColorTables.Of("greys"));

    /// <summary>Pale yellow through orange to dark brown (matplotlib's own table for <c>YlOrBr</c>).</summary>
    public static IColorMap YlOrBr { get; } = Shelf.Add(MatplotlibColorTables.Of("ylorbr"));

    /// <summary>Pale yellow through orange to dark red (matplotlib's own table for <c>YlOrRd</c>).</summary>
    public static IColorMap YlOrRd { get; } = Shelf.Add(MatplotlibColorTables.Of("ylorrd"));

    /// <summary>Pale cream through orange to dark red (matplotlib's own table for <c>OrRd</c>).</summary>
    public static IColorMap OrRd { get; } = Shelf.Add(MatplotlibColorTables.Of("orrd"));

    /// <summary>Near-white through light purple-blue to deep blue (matplotlib's own table for <c>PuBu</c>).</summary>
    public static IColorMap PuBu { get; } = Shelf.Add(MatplotlibColorTables.Of("pubu"));

    /// <summary>Pale yellow through light green to dark green (matplotlib's own table for <c>YlGn</c>).</summary>
    public static IColorMap YlGn { get; } = Shelf.Add(MatplotlibColorTables.Of("ylgn"));

    /// <summary>Near-white through pale blue-green to dark green (matplotlib's own table for <c>BuGn</c>).</summary>
    public static IColorMap BuGn { get; } = Shelf.Add(MatplotlibColorTables.Of("bugn"));

    /// <summary>Black through red and orange to yellow and white, like a heated metal (matplotlib's own table for <c>hot</c>).</summary>
    public static IColorMap Hot { get; } = Shelf.Add(MatplotlibColorTables.Of("hot"));

    /// <summary>Black to a light copper tone (matplotlib's own table for <c>copper</c>).</summary>
    public static IColorMap Copper { get; } = Shelf.Add(MatplotlibColorTables.Of("copper"));

    /// <summary>Black through blue-tinted greys to near-white, the look of an X-ray plate (matplotlib's own table for <c>bone</c>).</summary>
    public static IColorMap Bone { get; } = Shelf.Add(MatplotlibColorTables.Of("bone"));

    /// <summary>Near-white through pale blue to dark purple (matplotlib's own table for <c>BuPu</c>).</summary>
    public static IColorMap BuPu { get; } = Shelf.Add(MatplotlibColorTables.Of("bupu"));

    /// <summary>Near-white through light green and teal to dark blue (matplotlib's own table for <c>GnBu</c>).</summary>
    public static IColorMap GnBu { get; } = Shelf.Add(MatplotlibColorTables.Of("gnbu"));

    /// <summary>Near-white through lilac and pink to dark red-violet (matplotlib's own table for <c>PuRd</c>).</summary>
    public static IColorMap PuRd { get; } = Shelf.Add(MatplotlibColorTables.Of("purd"));

    /// <summary>Near-white through pink to dark purple (matplotlib's own table for <c>RdPu</c>).</summary>
    public static IColorMap RdPu { get; } = Shelf.Add(MatplotlibColorTables.Of("rdpu"));

    /// <summary>Pale yellow through green and teal to dark blue (matplotlib's own table for <c>YlGnBu</c>).</summary>
    public static IColorMap YlGnBu { get; } = Shelf.Add(MatplotlibColorTables.Of("ylgnbu"));

    /// <summary>Black to white along a helix of hues, with brightness rising steadily so it also reads in grayscale (matplotlib's own table for <c>cubehelix</c>).</summary>
    public static IColorMap Cubehelix { get; } = Shelf.Add(MatplotlibColorTables.Of("cubehelix"));

    /// <summary>Near-white through light purple-blue and blue to dark green (matplotlib's own table for <c>PuBuGn</c>).</summary>
    public static IColorMap PuBuGn { get; } = Shelf.Add(MatplotlibColorTables.Of("pubugn"));
}
