// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.Themes;

/// <summary>
/// Builds the Matplotlib* themes that mimic Python matplotlib's iconic look. Centralizes the
/// shared font stack and grid defaults so the Classic and V2 themes only differ in the values
/// they actually disagree on (color cycle, font size, foreground text).
/// </summary>
internal static class MatplotlibThemeFactory
{
    // Matplotlib's pre-2.0 "classic" 7-color cycle (the legendary bgrcmyk).
    private static readonly Color[] ClassicCycleColors =
    [
        Color.FromHex("#0000FF"), // blue
        Color.FromHex("#008000"), // green
        Color.FromHex("#FF0000"), // red
        Color.FromHex("#00BFBF"), // cyan
        Color.FromHex("#BF00BF"), // magenta
        Color.FromHex("#BFBF00"), // yellow
        Color.FromHex("#000000"), // black
    ];

    // Matplotlib v2.0+ "tab10" categorical cycle — the modern default since 2017.
    private static readonly Color[] Tab10CycleColors =
    [
        Color.FromHex("#1f77b4"), // blue
        Color.FromHex("#ff7f0e"), // orange
        Color.FromHex("#2ca02c"), // green
        Color.FromHex("#d62728"), // red
        Color.FromHex("#9467bd"), // purple
        Color.FromHex("#8c564b"), // brown
        Color.FromHex("#e377c2"), // pink
        Color.FromHex("#7f7f7f"), // gray
        Color.FromHex("#bcbd22"), // olive
        Color.FromHex("#17becf"), // cyan
    ];

    private static readonly MatplotlibFontStack ClassicFontStack =
        new("DejaVu Sans, Bitstream Vera Sans, sans-serif", BaseSize: 12.0, TickSize: 12.0, TitleSize: 14.0);

    private static readonly MatplotlibFontStack V2FontStack =
        new("DejaVu Sans, sans-serif", BaseSize: 10.0, TickSize: 10.0, TitleSize: 12.0);

    // Matplotlib default subplot params: left=0.125, right=0.9 (absolute, so right margin=0.1),
    // top=0.88 (absolute, so top margin=0.12), bottom=0.11.
    // These match `rcParams['figure.subplot.*']` in matplotlib classic style.
    private static readonly Models.SubPlotSpacing MatplotlibSpacing =
        Models.SubPlotSpacing.FromFractions(left: 0.125, right: 0.10, top: 0.12, bottom: 0.11);

    // Matplotlib classic style sets `figure.facecolor = 0.75`, which is RGB(191,191,191) = #BFBFBF.
    // Axes background (axes.facecolor) remains white in classic style.
    private static readonly Color ClassicFigureBackground = Color.FromHex("#BFBFBF");

    // matplotlib classic: patch.edgecolor=k, violin bodies use 'y' (#BFBF00) at alpha=0.3,
    // violin stats lines use 'r' (#FF0000) — empirically confirmed against matplotlib 3.10.8.
    private static readonly Color ClassicPatchEdge   = Color.FromHex("#000000"); // 'k'
    private static readonly Color ClassicViolinBody  = Color.FromHex("#BFBF00"); // 'y'
    private static readonly Color ClassicViolinStats = Color.FromHex("#FF0000"); // 'r'

    internal static Theme CreateClassic() => Build(
        name: "matplotlib-classic",
        background: ClassicFigureBackground,
        foreground: Colors.Black,
        cycleColors: ClassicCycleColors,
        fontStack: ClassicFontStack,
        patchEdgeColor: ClassicPatchEdge,
        violinBodyColor: ClassicViolinBody,
        violinStatsColor: ClassicViolinStats);

    internal static Theme CreateV2() => Build(
        name: "matplotlib-v2",
        background: Colors.White,
        foreground: Color.FromHex("#262626"),
        cycleColors: Tab10CycleColors,
        fontStack: V2FontStack);

    private static Theme Build(string name, Color background, Color foreground, Color[] cycleColors, MatplotlibFontStack fontStack,
        Color? patchEdgeColor = null, Color? violinBodyColor = null, Color? violinStatsColor = null) =>
        new(
            name: name,
            background: background,
            foregroundText: foreground,
            axesBackground: Colors.White,
            cycleColors: cycleColors,
            defaultFont: BuildFont(fontStack, foreground),
            defaultGrid: BuildHiddenGrid(),
            defaultSpacing: MatplotlibSpacing)
        {
            PatchEdgeColor   = patchEdgeColor,
            ViolinBodyColor  = violinBodyColor,
            ViolinStatsColor = violinStatsColor,
        };

    private static Font BuildFont(MatplotlibFontStack stack, Color color) => new()
    {
        Family = stack.PrimaryFamily,
        Size = stack.BaseSize,
        Weight = FontWeight.Normal,
        Slant = FontSlant.Normal,
        Color = color,
    };

    // Matplotlib historically ships with the grid OFF; users opt in via plt.grid(True).
    // We mirror that opt-in behaviour so themes are visually faithful out of the box.
    private static GridStyle BuildHiddenGrid() => new()
    {
        Visible = false,
        Color = Color.FromHex("#B0B0B0"),
        LineStyle = LineStyle.Solid,
        LineWidth = 0.8,
    };
}
