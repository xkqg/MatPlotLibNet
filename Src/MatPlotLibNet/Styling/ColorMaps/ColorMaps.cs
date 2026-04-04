// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides built-in color maps for mapping scalar values to colors.
/// </summary>
public static class ColorMaps
{
    /// <summary>Gets the Viridis color map (dark purple to teal to yellow).</summary>
    public static IColorMap Viridis { get; } = new LerpColorMap("viridis",
    [
        Color.FromHex("#440154"),
        Color.FromHex("#482777"),
        Color.FromHex("#3F4A8A"),
        Color.FromHex("#31678E"),
        Color.FromHex("#26838F"),
        Color.FromHex("#1F9D8A"),
        Color.FromHex("#6CCE59"),
        Color.FromHex("#B6DE2B"),
        Color.FromHex("#FDE725"),
    ]);

    /// <summary>Gets the Plasma color map (dark purple to magenta to orange to yellow).</summary>
    public static IColorMap Plasma { get; } = new LerpColorMap("plasma",
    [
        Color.FromHex("#0D0887"),
        Color.FromHex("#46039F"),
        Color.FromHex("#7201A8"),
        Color.FromHex("#9C179E"),
        Color.FromHex("#BD3786"),
        Color.FromHex("#D8576B"),
        Color.FromHex("#ED7953"),
        Color.FromHex("#FB9F3A"),
        Color.FromHex("#FDC328"),
        Color.FromHex("#F0F921"),
    ]);

    /// <summary>Gets the Inferno color map (black to dark red to orange to yellow to white).</summary>
    public static IColorMap Inferno { get; } = new LerpColorMap("inferno",
    [
        Color.FromHex("#000004"),
        Color.FromHex("#1B0C41"),
        Color.FromHex("#4A0C6B"),
        Color.FromHex("#781C6D"),
        Color.FromHex("#A52C60"),
        Color.FromHex("#CF4446"),
        Color.FromHex("#ED6925"),
        Color.FromHex("#FB9B06"),
        Color.FromHex("#F7D13D"),
        Color.FromHex("#FCFFA4"),
    ]);

    /// <summary>Gets the Magma color map (black to purple to pink to light).</summary>
    public static IColorMap Magma { get; } = new LerpColorMap("magma",
    [
        Color.FromHex("#000004"),
        Color.FromHex("#180F3D"),
        Color.FromHex("#440F76"),
        Color.FromHex("#721F81"),
        Color.FromHex("#9E2F7F"),
        Color.FromHex("#CD4071"),
        Color.FromHex("#F1605D"),
        Color.FromHex("#FD9668"),
        Color.FromHex("#FECA8D"),
        Color.FromHex("#FCFDBF"),
    ]);

    /// <summary>Gets the Coolwarm diverging color map (blue to white to red).</summary>
    public static IColorMap Coolwarm { get; } = new LerpColorMap("coolwarm",
    [
        Color.FromHex("#3B4CC0"),
        Color.FromHex("#6788EE"),
        Color.FromHex("#9ABBFF"),
        Color.FromHex("#C9D7EF"),
        Color.FromHex("#EDDBD5"),
        Color.FromHex("#F0A582"),
        Color.FromHex("#E26952"),
        Color.FromHex("#B40426"),
    ]);

    /// <summary>Gets the Blues sequential color map (white to dark blue).</summary>
    public static IColorMap Blues { get; } = new LerpColorMap("blues",
    [
        Color.FromHex("#F7FBFF"),
        Color.FromHex("#DEEBF7"),
        Color.FromHex("#C6DBEF"),
        Color.FromHex("#9ECAE1"),
        Color.FromHex("#6BAED6"),
        Color.FromHex("#4292C6"),
        Color.FromHex("#2171B5"),
        Color.FromHex("#084594"),
    ]);

    /// <summary>Gets the Reds sequential color map (white to dark red).</summary>
    public static IColorMap Reds { get; } = new LerpColorMap("reds",
    [
        Color.FromHex("#FFF5F0"),
        Color.FromHex("#FEE0D2"),
        Color.FromHex("#FCBBA1"),
        Color.FromHex("#FC9272"),
        Color.FromHex("#FB6A4A"),
        Color.FromHex("#EF3B2C"),
        Color.FromHex("#CB181D"),
        Color.FromHex("#99000D"),
    ]);
}
