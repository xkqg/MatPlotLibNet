// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides qualitative color maps with distinct, unrelated colors
/// suitable for categorical data where no ordering is implied.
/// </summary>
public static class QualitativeColorMaps
{
    internal static ColorMapShelf Shelf { get; } = new();

    /// <summary>The ten colours of matplotlib's default colour cycle: the one place they are written. A theme's colour cycle and
    /// the hue palette of a grouped chart read them through <see cref="Tab10Cycle"/>.</summary>
    private static readonly Color[] Tab10Colors =
    [
        Color.FromHex("#1F77B4"), // blue
        Color.FromHex("#FF7F0E"), // orange
        Color.FromHex("#2CA02C"), // green
        Color.FromHex("#D62728"), // red
        Color.FromHex("#9467BD"), // purple
        Color.FromHex("#8C564B"), // brown
        Color.FromHex("#E377C2"), // pink
        Color.FromHex("#7F7F7F"), // gray
        Color.FromHex("#BCBD22"), // olive
        Color.FromHex("#17BECF"), // cyan
    ];

    /// <summary>The ten colours of matplotlib's default colour cycle, in cycle order, as an array of the caller's own: a theme
    /// that changes its cycle in place does not repaint the other themes, the hue palette or <see cref="Tab10"/>.</summary>
    /// <returns>A new array of the colours of <see cref="Tab10"/>.</returns>
    internal static Color[] Tab10Cycle() => [.. Tab10Colors];

    /// <summary>One colour of matplotlib's default colour cycle, without the array <see cref="Tab10Cycle"/> hands out: the named
    /// colours <c>Colors.Tab10Blue</c> and its nine siblings read their colour here.</summary>
    /// <param name="index">The place in the cycle, from 0 (blue) to 9 (cyan).</param>
    /// <returns>The colour at that place.</returns>
    internal static Color Tab10At(int index) => Tab10Colors[index];

    /// <summary>The eight Okabe-Ito colours, black last: the one place they are written. The <see cref="OkabeIto"/> map and the
    /// colour-blind-safe theme's cycle read them through <see cref="OkabeItoCycle"/>.</summary>
    private static readonly Color[] OkabeItoColors =
    [
        Color.FromHex("#E69F00"), // orange
        Color.FromHex("#56B4E9"), // sky blue
        Color.FromHex("#009E73"), // bluish green
        Color.FromHex("#F0E442"), // yellow
        Color.FromHex("#0072B2"), // blue
        Color.FromHex("#D55E00"), // vermillion
        Color.FromHex("#CC79A7"), // reddish purple
        Color.FromHex("#000000"), // black
    ];

    /// <summary>The eight Okabe-Ito colours in the order <see cref="OkabeIto"/> holds them, as an array of the caller's own.</summary>
    /// <returns>A new array of the colours of <see cref="OkabeIto"/>.</returns>
    internal static Color[] OkabeItoCycle() => [.. OkabeItoColors];

    /// <summary>One colour of the Okabe-Ito palette, without the array <see cref="OkabeItoCycle"/> hands out: the alarm palette's amber and
    /// vermillion read it.</summary>
    /// <param name="index">The place in the palette, from 0 (orange) to 7 (black).</param>
    /// <returns>The colour at that place.</returns>
    internal static Color OkabeItoAt(int index) => OkabeItoColors[index];

    /// <summary>Ten distinct colours: matplotlib's default colour cycle (blue, orange, green, red, purple, brown, pink, grey, olive, cyan).</summary>
    public static IColorMap Tab10 { get; } = Shelf.Add(new ListedColorMap("tab10", Tab10Cycle()));

    /// <summary>Twenty colours: the ten <see cref="Tab10"/> hues, each followed by a lighter tint of itself.</summary>
    public static IColorMap Tab20 { get; } = Shelf.Add(new ListedColorMap("tab20", Tab20Colours()));

    /// <summary>The colours of <see cref="Tab20"/>: each hue of the cycle (read from the one list that writes them) and its tint, the
    /// tint being the only colour this map adds.</summary>
    private static Color[] Tab20Colours()
    {
        ReadOnlySpan<string> tints = ["#AEC7E8", "#FFBB78", "#98DF8A", "#FF9896", "#C5B0D5", "#C49C94", "#F7B6D2", "#C7C7C7", "#DBDB8D", "#9EDAE5"];
        var colours = new Color[2 * Tab10Colors.Length];
        for (int hue = 0; hue < Tab10Colors.Length; hue++)
        {
            colours[2 * hue] = Tab10Colors[hue];
            colours[2 * hue + 1] = Color.FromHex(tints[hue]);
        }

        return colours;
    }

