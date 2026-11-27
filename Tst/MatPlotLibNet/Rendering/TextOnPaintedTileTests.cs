// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Builders;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// A name written on a tile, a cell, a ring segment, a node or a slice that the series paints is read against that tile
/// (<see cref="ThemeFontExtensions.DataTextFontOn"/>), not drawn in a fixed white. White on yellow is a text nobody reads. The
/// theme's own text colour stands when it can be read there; black or white, whichever stands out, when it cannot; and a
/// colour the series gives its text keeps winning.
/// </summary>
public class TextOnPaintedTileTests
{
    private static readonly Theme Dark = Theme.Dark;
    private static readonly string Black = Colors.Black.ToHex();
    private static readonly string White = Colors.White.ToHex();
    private static readonly Color VeryDark = new(30, 30, 30);
    private static string On(Theme theme, Action<AxesBuilder> draw) =>
        Plt.Create().WithSize(600, 450).WithTheme(theme).AddSubPlot(1, 1, 1, draw).ToSvg();

    private static TreeNode OneLeaf(Color? colour, string? headline = null) =>
        new() { Label = "Root", Children = [new() { Label = "leaf", Value = 1, Color = colour, Headline = headline }] };

    // ── treemap ────────────────────────────────────────────────────────────────────────────────────────────

    public static TheoryData<string, Action<AxesBuilder, TreeNode>> TreeCharts => new()
    {
        { "treemap", (ax, tree) => ax.Treemap(tree) },
        { "icicle", (ax, tree) => ax.Icicle(tree) },
        { "sunburst", (ax, tree) => ax.Sunburst(tree) },
    };

    /// <summary>The name a tree chart writes on a yellow tile, cell or segment is black, not white; on a dark one it is white.</summary>
    [Theory]
    [MemberData(nameof(TreeCharts))]
    public void ALeafName_IsBlackOnAYellowTile_AndWhiteOnADarkOne(string chart, Action<AxesBuilder, TreeNode> draw)
    {
        Assert.Equal(Black, On(Theme.Default, ax => draw(ax, OneLeaf(Colors.Yellow))).FillOf("leaf"));
        Assert.Equal(White, On(Theme.Default, ax => draw(ax, OneLeaf(Colors.Navy))).FillOf("leaf"));
        _ = chart;
    }

    [Fact]
    public void ATreemapLeafName_OnADarkTheme_OnAYellowTile_IsBlack() =>
        Assert.Equal(Black, On(Dark, ax => ax.Treemap(OneLeaf(Colors.Yellow))).FillOf("leaf"));

    [Fact]
    public void ATreemapLeafName_OnADarkTheme_OnADarkTile_IsTheThemesTextColour() =>
        Assert.Equal(Dark.ForegroundText.ToHex(), On(Dark, ax => ax.Treemap(OneLeaf(VeryDark))).FillOf("leaf"));

    [Fact]
    public void ATreemapLeafHeadline_OnAYellowTile_IsBlack() =>
        Assert.Equal(Black, On(Theme.Default, ax => ax.Treemap(OneLeaf(Colors.Yellow, "42"))).FillOf("42"));

    [Fact]
    public void ATreemapHeader_OnAYellowParentTile_IsBlack()
    {
        var tree = new TreeNode
        {
            Label = "Root",
            Children = [new() { Label = "group", Color = Colors.Yellow, Children = [new() { Label = "leaf", Value = 1, Color = Colors.Navy }] }],
        };

        var svg = On(Theme.Default, ax => ax.Treemap(tree));

        Assert.Equal(Black, svg.FillOf("group"));
        Assert.Equal(White, svg.FillOf("leaf"));
    }

