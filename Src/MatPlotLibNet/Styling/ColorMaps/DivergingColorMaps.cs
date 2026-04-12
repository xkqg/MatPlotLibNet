// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides diverging color maps that emphasize deviation from a central value,
/// using two contrasting hues that meet at a neutral midpoint.
/// </summary>
public static class DivergingColorMaps
{
    public static IColorMap RdBu { get; } = new LinearColorMap("rdbu",
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

    public static IColorMap RdYlGn { get; } = new LinearColorMap("rdylgn",
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

    public static IColorMap RdYlBu { get; } = new LinearColorMap("rdylbu",
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

    public static IColorMap BrBG { get; } = new LinearColorMap("brbg",
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

    public static IColorMap PiYG { get; } = new LinearColorMap("piyg",
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

    public static IColorMap Spectral { get; } = new LinearColorMap("spectral",
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

    public static IColorMap PuOr { get; } = new LinearColorMap("puor",
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

    public static IColorMap Seismic { get; } = new LinearColorMap("seismic",
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

    public static IColorMap Bwr { get; } = new LinearColorMap("bwr",
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
