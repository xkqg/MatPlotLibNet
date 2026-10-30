// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides sequential color maps that progress from low to high intensity through a single hue.
/// </summary>
public static class SequentialColorMaps
{
    /// <summary>Dark blue to yellow through grey, designed to look the same under the common forms of colour vision deficiency (after matplotlib's <c>cividis</c>).</summary>
    public static IColorMap Cividis { get; } = new LinearColorMap("cividis",
    [
        Color.FromHex("#00204D"),
        Color.FromHex("#414D6B"),
        Color.FromHex("#7B7B78"),
        Color.FromHex("#A69D75"),
        Color.FromHex("#CFD369"),
        Color.FromHex("#FFE945"),
    ]);

    /// <summary>Near-white to dark green, so larger values read darker (after matplotlib's <c>Greens</c>).</summary>
    public static IColorMap Greens { get; } = new LinearColorMap("greens",
    [
        Color.FromHex("#F7FCF5"),
        Color.FromHex("#E5F5E0"),
        Color.FromHex("#C7E9C0"),
        Color.FromHex("#A1D99B"),
        Color.FromHex("#74C476"),
        Color.FromHex("#41AB5D"),
        Color.FromHex("#238B45"),
        Color.FromHex("#005A32"),
    ]);

    /// <summary>Near-white to dark orange-brown, so larger values read darker (after matplotlib's <c>Oranges</c>).</summary>
    public static IColorMap Oranges { get; } = new LinearColorMap("oranges",
    [
        Color.FromHex("#FFF5EB"),
        Color.FromHex("#FEE6CE"),
        Color.FromHex("#FDD0A2"),
        Color.FromHex("#FDAE6B"),
        Color.FromHex("#FD8D3C"),
        Color.FromHex("#F16913"),
        Color.FromHex("#D94801"),
        Color.FromHex("#8C2D04"),
    ]);

    /// <summary>Near-white to dark purple, so larger values read darker (after matplotlib's <c>Purples</c>).</summary>
    public static IColorMap Purples { get; } = new LinearColorMap("purples",
    [
        Color.FromHex("#FCFBFD"),
        Color.FromHex("#EFEDF5"),
        Color.FromHex("#DADAEB"),
        Color.FromHex("#BCBDDC"),
        Color.FromHex("#9E9AC8"),
        Color.FromHex("#807DBA"),
        Color.FromHex("#6A51A3"),
        Color.FromHex("#4A1486"),
    ]);

    /// <summary>White to near-black, so larger values read darker (after matplotlib's <c>Greys</c>).</summary>
    public static IColorMap Greys { get; } = new LinearColorMap("greys",
    [
        Color.FromHex("#FFFFFF"),
        Color.FromHex("#F0F0F0"),
        Color.FromHex("#D9D9D9"),
        Color.FromHex("#BDBDBD"),
        Color.FromHex("#969696"),
        Color.FromHex("#737373"),
        Color.FromHex("#525252"),
        Color.FromHex("#252525"),
    ]);

    /// <summary>Pale yellow through orange to dark brown (after matplotlib's <c>YlOrBr</c>).</summary>
    public static IColorMap YlOrBr { get; } = new LinearColorMap("ylorbr",
    [
        Color.FromHex("#FFFFE5"),
        Color.FromHex("#FFF7BC"),
        Color.FromHex("#FEE391"),
        Color.FromHex("#FEC44F"),
        Color.FromHex("#FE9929"),
        Color.FromHex("#EC7014"),
        Color.FromHex("#CC4C02"),
        Color.FromHex("#8C2D04"),
    ]);

    /// <summary>Pale yellow through orange to dark red (after matplotlib's <c>YlOrRd</c>).</summary>
    public static IColorMap YlOrRd { get; } = new LinearColorMap("ylorrd",
    [
        Color.FromHex("#FFFFCC"),
        Color.FromHex("#FFEDA0"),
        Color.FromHex("#FED976"),
        Color.FromHex("#FEB24C"),
        Color.FromHex("#FD8D3C"),
        Color.FromHex("#FC4E2A"),
        Color.FromHex("#E31A1C"),
        Color.FromHex("#B10026"),
    ]);

