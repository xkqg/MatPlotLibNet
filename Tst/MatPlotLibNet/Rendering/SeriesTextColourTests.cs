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
/// Every text a series draws carries a colour of its own in the picture. A text written with no fill is drawn black by the
/// browser whatever the theme, which is unreadable on a dark one: a gauge's value, a progress bar's percentage, a donut's
/// centre, a key's label, a table's cells and a state's name came out that way. The theme's text colour is the colour of a
/// text that stands on the plot's own ground (<see cref="Rendering.ThemeFontExtensions.DataTextFont(Theme, double, Color?, FontWeight)"/>
/// gives it when the series names none); a text that stands on a ground the series paints itself (a table cell, a progress
/// bar, a state's segment) keeps the theme's colour when it can be read against that ground, and is black or white when it cannot.
/// </summary>
public class SeriesTextColourTests
{
    private static readonly Theme Dark = Theme.Dark;
    private static readonly string Light = Dark.ForegroundText.ToHex();
    private static readonly string Black = Colors.Black.ToHex();
    private static readonly string White = Colors.White.ToHex();
    private static string On(Theme theme, Action<AxesBuilder> draw) =>
        Plt.Create().WithSize(600, 450).WithTheme(theme).AddSubPlot(1, 1, 1, draw).ToSvg();

    // ── a text on the plot's own ground: the theme's text colour ────────────────────────────────────────────

    [Fact]
    public void AGaugeValue_OnADarkTheme_IsInTheThemesTextColour() =>
        Assert.Equal(Light, On(Dark, ax => ax.Gauge(0.37)).FillOf("0.37"));

    [Fact]
    public void ADonutCentre_OnADarkTheme_IsInTheThemesTextColour() =>
        Assert.Equal(Light, On(Dark, ax => ax.Donut([30.0, 70.0], null, s => s.CenterText = "ctr")).FillOf("ctr"));

    [Fact]
    public void AKeyLabel_OnADarkTheme_IsInTheThemesTextColour() =>
        Assert.Equal(Light, On(Dark, ax => ax.QuiverKey(0.8, 0.9, 1.0, "1 m/s")).FillOf("1 m/s"));

    [Fact]
    public void AGaugeValue_OnTheDefaultTheme_IsWrittenBlack_AsMatplotlibWritesItsTextColour() =>
        Assert.Equal(Black, On(Theme.Default, ax => ax.Gauge(0.37)).FillOf("0.37"));

    [Fact]
    public void ADonutCentre_OnTheDefaultTheme_IsWrittenBlack() =>
        Assert.Equal(Black, On(Theme.Default, ax => ax.Donut([30.0, 70.0], null, s => s.CenterText = "ctr")).FillOf("ctr"));

    [Fact]
    public void AKeyLabel_OnTheDefaultTheme_IsWrittenBlack() =>
        Assert.Equal(Black, On(Theme.Default, ax => ax.QuiverKey(0.8, 0.9, 1.0, "1 m/s")).FillOf("1 m/s"));

    // ── a text on a ground the series paints: readable against that ground ──────────────────────────────────

    [Fact]
    public void AProgressPercentage_OnTheLightTrack_OfADarkTheme_IsBlack() =>
        Assert.Equal(Black, On(Dark, ax => ax.ProgressBar(0.37)).FillOf("37%"));

    [Fact]
    public void AProgressPercentage_OnADarkTrack_OfADarkTheme_IsTheThemesTextColour() =>
        Assert.Equal(Light, On(Dark, ax => ax.ProgressBar(0.37, s => s.TrackColor = new Color(30, 30, 30))).FillOf("37%"));

    [Fact]
    public void AProgressPercentage_OnTheFill_ReadsAgainstTheFill_NotTheTrack() =>
        Assert.Equal(Light, On(Dark, ax => ax.ProgressBar(0.8)).FillOf("80%"));

    [Fact]
    public void AProgressPercentage_OnTheDefaultTheme_IsWrittenBlack() =>
        Assert.Equal(Black, On(Theme.Default, ax => ax.ProgressBar(0.37)).FillOf("37%"));

    [Theory]
    [InlineData("Alice")] // the white cell
    [InlineData("Name")]  // the grey header
    public void ATableCellOrHeader_OnItsLightFill_OfADarkTheme_IsBlack(string text) =>
        Assert.Equal(Black, On(Dark, ax => ax.Table([["Alice", "90"]], s => s.ColumnHeaders = ["Name", "Score"])).FillOf(text));

    [Fact]
    public void ATableCell_OnADarkCell_OfADarkTheme_IsTheThemesTextColour() =>
        Assert.Equal(Light, On(Dark, ax => ax.Table([["Alice", "90"]], s => s.CellColor = new Color(30, 30, 30))).FillOf("Alice"));