    /// <summary>Nine strong, saturated colours (after matplotlib's <c>Set1</c>).</summary>
    public static IColorMap Set1 { get; } = Shelf.Add(new ListedColorMap("set1",
    [
        Color.FromHex("#E41A1C"),
        Color.FromHex("#377EB8"),
        Color.FromHex("#4DAF4A"),
        Color.FromHex("#984EA3"),
        Color.FromHex("#FF7F00"),
        Color.FromHex("#FFFF33"),
        Color.FromHex("#A65628"),
        Color.FromHex("#F781BF"),
        Color.FromHex("#999999"),
    ]));

    /// <summary>Eight softer, muted colours (after matplotlib's <c>Set2</c>).</summary>
    public static IColorMap Set2 { get; } = Shelf.Add(new ListedColorMap("set2",
    [
        Color.FromHex("#66C2A5"),
        Color.FromHex("#FC8D62"),
        Color.FromHex("#8DA0CB"),
        Color.FromHex("#E78AC3"),
        Color.FromHex("#A6D854"),
        Color.FromHex("#FFD92F"),
        Color.FromHex("#E5C494"),
        Color.FromHex("#B3B3B3"),
    ]));

    /// <summary>Twelve light, low-saturation colours (after matplotlib's <c>Set3</c>).</summary>
    public static IColorMap Set3 { get; } = Shelf.Add(new ListedColorMap("set3",
    [
        Color.FromHex("#8DD3C7"),
        Color.FromHex("#FFFFB3"),
        Color.FromHex("#BEBADA"),
        Color.FromHex("#FB8072"),
        Color.FromHex("#80B1D3"),
        Color.FromHex("#FDB462"),
        Color.FromHex("#B3DE69"),
        Color.FromHex("#FCCDE5"),
        Color.FromHex("#D9D9D9"),
        Color.FromHex("#BC80BD"),
        Color.FromHex("#CCEBC5"),
        Color.FromHex("#FFED6F"),
    ]));

    /// <summary>Nine very light pastel colours (after matplotlib's <c>Pastel1</c>).</summary>
    public static IColorMap Pastel1 { get; } = Shelf.Add(new ListedColorMap("pastel1",
    [
        Color.FromHex("#FBB4AE"),
        Color.FromHex("#B3CDE3"),
        Color.FromHex("#CCEBC5"),
        Color.FromHex("#DECBE4"),
        Color.FromHex("#FED9A6"),
        Color.FromHex("#FFFFCC"),
        Color.FromHex("#E5D8BD"),
        Color.FromHex("#FDDAEC"),
        Color.FromHex("#F2F2F2"),
    ]));

    /// <summary>Eight light pastel colours, a little more muted than <see cref="Pastel1"/> (after matplotlib's <c>Pastel2</c>).</summary>
    public static IColorMap Pastel2 { get; } = Shelf.Add(new ListedColorMap("pastel2",
    [
        Color.FromHex("#B3E2CD"),
        Color.FromHex("#FDCDAC"),
        Color.FromHex("#CBD5E8"),
        Color.FromHex("#F4CAE4"),
        Color.FromHex("#E6F5C9"),
        Color.FromHex("#FFF2AE"),
        Color.FromHex("#F1E2CC"),
        Color.FromHex("#CCCCCC"),
    ]));

    /// <summary>Eight dark, saturated colours that hold up on a light background (after matplotlib's <c>Dark2</c>).</summary>
    public static IColorMap Dark2 { get; } = Shelf.Add(new ListedColorMap("dark2",
    [
        Color.FromHex("#1B9E77"),
        Color.FromHex("#D95F02"),
        Color.FromHex("#7570B3"),
        Color.FromHex("#E7298A"),
        Color.FromHex("#66A61E"),
        Color.FromHex("#E6AB02"),
        Color.FromHex("#A6761D"),
        Color.FromHex("#666666"),
    ]));