    /// <summary>Pale cream through orange to dark red (after matplotlib's <c>OrRd</c>).</summary>
    public static IColorMap OrRd { get; } = new LinearColorMap("orrd",
    [
        Color.FromHex("#FFF7EC"),
        Color.FromHex("#FEE8C8"),
        Color.FromHex("#FDD49E"),
        Color.FromHex("#FDBB84"),
        Color.FromHex("#FC8D59"),
        Color.FromHex("#EF6548"),
        Color.FromHex("#D7301F"),
        Color.FromHex("#990000"),
    ]);

    /// <summary>Near-white through light purple-blue to deep blue (after matplotlib's <c>PuBu</c>).</summary>
    public static IColorMap PuBu { get; } = new LinearColorMap("pubu",
    [
        Color.FromHex("#FFF7FB"),
        Color.FromHex("#ECE7F2"),
        Color.FromHex("#D0D1E6"),
        Color.FromHex("#A6BDDB"),
        Color.FromHex("#74A9CF"),
        Color.FromHex("#3690C0"),
        Color.FromHex("#0570B0"),
        Color.FromHex("#034E7B"),
    ]);

    /// <summary>Pale yellow through light green to dark green (after matplotlib's <c>YlGn</c>).</summary>
    public static IColorMap YlGn { get; } = new LinearColorMap("ylgn",
    [
        Color.FromHex("#FFFFE5"),
        Color.FromHex("#F7FCB1"),
        Color.FromHex("#D9F0A3"),
        Color.FromHex("#ADDD8E"),
        Color.FromHex("#78C679"),
        Color.FromHex("#41AB5D"),
        Color.FromHex("#238443"),
        Color.FromHex("#005A32"),
    ]);

    /// <summary>Near-white through pale blue-green to dark green (after matplotlib's <c>BuGn</c>).</summary>
    public static IColorMap BuGn { get; } = new LinearColorMap("bugn",
    [
        Color.FromHex("#F7FCFD"),
        Color.FromHex("#E5F5F9"),
        Color.FromHex("#CCECE6"),
        Color.FromHex("#99D8C9"),
        Color.FromHex("#66C2A4"),
        Color.FromHex("#41AE76"),
        Color.FromHex("#238B45"),
        Color.FromHex("#005824"),
    ]);

    /// <summary>Black through red and orange to yellow and white, like a heated metal (after matplotlib's <c>hot</c>).</summary>
    public static IColorMap Hot { get; } = new LinearColorMap("hot",
    [
        Color.FromHex("#0B0000"),
        Color.FromHex("#6F0000"),
        Color.FromHex("#DF0000"),
        Color.FromHex("#FF4F00"),
        Color.FromHex("#FFBF00"),
        Color.FromHex("#FFFF3F"),
        Color.FromHex("#FFFFFF"),
    ]);

    /// <summary>Black to a light copper tone (after matplotlib's <c>copper</c>).</summary>
    public static IColorMap Copper { get; } = new LinearColorMap("copper",
    [
        Color.FromHex("#000000"),
        Color.FromHex("#3F2819"),
        Color.FromHex("#7F5033"),
        Color.FromHex("#BF784D"),
        Color.FromHex("#FFA066"),
        Color.FromHex("#FFC882"),
    ]);

    /// <summary>Black through blue-tinted greys to near-white, the look of an X-ray plate (after matplotlib's <c>bone</c>).</summary>
    public static IColorMap Bone { get; } = new LinearColorMap("bone",
    [
        Color.FromHex("#000000"),
        Color.FromHex("#1A1A2E"),
        Color.FromHex("#333355"),
        Color.FromHex("#4D5D73"),
        Color.FromHex("#738791"),
        Color.FromHex("#99B1AE"),
        Color.FromHex("#C3D5CB"),
        Color.FromHex("#E8E8E8"),
    ]);

