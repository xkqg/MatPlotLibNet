// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides diverging color maps that emphasize deviation from a central value,
/// using two contrasting hues that meet at a neutral midpoint.
/// </summary>
public static class DivergingColorMaps
{
    /// <summary>Gets the RdBu color map (red to blue).</summary>
    public static IColorMap RdBu { get; } = new LerpColorMap("rdbu",
    [
        Color.FromHex("#67001F"),
        Color.FromHex("#B2182B"),
        Color.FromHex("#D6604D"),
        Color.FromHex("#F4A582"),
        Color.FromHex("#FDDBC7"),
        Color.FromHex("#D1E5F0"),
        Color.FromHex("#92C5DE"),
        Color.FromHex("#4393C3"),
        Color.FromHex("#2166AC"),
        Color.FromHex("#053061"),
    ]);

    /// <summary>Gets the RdYlGn color map (red to green via yellow).</summary>
    public static IColorMap RdYlGn { get; } = new LerpColorMap("rdylgn",
    [
        Color.FromHex("#A50026"),
        Color.FromHex("#D73027"),
        Color.FromHex("#F46D43"),
        Color.FromHex("#FDAE61"),
        Color.FromHex("#FEE08B"),
        Color.FromHex("#D9EF8B"),
        Color.FromHex("#A6D96A"),
        Color.FromHex("#66BD63"),
        Color.FromHex("#1A9850"),
        Color.FromHex("#006837"),
    ]);

    /// <summary>Gets the RdYlBu color map (red to blue via yellow).</summary>
    public static IColorMap RdYlBu { get; } = new LerpColorMap("rdylbu",
    [
        Color.FromHex("#A50026"),
        Color.FromHex("#D73027"),
        Color.FromHex("#F46D43"),
        Color.FromHex("#FDAE61"),
        Color.FromHex("#FEE090"),
        Color.FromHex("#E0F3F8"),
        Color.FromHex("#ABD9E9"),
        Color.FromHex("#74ADD1"),
        Color.FromHex("#4575B4"),
        Color.FromHex("#313695"),
    ]);

    /// <summary>Gets the BrBG color map (brown to teal).</summary>
    public static IColorMap BrBG { get; } = new LerpColorMap("brbg",
    [
        Color.FromHex("#543005"),
        Color.FromHex("#8C510A"),
        Color.FromHex("#BF812D"),
        Color.FromHex("#DFC27D"),
        Color.FromHex("#F6E8C3"),
        Color.FromHex("#C7EAE5"),
        Color.FromHex("#80CDC1"),
        Color.FromHex("#35978F"),
        Color.FromHex("#01665E"),
        Color.FromHex("#003C30"),
    ]);

    /// <summary>Gets the PiYG color map (pink to green).</summary>
    public static IColorMap PiYG { get; } = new LerpColorMap("piyg",
    [
        Color.FromHex("#8E0152"),
        Color.FromHex("#C51B7D"),
        Color.FromHex("#DE77AE"),
        Color.FromHex("#F1B6DA"),
        Color.FromHex("#FDE0EF"),
        Color.FromHex("#E6F5D0"),
        Color.FromHex("#B8E186"),
        Color.FromHex("#7FBC41"),
        Color.FromHex("#4D9221"),
        Color.FromHex("#276419"),
    ]);

    /// <summary>Gets the Spectral color map (red to blue via rainbow).</summary>
    public static IColorMap Spectral { get; } = new LerpColorMap("spectral",
    [
        Color.FromHex("#9E0142"),
        Color.FromHex("#D53E4F"),
        Color.FromHex("#F46D43"),
        Color.FromHex("#FDAE61"),
        Color.FromHex("#FEE08B"),
        Color.FromHex("#E6F598"),
        Color.FromHex("#ABDDA4"),
        Color.FromHex("#66C2A5"),
        Color.FromHex("#3288BD"),
        Color.FromHex("#5E4FA2"),
    ]);

    /// <summary>Gets the PuOr color map (dark purple to dark orange).</summary>
    public static IColorMap PuOr { get; } = new LerpColorMap("puor",
    [
        Color.FromHex("#2D004B"),
        Color.FromHex("#542788"),
        Color.FromHex("#8073AC"),
        Color.FromHex("#B2ABD2"),
        Color.FromHex("#D8DAEB"),
        Color.FromHex("#FEE0B6"),
        Color.FromHex("#FDB863"),
        Color.FromHex("#E08214"),
        Color.FromHex("#B35806"),
        Color.FromHex("#7F3B08"),
    ]);

    /// <summary>Gets the Seismic color map (dark blue to dark red with strong contrast).</summary>
    public static IColorMap Seismic { get; } = new LerpColorMap("seismic",
    [
        Color.FromHex("#00004C"),
        Color.FromHex("#0000B3"),
        Color.FromHex("#0000FF"),
        Color.FromHex("#6666FF"),
        Color.FromHex("#CCCCFF"),
        Color.FromHex("#FFFFFF"),
        Color.FromHex("#FFCCCC"),
        Color.FromHex("#FF6666"),
        Color.FromHex("#FF0000"),
        Color.FromHex("#CC0000"),
        Color.FromHex("#800000"),
    ]);

    /// <summary>Gets the Bwr color map (blue to white to red).</summary>
    public static IColorMap Bwr { get; } = new LerpColorMap("bwr",
    [
        Color.FromHex("#0000FF"),
        Color.FromHex("#4444FF"),
        Color.FromHex("#8888FF"),
        Color.FromHex("#CCCCFF"),
        Color.FromHex("#FFFFFF"),
        Color.FromHex("#FFCCCC"),
        Color.FromHex("#FF8888"),
        Color.FromHex("#FF4444"),
        Color.FromHex("#FF0000"),
    ]);
}
