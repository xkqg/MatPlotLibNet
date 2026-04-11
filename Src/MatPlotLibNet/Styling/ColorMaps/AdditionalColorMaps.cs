// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>Provides additional color maps extending the matplotlib palette.</summary>
public static class AdditionalColorMaps
{
    /// <summary>Gets the Gray color map (black to white).</summary>
    public static IColorMap Gray { get; } = new LinearColorMap("gray",
    [
        Color.FromHex("#000000"),
        Color.FromHex("#FFFFFF"),
    ]);

    /// <summary>Gets the Spring color map (magenta to yellow).</summary>
    public static IColorMap Spring { get; } = new LinearColorMap("spring",
    [
        Color.FromHex("#FF00FF"),
        Color.FromHex("#FFFF00"),
    ]);

    /// <summary>Gets the Summer color map (green to yellow).</summary>
    public static IColorMap Summer { get; } = new LinearColorMap("summer",
    [
        Color.FromHex("#008066"),
        Color.FromHex("#FFFF66"),
    ]);

    /// <summary>Gets the Autumn color map (red to yellow).</summary>
    public static IColorMap Autumn { get; } = new LinearColorMap("autumn",
    [
        Color.FromHex("#FF0000"),
        Color.FromHex("#FFFF00"),
    ]);

    /// <summary>Gets the Winter color map (blue to green).</summary>
    public static IColorMap Winter { get; } = new LinearColorMap("winter",
    [
        Color.FromHex("#0000FF"),
        Color.FromHex("#00FF80"),
    ]);

    /// <summary>Gets the Cool color map (cyan to magenta).</summary>
    public static IColorMap Cool { get; } = new LinearColorMap("cool",
    [
        Color.FromHex("#00FFFF"),
        Color.FromHex("#FF00FF"),
    ]);

    /// <summary>Gets the afmhot color map (black through orange to white, like a hot iron).</summary>
    public static IColorMap AfmHot { get; } = new LinearColorMap("afmhot",
    [
        Color.FromHex("#000000"),
        Color.FromHex("#800000"),
        Color.FromHex("#FF8000"),
        Color.FromHex("#FFFF00"),
        Color.FromHex("#FFFFFF"),
    ]);

    /// <summary>Gets the PRGn diverging color map (purple to white to green).</summary>
    public static IColorMap PRGn { get; } = new LinearColorMap("prgn",
    [
        Color.FromHex("#40004B"),
        Color.FromHex("#762A83"),
        Color.FromHex("#C2A5CF"),
        Color.FromHex("#F7F7F7"),
        Color.FromHex("#ACD39E"),
        Color.FromHex("#4DAC26"),
        Color.FromHex("#00441B"),
    ]);

    /// <summary>Gets the RdGy diverging color map (red to white to gray).</summary>
    public static IColorMap RdGy { get; } = new LinearColorMap("rdgy",
    [
        Color.FromHex("#67001F"),
        Color.FromHex("#D6604D"),
        Color.FromHex("#FDDBC7"),
        Color.FromHex("#FFFFFF"),
        Color.FromHex("#BABABA"),
        Color.FromHex("#4D4D4D"),
        Color.FromHex("#1A1A1A"),
    ]);

    /// <summary>Gets the Rainbow color map (blue to cyan to green to yellow to red).</summary>
    public static IColorMap Rainbow { get; } = new LinearColorMap("rainbow",
    [
        Color.FromHex("#0000FF"),
        Color.FromHex("#00FFFF"),
        Color.FromHex("#00FF00"),
        Color.FromHex("#FFFF00"),
        Color.FromHex("#FF0000"),
    ]);

    /// <summary>Gets the Ocean color map (dark blue through green to white).</summary>
    public static IColorMap Ocean { get; } = new LinearColorMap("ocean",
    [
        Color.FromHex("#00004C"),
        Color.FromHex("#003399"),
        Color.FromHex("#006666"),
        Color.FromHex("#00CC00"),
        Color.FromHex("#FFFFFF"),
    ]);

    /// <summary>Gets the Terrain color map (blue for water, green for land, brown for mountains, white for peaks).</summary>
    public static IColorMap Terrain { get; } = new LinearColorMap("terrain",
    [
        Color.FromHex("#333399"),
        Color.FromHex("#1A99FF"),
        Color.FromHex("#33CC33"),
        Color.FromHex("#99CC33"),
        Color.FromHex("#CC9966"),
        Color.FromHex("#FFFFFF"),
    ]);

    /// <summary>Gets the CMRmap color map (black to blue to green to red to white).</summary>
    public static IColorMap CMRmap { get; } = new LinearColorMap("cmrmap",
    [
        Color.FromHex("#000000"),
        Color.FromHex("#1F1FBF"),
        Color.FromHex("#007FBF"),
        Color.FromHex("#00BF7F"),
        Color.FromHex("#7FBF00"),
        Color.FromHex("#BF7F00"),
        Color.FromHex("#BF3F00"),
        Color.FromHex("#FF0000"),
        Color.FromHex("#FFFFFF"),
    ]);
}