    [Fact]
    public void ATableRowHeader_ReadsAgainstTheHeaderGround() =>
        Assert.Equal(Light, On(Dark, ax => ax.Table([["Alice", "90"]], s =>
        {
            s.RowHeaders = ["row-a"];
            s.HeaderColor = new Color(30, 30, 30);
        })).FillOf("row-a"));

    [Fact]
    public void ATableCell_OnTheDefaultTheme_IsWrittenBlack() =>
        Assert.Equal(Black, On(Theme.Default, ax => ax.Table([["Alice", "90"]])).FillOf("Alice"));

    [Fact]
    public void AStateName_OnALightSegment_OfADarkTheme_IsBlack() =>
        Assert.Equal(Black, On(Dark, ax => ax.StateTimeline([new StateSegment(0, 10, "Idle", Colors.Yellow)])).FillOf("Idle"));

    [Fact]
    public void AStateName_OnADarkSegment_OfADarkTheme_IsTheThemesTextColour() =>
        Assert.Equal(Light, On(Dark, ax => ax.StateTimeline([new StateSegment(0, 10, "Busy", Colors.Blue)])).FillOf("Busy"));

    [Fact]
    public void AStateName_OnADarkSegment_OfTheDefaultTheme_IsWhite_BlackWouldNotBeRead() =>
        Assert.Equal(White, On(Theme.Default, ax => ax.StateTimeline([new StateSegment(0, 10, "Busy", Colors.Blue)])).FillOf("Busy"));

    // ── a text that took its colour from the theme's default font, which most themes leave unset ───────────

    /// <summary>A dark theme whose default font names no colour, as most themes' default fonts do not: the text colour is
    /// the theme's <c>ForegroundText</c>, and only that.</summary>
    private static readonly Theme DarkFontWithoutColour =
        Theme.CreateFrom(Dark).WithFont(f => f with { Color = null }).Build();

    private static readonly TreeNode SmallTree =
        new() { Label = "root", Children = [new() { Label = "leafa", Value = 1.0 }, new() { Label = "leafb", Value = 2.0 }] };

    private static readonly GraphNode[] Nodes = [new("alpha", 0, 0), new("beta", 1, 1)];

    private readonly record struct Chart(Action<AxesBuilder> Draw, string Text);

    private static readonly Dictionary<string, Chart> Charts = new()
    {
        ["PieOuterLabel"] = new(ax => ax.Pie([1.0, 3.0], ["slice-a", "slice-b"]), "slice-a"),
        ["SankeyOuterLabel"] = new(ax => ax.Sankey([new SankeyNode("src-node"), new SankeyNode("dst-node")], [new SankeyLink(0, 1, 5)]), "src-node"),
        ["DendrogramLeaf"] = new(ax => ax.Dendrogram(SmallTree), "leafa"),
        ["NetworkGraphEdgeWeight"] = new(
            ax => ax.NetworkGraph(Nodes, [new GraphEdge("alpha", "beta", 2.5, false)], s => { s.Layout = GraphLayout.Manual; s.ShowEdgeWeights = true; }),
            "2.5"),
        ["NetworkGraphSelfLoopWeight"] = new(
            ax => ax.NetworkGraph(Nodes, [new GraphEdge("alpha", "alpha", 7.5, false)], s => { s.Layout = GraphLayout.Manual; s.ShowEdgeWeights = true; }),
            "7.5"),
        ["RelativeRotationLabel"] = new(
            ax => ax.RelativeRotation(
                [Enumerable.Range(0, 60).Select(i => 100.0 + i).ToArray(), Enumerable.Repeat(95.0, 60).ToArray()],
                Enumerable.Repeat(100.0, 60).ToArray(), ["ETH", "BNB"]),
            "ETH"),
        ["Gauge"] = new(ax => ax.Gauge(0.37), "0.37"),
        ["Donut"] = new(ax => ax.Donut([30.0, 70.0], null, s => s.CenterText = "ctr"), "ctr"),
        ["QuiverKey"] = new(ax => ax.QuiverKey(0.8, 0.9, 1.0, "1 m/s"), "1 m/s"),
    };

    public static TheoryData<string> ChartNames => [.. Charts.Keys];

    [Theory]
    [MemberData(nameof(ChartNames))]
    public void ASeriesText_OnTheAxesGround_IsInTheThemesTextColour_WhateverTheDefaultFontSays(string name)
    {
        var chart = Charts[name];

        Assert.Equal(Light, On(DarkFontWithoutColour, chart.Draw).FillOf(chart.Text));
    }

    [Theory]
    [MemberData(nameof(ChartNames))]
    public void ASeriesText_OnTheAxesGround_IsWrittenBlack_OnTheDefaultTheme(string name)
    {
        var chart = Charts[name];

        Assert.Equal(Black, On(Theme.Default, chart.Draw).FillOf(chart.Text));
    }

