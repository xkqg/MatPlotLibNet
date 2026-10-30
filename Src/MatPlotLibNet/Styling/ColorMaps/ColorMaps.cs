// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides built-in color maps for mapping scalar values to colors.
/// </summary>
public static class ColorMaps
{
    /// <summary>Perceptually uniform map from dark purple through teal and green to yellow, nine stops after matplotlib's <c>viridis</c>. A sound default for continuous data.</summary>
    public static IColorMap Viridis { get; } = new LinearColorMap("viridis",
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

    /// <summary>Perceptually uniform map from deep blue-violet through magenta and orange to bright yellow, after matplotlib's <c>plasma</c>.</summary>
    public static IColorMap Plasma { get; } = new LinearColorMap("plasma",
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

    /// <summary>Perceptually uniform map from black through purple, red and orange to pale yellow, after matplotlib's <c>inferno</c>.</summary>
    public static IColorMap Inferno { get; } = new LinearColorMap("inferno",
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

    /// <summary>Perceptually uniform map from black through purple and pink to pale cream, after matplotlib's <c>magma</c>.</summary>
    public static IColorMap Magma { get; } = new LinearColorMap("magma",
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

    /// <summary>Diverging map from a cool blue through a pale grey centre to a warm red: blue for low values, red for high ones (after matplotlib's <c>coolwarm</c>).</summary>
    public static IColorMap Coolwarm { get; } = new LinearColorMap("coolwarm",
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

    /// <summary>Sequential map from near-white to dark blue, so larger values read darker (after matplotlib's <c>Blues</c>).</summary>
    public static IColorMap Blues { get; } = new LinearColorMap("blues",
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

    /// <summary>Sequential map from near-white to dark red, so larger values read darker (after matplotlib's <c>Reds</c>).</summary>
    public static IColorMap Reds { get; } = new LinearColorMap("reds",
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

    /// <summary>Smoothed rainbow from dark blue through cyan, green and yellow to dark red, made as a better-behaved replacement for <see cref="Jet"/> (after matplotlib's <c>turbo</c>).</summary>
    public static IColorMap Turbo { get; } = new LinearColorMap("turbo",
    [
        Color.FromHex("#30123B"),
        Color.FromHex("#4662D7"),
        Color.FromHex("#36AAF9"),
        Color.FromHex("#1AE4B6"),
        Color.FromHex("#72FE5E"),
        Color.FromHex("#C8EF34"),
        Color.FromHex("#FABA39"),
        Color.FromHex("#F66B19"),
        Color.FromHex("#CA2A04"),
        Color.FromHex("#7A0403"),
    ]);

    /// <summary>The classic rainbow from dark blue through cyan and yellow to dark red (matplotlib's <c>jet</c>). Its brightness is uneven along the scale, so <see cref="Viridis"/> is usually the better choice for quantitative data.</summary>
    public static IColorMap Jet { get; } = new LinearColorMap("jet",
    [
        Color.FromHex("#000080"),
        Color.FromHex("#0000FF"),
        Color.FromHex("#0080FF"),
        Color.FromHex("#00FFFF"),
        Color.FromHex("#80FF80"),
        Color.FromHex("#FFFF00"),
        Color.FromHex("#FF8000"),
        Color.FromHex("#FF0000"),
        Color.FromHex("#800000"),
    ]);

    /// <summary>Gets a colormap by name (case-insensitive), or null if not found.</summary>
    public static IColorMap? Get(string name) => ColorMapRegistry.Get(name);

    /// <summary>Gets all registered colormaps (including reversed variants).</summary>
    public static IEnumerable<IColorMap> All =>
        ColorMapRegistry.Names.Select(n => ColorMapRegistry.Get(n)!);
}