    [Fact]
    public void ATreemapLeafName_OnTheDefaultColourMap_ReadsAgainstEachTile()
    {
        // The viridis ramp runs from a dark purple to a bright yellow: no tile name can be white on all of them.
        var tree = new TreeNode
        {
            Label = "Root",
            Children = [.. Enumerable.Range(0, 8).Select(i => new TreeNode { Label = "n" + i, Value = 1 })],
        };

        var svg = On(Theme.Default, ax => ax.Treemap(tree));

        var fills = Enumerable.Range(0, 8).Select(i => svg.FillOf("n" + i)).Distinct().ToList();
        Assert.Contains(Black, fills);
        Assert.Contains(White, fills);
    }

    // ── Sankey ─────────────────────────────────────────────────────────────────────────────────────────────

    private static Action<AxesBuilder> SankeyWith(Color nodeColour, string? subLabel = null, Color? subLabelColour = null) => ax => ax.Sankey(
        [new SankeyNode("inner", nodeColour, subLabel, subLabelColour), new SankeyNode("other", nodeColour)],
        [new SankeyLink(0, 1, 5)],
        s => s.InsideLabels = true);

    [Fact]
    public void ASankeyNameInsideAYellowNode_IsBlack() =>
        Assert.Equal(Black, On(Theme.Default, SankeyWith(Colors.Yellow)).FillOf("inner"));

    [Fact]
    public void ASankeyNameInsideADarkNode_IsWhite() =>
        Assert.Equal(White, On(Theme.Default, SankeyWith(Colors.Navy)).FillOf("inner"));

    [Fact]
    public void ASankeySubLabelInsideADarkNode_ReadsAgainstTheNodeToo() =>
        Assert.Equal(White, On(Theme.Default, SankeyWith(Colors.Navy, "sub")).FillOf("sub"));

    [Fact]
    public void ASankeySubLabelColour_KeepsWinning_InsideTheNode() =>
        Assert.Equal(Colors.Red.ToHex(), On(Theme.Default, SankeyWith(Colors.Yellow, "sub", Colors.Red)).FillOf("sub"));

    // ── pie ────────────────────────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void APiePercentage_OnAYellowSlice_IsBlack() =>
        Assert.Equal(Black, On(Theme.Default, ax => ax.Pie([1.0], null, s => { s.AutoPct = "{0:F0}%"; s.Colors = [Colors.Yellow]; })).FillOf("100%"));

    [Fact]
    public void APiePercentage_OnADarkSlice_IsWhite() =>
        Assert.Equal(White, On(Theme.Default, ax => ax.Pie([1.0], null, s => { s.AutoPct = "{0:F0}%"; s.Colors = [Colors.Navy]; })).FillOf("100%"));

    // ── funnel ─────────────────────────────────────────────────────────────────────────────────────────────

    private static string FunnelStageName(Theme theme, Color stage) =>
        On(theme, ax => ax.Funnel(["stage"], [1.0], s => s.Colors = [stage])).FillOf("stage");

    /// <summary>The olive of matplotlib's tab10 cycle with the theme's light text on it reads at a contrast of about 2.2 to 1; the
    /// stage name is read against its bar, so it stands at 3 to 1 or better.</summary>
    [Fact]
    public void AFunnelStageName_OnTheOliveOfTab10_UnderTheDarkTheme_IsReadable()
    {
        var olive = Color.FromHex("#BCBD22");

        var ink = Color.FromHex(FunnelStageName(Dark, olive));

        Assert.True(ink.ContrastRatioWith(olive) >= 3.0, $"contrast {ink.ContrastRatioWith(olive):F2} of {ink.ToHex()} on {olive.ToHex()}");
    }

    [Fact]
    public void AFunnelStageName_OnAYellowBar_IsBlack() =>
        Assert.Equal(Black, FunnelStageName(Theme.Default, Colors.Yellow));

    [Fact]
    public void AFunnelStageName_OnADarkBar_IsWhite() =>
        Assert.Equal(White, FunnelStageName(Theme.Default, Colors.Navy));

    [Fact]
    public void AFunnelStageName_OnADarkTheme_OnADarkBar_IsTheThemesTextColour() =>
        Assert.Equal(Dark.ForegroundText.ToHex(), FunnelStageName(Dark, VeryDark));
}