    // ── one owner: a text the series gives no colour is in the theme's ─────────────────────────────────────

    [Fact]
    public void ASeriesTextWithNoColour_TakesTheThemesTextColour_FromTheOneOwner()
    {
        var font = Dark.DataTextFont(12);
        Assert.Equal(Dark.ForegroundText, font.Color);
    }

    [Fact]
    public void ASeriesTextWithAColour_KeepsIt() =>
        Assert.Equal(Colors.Red, Dark.DataTextFont(12, Colors.Red).Color);

    [Fact]
    public void ATextReadAgainstItsGround_KeepsTheThemesColour_WhenItReadsThere()
    {
        Assert.Equal(Dark.ForegroundText, Dark.DataTextFontOn(new Color(30, 30, 30), 12, GroundInk.Readable).Color);
        Assert.Equal(Colors.Black, Dark.DataTextFontOn(Colors.White, 12, GroundInk.Readable).Color);
        Assert.Equal(Colors.White, Theme.Default.DataTextFontOn(Colors.Black, 12, GroundInk.Readable).Color);
    }

    [Fact]
    public void ATextReadAgainstItsGround_KeepsSizeAndWeight()
    {
        var font = Dark.DataTextFontOn(Colors.White, 14, GroundInk.Readable, FontWeight.Bold);
        Assert.Equal(14, font.Size);
        Assert.Equal(FontWeight.Bold, font.Weight);
        Assert.Equal(Dark.DefaultFont.Family, font.Family);
    }

    /// <summary>matplotlib writes the text it draws on a patch of its own in <c>text.color</c>, whatever the patch: <c>ax.pie(..., autopct=...)</c>'s
    /// percentages are <c>get_color()</c> 'k' under the classic style (on its pure blue wedge too) and 'black' under the default one, and so is
    /// a table cell's text. Under the matplotlib themes the pie's percentages and the table's cells are inked so
    /// (<see cref="GroundInk.TextColour"/>, named by the theme's wedge and table looks); the library's own themes keep choosing black or white
    /// where the theme's colour would not read (<see cref="GroundInk.Readable"/>).</summary>
    [Theory]
    [InlineData("MatplotlibClassic", 0.0)]
    [InlineData("MatplotlibV2", 0.0)]
    [InlineData("Default", 3.0)]
    public void APiesPercentagesAndATablesCells_AreInkedByTheirThemesLooks(string themeName, double contrast)
    {
        var theme = themeName.AsTheme();

        Assert.Equal(contrast, theme.Looks.Wedges.PercentInk.Contrast);
        Assert.Equal(contrast, theme.Looks.Table.TextInk.Contrast);
    }

    [Fact]
    public void ATextColourInk_KeepsTheThemesColourOnAnyGround_AndAReadableOne_SwitchesWhereItWouldNotRead()
    {
        Assert.Equal(Colors.Black, GroundInk.TextColour.On(Colors.Black, Colors.Blue));
        Assert.Equal(Colors.White, GroundInk.Readable.On(Colors.Black, Colors.Blue));
        Assert.Equal(Colors.White, Theme.Default.DataTextFontOn(Colors.Blue, 12, GroundInk.Readable).Color);
        Assert.Equal(Colors.Black, Theme.MatplotlibClassic.DataTextFontOn(Colors.Blue, 12, GroundInk.TextColour).Color);
    }

    [Fact]
    public void UnderTheMatplotlibClassicTheme_APiePercentageOnItsBlueWedge_IsBlack() =>
        Assert.Equal(Black, On(Theme.MatplotlibClassic, ax => ax.Pie([35.0, 65.0], configure: s => s.AutoPct = "{0:F1}%")).FillOf("35.0%"));

    /// <summary>An icicle, a treemap, a sunburst and the other charts matplotlib has no artist for keep the readable ink under every theme:
    /// matplotlib has no text of theirs to match, and a black name on a dark viridis tile is a name nobody reads.</summary>
    [Fact]
    public void UnderTheDefaultMatplotlibTheme_AnIciclesNameOnADarkCell_StaysReadable() =>
        Assert.Equal(White, On(Theme.MatplotlibV2, ax => ax.Icicle(new TreeNode
        {
            Label = "root",
            Children = [new TreeNode { Label = "dark", Value = 1, Color = new Color(68, 1, 84) }],
        })).FillOf("dark"));

    [Fact]
    public void AGroundWithAlpha_IsReadAsItLooksOverTheAxesBackground()
    {
        // A white ground at 10 % opacity over the dark axes is still dark: the theme's light colour reads there.
        var ground = Colors.White.WithAlpha(25);
        Assert.Equal(Dark.ForegroundText, Dark.DataTextFontOn(ground, 12, GroundInk.Readable).Color);
    }
}
