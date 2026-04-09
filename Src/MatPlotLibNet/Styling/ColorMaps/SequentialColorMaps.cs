// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides sequential color maps that progress from low to high intensity through a single hue.
/// </summary>
public static class SequentialColorMaps
{
    /// <summary>Gets the Cividis color map (colorblind-friendly, dark blue to yellow).</summary>
    public static IColorMap Cividis { get; } = new LerpColorMap("cividis",
    [
        Color.FromHex("#00204D"),
        Color.FromHex("#414D6B"),
        Color.FromHex("#7B7B78"),
        Color.FromHex("#A69D75"),
        Color.FromHex("#CFD369"),
        Color.FromHex("#FFE945"),
    ]);

    /// <summary>Gets the Greens color map (white to dark green).</summary>
    public static IColorMap Greens { get; } = new LerpColorMap("greens",
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

    /// <summary>Gets the Oranges color map (white to dark orange).</summary>
    public static IColorMap Oranges { get; } = new LerpColorMap("oranges",
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

    /// <summary>Gets the Purples color map (white to dark purple).</summary>
    public static IColorMap Purples { get; } = new LerpColorMap("purples",
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

    /// <summary>Gets the Greys color map (white to black).</summary>
    public static IColorMap Greys { get; } = new LerpColorMap("greys",
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

    /// <summary>Gets the YlOrBr color map (yellow to brown).</summary>
    public static IColorMap YlOrBr { get; } = new LerpColorMap("ylorbr",
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

    /// <summary>Gets the YlOrRd color map (yellow to red).</summary>
    public static IColorMap YlOrRd { get; } = new LerpColorMap("ylorrd",
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

    /// <summary>Gets the OrRd color map (orange to red).</summary>
    public static IColorMap OrRd { get; } = new LerpColorMap("orrd",
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

    /// <summary>Gets the PuBu color map (purple to blue).</summary>
    public static IColorMap PuBu { get; } = new LerpColorMap("pubu",
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

    /// <summary>Gets the YlGn color map (yellow to green).</summary>
    public static IColorMap YlGn { get; } = new LerpColorMap("ylgn",
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

    /// <summary>Gets the BuGn color map (blue to green).</summary>
    public static IColorMap BuGn { get; } = new LerpColorMap("bugn",
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

    /// <summary>Gets the Hot color map (black to red to yellow to white).</summary>
    public static IColorMap Hot { get; } = new LerpColorMap("hot",
    [
        Color.FromHex("#0B0000"),
        Color.FromHex("#6F0000"),
        Color.FromHex("#DF0000"),
        Color.FromHex("#FF4F00"),
        Color.FromHex("#FFBF00"),
        Color.FromHex("#FFFF3F"),
        Color.FromHex("#FFFFFF"),
    ]);

    /// <summary>Gets the Copper color map (black to warm copper brown).</summary>
    public static IColorMap Copper { get; } = new LerpColorMap("copper",
    [
        Color.FromHex("#000000"),
        Color.FromHex("#3F2819"),
        Color.FromHex("#7F5033"),
        Color.FromHex("#BF784D"),
        Color.FromHex("#FFA066"),
        Color.FromHex("#FFC882"),
    ]);

    /// <summary>Gets the Bone color map (blue-grey ramp from black to near-white).</summary>
    public static IColorMap Bone { get; } = new LerpColorMap("bone",
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

    /// <summary>Gets the BuPu color map (light blue to dark purple).</summary>
    public static IColorMap BuPu { get; } = new LerpColorMap("bupu",
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

    /// <summary>Gets the GnBu color map (light green to dark blue).</summary>
    public static IColorMap GnBu { get; } = new LerpColorMap("gnbu",
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

    /// <summary>Gets the PuRd color map (light purple to dark red).</summary>
    public static IColorMap PuRd { get; } = new LerpColorMap("purd",
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

    /// <summary>Gets the RdPu color map (light red to dark purple).</summary>
    public static IColorMap RdPu { get; } = new LerpColorMap("rdpu",
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

    /// <summary>Gets the YlGnBu color map (yellow to green to dark blue).</summary>
    public static IColorMap YlGnBu { get; } = new LerpColorMap("ylgnbu",
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

    /// <summary>Gets the Cubehelix color map (monotonically increasing brightness with helical color progression).</summary>
    public static IColorMap Cubehelix { get; } = new LerpColorMap("cubehelix",
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

    /// <summary>Gets the PuBuGn color map (light purple to blue to dark green).</summary>
    public static IColorMap PuBuGn { get; } = new LerpColorMap("pubugn",
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