    /// <summary>Eight colours that open with four soft tones and end with four vivid ones (after matplotlib's <c>Accent</c>).</summary>
    public static IColorMap Accent { get; } = Shelf.Add(new ListedColorMap("accent",
    [
        Color.FromHex("#7FC97F"),
        Color.FromHex("#BEAED4"),
        Color.FromHex("#FDC086"),
        Color.FromHex("#FFFF99"),
        Color.FromHex("#386CB0"),
        Color.FromHex("#F0027F"),
        Color.FromHex("#BF5B17"),
        Color.FromHex("#666666"),
    ]));

    /// <summary>Twelve colours in light and dark pairs of the same hue, so neighbouring entries belong together (after matplotlib's <c>Paired</c>).</summary>
    public static IColorMap Paired { get; } = Shelf.Add(new ListedColorMap("paired",
    [
        Color.FromHex("#A6CEE3"),
        Color.FromHex("#1F78B4"),
        Color.FromHex("#B2DF8A"),
        Color.FromHex("#33A02C"),
        Color.FromHex("#FB9A99"),
        Color.FromHex("#E31A1C"),
        Color.FromHex("#FDBF6F"),
        Color.FromHex("#FF7F00"),
        Color.FromHex("#CAB2D6"),
        Color.FromHex("#6A3D9A"),
        Color.FromHex("#FFFF99"),
        Color.FromHex("#B15928"),
    ]));

    /// <summary>
    /// Okabe-Ito 8-color palette — designed to be distinguishable under all three common forms
    /// of color vision deficiency (deuteranopia, protanopia, tritanopia). matplotlib's <c>okabe_ito</c> lists the same eight colours
    /// with black first; here black stands last, so the map opens with orange, the order the color-blind-safe theme cycles through.
    /// </summary>
    public static IColorMap OkabeIto { get; } = Shelf.Add(new ListedColorMap("okabe_ito", OkabeItoCycle()));

    /// <summary>
    /// Petroff 6-color sequence — the shortest of the three, and the one with the most contrast between
    /// neighbours. From Petroff, "Accessible Color Sequences for Data Visualization" (2021): every pair inside a
    /// sequence stays distinguishable under the common forms of color vision deficiency, and the sequences are
    /// also readable in grayscale. Same values matplotlib ships as its `petroff6` style.
    /// </summary>
    public static IColorMap Petroff6 { get; } = Shelf.Add(new ListedColorMap("petroff6",
    [
        Color.FromHex("#5790FC"), // blue
        Color.FromHex("#F89C20"), // orange
        Color.FromHex("#E42536"), // red
        Color.FromHex("#964A8B"), // purple
        Color.FromHex("#9C9CA1"), // grey
        Color.FromHex("#7A21DD"), // violet
    ]));

    /// <summary>
    /// Petroff 8-color sequence — for eight series that must all stay apart. See <see cref="Petroff6"/> for the
    /// source; same values as matplotlib's `petroff8` style.
    /// </summary>
    public static IColorMap Petroff8 { get; } = Shelf.Add(new ListedColorMap("petroff8",
    [
        Color.FromHex("#1845FB"), // blue
        Color.FromHex("#FF5E02"), // orange
        Color.FromHex("#C91F16"), // red
        Color.FromHex("#C849A9"), // pink
        Color.FromHex("#ADAD7D"), // khaki
        Color.FromHex("#86C8DD"), // light blue
        Color.FromHex("#578DFF"), // mid blue
        Color.FromHex("#656364"), // grey
    ]));

    /// <summary>
    /// Petroff 10-color sequence — the longest of the three, and the one to reach for when a chart has ten
    /// categories and they all matter. See <see cref="Petroff6"/> for the source; same values as matplotlib's
    /// `petroff10` style.
    /// </summary>
    public static IColorMap Petroff10 { get; } = Shelf.Add(new ListedColorMap("petroff10",
    [
        Color.FromHex("#3F90DA"), // blue
        Color.FromHex("#FFA90E"), // amber
        Color.FromHex("#BD1F01"), // brick
        Color.FromHex("#94A4A2"), // grey green
        Color.FromHex("#832DB6"), // purple
        Color.FromHex("#A96B59"), // brown
        Color.FromHex("#E76300"), // orange
        Color.FromHex("#B9AC70"), // sand
        Color.FromHex("#717581"), // slate
        Color.FromHex("#92DADD"), // pale cyan
    ]));
}
