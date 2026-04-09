// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>
/// Provides qualitative color maps with distinct, unrelated colors
/// suitable for categorical data where no ordering is implied.
/// </summary>
public static class QualitativeColorMaps
{
    /// <summary>Gets the Tab10 color map (10 distinct categorical colors).</summary>
    public static IColorMap Tab10 { get; } = new LerpColorMap("tab10",
    [
        Color.FromHex("#1F77B4"),
        Color.FromHex("#FF7F0E"),
        Color.FromHex("#2CA02C"),
        Color.FromHex("#D62728"),
        Color.FromHex("#9467BD"),
        Color.FromHex("#8C564B"),
        Color.FromHex("#E377C2"),
        Color.FromHex("#7F7F7F"),
        Color.FromHex("#BCBD22"),
        Color.FromHex("#17BECF"),
    ]);

    /// <summary>Gets the Tab20 color map (20 colors in 10 light/dark pairs).</summary>
    public static IColorMap Tab20 { get; } = new LerpColorMap("tab20",
    [
        Color.FromHex("#1F77B4"),
        Color.FromHex("#AEC7E8"),
        Color.FromHex("#FF7F0E"),
        Color.FromHex("#FFBB78"),
        Color.FromHex("#2CA02C"),
        Color.FromHex("#98DF8A"),
        Color.FromHex("#D62728"),
        Color.FromHex("#FF9896"),
        Color.FromHex("#9467BD"),
        Color.FromHex("#C5B0D5"),
        Color.FromHex("#8C564B"),
        Color.FromHex("#C49C94"),
        Color.FromHex("#E377C2"),
        Color.FromHex("#F7B6D2"),
        Color.FromHex("#7F7F7F"),
        Color.FromHex("#C7C7C7"),
        Color.FromHex("#BCBD22"),
        Color.FromHex("#DBDB8D"),
        Color.FromHex("#17BECF"),
        Color.FromHex("#9EDAE5"),
    ]);

    /// <summary>Gets the Set1 color map (9 bold categorical colors).</summary>
    public static IColorMap Set1 { get; } = new LerpColorMap("set1",
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
    ]);

    /// <summary>Gets the Set2 color map (8 pastel categorical colors).</summary>
    public static IColorMap Set2 { get; } = new LerpColorMap("set2",
    [
        Color.FromHex("#66C2A5"),
        Color.FromHex("#FC8D62"),
        Color.FromHex("#8DA0CB"),
        Color.FromHex("#E78AC3"),
        Color.FromHex("#A6D854"),
        Color.FromHex("#FFD92F"),
        Color.FromHex("#E5C494"),
        Color.FromHex("#B3B3B3"),
    ]);

    /// <summary>Gets the Set3 color map (12 light categorical colors).</summary>
    public static IColorMap Set3 { get; } = new LerpColorMap("set3",
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
    ]);

    /// <summary>Gets the Pastel1 color map (9 very light categorical colors).</summary>
    public static IColorMap Pastel1 { get; } = new LerpColorMap("pastel1",
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
    ]);

    /// <summary>Gets the Pastel2 color map (8 light pastel categorical colors).</summary>
    public static IColorMap Pastel2 { get; } = new LerpColorMap("pastel2",
    [
        Color.FromHex("#B3E2CD"),
        Color.FromHex("#FDCDAC"),
        Color.FromHex("#CBD5E8"),
        Color.FromHex("#F4CAE4"),
        Color.FromHex("#E6F5C9"),
        Color.FromHex("#FFF2AE"),
        Color.FromHex("#F1E2CC"),
        Color.FromHex("#CCCCCC"),
    ]);

    /// <summary>Gets the Dark2 color map (8 bold, dark categorical colors).</summary>
    public static IColorMap Dark2 { get; } = new LerpColorMap("dark2",
    [
        Color.FromHex("#1B9E77"),
        Color.FromHex("#D95F02"),
        Color.FromHex("#7570B3"),
        Color.FromHex("#E7298A"),
        Color.FromHex("#66A61E"),
        Color.FromHex("#E6AB02"),
        Color.FromHex("#A6761D"),
        Color.FromHex("#666666"),
    ]);

    /// <summary>Gets the Accent color map (8 accented categorical colors).</summary>
    public static IColorMap Accent { get; } = new LerpColorMap("accent",
    [
        Color.FromHex("#7FC97F"),
        Color.FromHex("#BEAED4"),
        Color.FromHex("#FDC086"),
        Color.FromHex("#FFFF99"),
        Color.FromHex("#386CB0"),
        Color.FromHex("#F0027F"),
        Color.FromHex("#BF5B17"),
        Color.FromHex("#666666"),
    ]);

    /// <summary>Gets the Paired color map (12 paired light/dark categorical colors).</summary>
    public static IColorMap Paired { get; } = new LerpColorMap("paired",
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
    ]);
}