    /// <summary>Near-white through pale blue to dark purple (after matplotlib's <c>BuPu</c>).</summary>
    public static IColorMap BuPu { get; } = new LinearColorMap("bupu",
    [
        Color.FromHex("#F7FCFD"),
        Color.FromHex("#E0ECF4"),
        Color.FromHex("#BFD3E6"),
        Color.FromHex("#9EBCDA"),
        Color.FromHex("#8C96C6"),
        Color.FromHex("#8C6BB1"),
        Color.FromHex("#88419D"),
        Color.FromHex("#6E016B"),
    ]);

    /// <summary>Near-white through light green and teal to dark blue (after matplotlib's <c>GnBu</c>).</summary>
    public static IColorMap GnBu { get; } = new LinearColorMap("gnbu",
    [
        Color.FromHex("#F7FCF0"),
        Color.FromHex("#E0F3DB"),
        Color.FromHex("#CCEBC5"),
        Color.FromHex("#A8DDB5"),
        Color.FromHex("#7BCCC4"),
        Color.FromHex("#4EB3D3"),
        Color.FromHex("#2B8CBE"),
        Color.FromHex("#084081"),
    ]);

    /// <summary>Near-white through lilac and pink to dark red-violet (after matplotlib's <c>PuRd</c>).</summary>
    public static IColorMap PuRd { get; } = new LinearColorMap("purd",
    [
        Color.FromHex("#F7F4F9"),
        Color.FromHex("#E7E1EF"),
        Color.FromHex("#D4B9DA"),
        Color.FromHex("#C994C7"),
        Color.FromHex("#DF65B0"),
        Color.FromHex("#E7298A"),
        Color.FromHex("#CE1256"),
        Color.FromHex("#91003F"),
    ]);

    /// <summary>Near-white through pink to dark purple (after matplotlib's <c>RdPu</c>).</summary>
    public static IColorMap RdPu { get; } = new LinearColorMap("rdpu",
    [
        Color.FromHex("#FFF7F3"),
        Color.FromHex("#FDE0DD"),
        Color.FromHex("#FCC5C0"),
        Color.FromHex("#FA9FB5"),
        Color.FromHex("#F768A1"),
        Color.FromHex("#DD3497"),
        Color.FromHex("#AE017E"),
        Color.FromHex("#7A0177"),
    ]);

    /// <summary>Pale yellow through green and teal to dark blue (after matplotlib's <c>YlGnBu</c>).</summary>
    public static IColorMap YlGnBu { get; } = new LinearColorMap("ylgnbu",
    [
        Color.FromHex("#FFFFD9"),
        Color.FromHex("#EDF8B1"),
        Color.FromHex("#C7E9B4"),
        Color.FromHex("#7FCDBB"),
        Color.FromHex("#41B6C4"),
        Color.FromHex("#1D91C0"),
        Color.FromHex("#225EA8"),
        Color.FromHex("#0C2C84"),
    ]);

    /// <summary>Black to white along a helix of hues, with brightness rising steadily so it also reads in grayscale (after matplotlib's <c>cubehelix</c>).</summary>
    public static IColorMap Cubehelix { get; } = new LinearColorMap("cubehelix",
    [
        Color.FromHex("#000000"),
        Color.FromHex("#1A1530"),
        Color.FromHex("#163D4E"),
        Color.FromHex("#1F6642"),
        Color.FromHex("#568228"),
        Color.FromHex("#A07949"),
        Color.FromHex("#C07B8A"),
        Color.FromHex("#BD90C4"),
        Color.FromHex("#C8B8E0"),
        Color.FromHex("#E0E0E0"),
        Color.FromHex("#FFFFFF"),
    ]);

    /// <summary>Near-white through light purple-blue and blue to dark green (after matplotlib's <c>PuBuGn</c>).</summary>
    public static IColorMap PuBuGn { get; } = new LinearColorMap("pubugn",
    [
        Color.FromHex("#FFF7FB"),
        Color.FromHex("#ECE2F0"),
        Color.FromHex("#D0D1E6"),
        Color.FromHex("#A6BDDB"),
        Color.FromHex("#67A9CF"),
        Color.FromHex("#3690C0"),
        Color.FromHex("#02818A"),
        Color.FromHex("#016450"),
    ]);
}
