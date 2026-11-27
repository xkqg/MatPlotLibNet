// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.TestFixtures;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// The text a chart draws among its data (annotations, bar labels, radar spokes, pie percentages, funnel stages, graph
/// node names) is as large as the tick labels of the same theme: one role, <see cref="ThemeFontExtensions.DataTextFont"/>,
/// so a poster theme grows it and a paper theme shrinks it along with everything else, as matplotlib's one text size does.
/// </summary>
public class DataTextFontTests
{
    private static readonly Dictionary<string, Theme> Themes = new()
    {
        ["Default"] = Theme.Default,
        ["MatplotlibV2"] = Theme.MatplotlibV2,
        ["MatplotlibClassic"] = Theme.MatplotlibClassic,
        ["Poster"] = Theme.Poster,
        ["Paper"] = Theme.Paper,
    };

    public static TheoryData<string> ThemeNames => [.. Themes.Keys];

    private static double SizeOf(string svg, string text) => svg.TextReading(text).Number("font-size");

    private static FigureBuilder On(Theme theme) => Plt.Create().WithSize(600, 450).WithTheme(theme);

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void DataTextFont_IsTheThemesDefaultFont_InTheForegroundColour(string themeName)
    {
        var theme = Themes[themeName];

        var font = theme.DataTextFont();

        Assert.Equal(theme.DefaultFont.Family, font.Family);
        Assert.Equal(theme.DefaultFont.Size, font.Size);
        Assert.Equal(theme.ForegroundText, font.Color);
        Assert.Equal(FontWeight.Normal, font.Weight);
    }

    /// <summary>A text the caller places (an annotation, a figure text) is drawn in the font it was given, or the theme's
    /// text font; its colour is the font's own, else the colour set beside it, else the theme's text colour.</summary>
    [Fact]
    public void APlacedTextWithoutAFont_TakesTheThemesTextFont_InTheColourBesideIt()
    {
        var font = Theme.Default.DataTextFont(null, Colors.Red);

        Assert.Equal(Theme.Default.DataTextFont() with { Color = Colors.Red }, font);
    }

    /// <summary>A series text with a size of its own keeps that size, weight and colour, and takes only the family from the theme; one
    /// with no colour of its own is in the theme's text colour, as a text written with no fill would be black on any theme.</summary>
    [Fact]
    public void ASeriesTextWithASizeOfItsOwn_TakesTheThemesFamily_AndKeepsSizeWeightAndColour()
    {
        var theme = Theme.CreateFrom(Theme.Default).WithFont(f => f with { Family = "serif" }).Build();

        Assert.Equal(new Font { Family = "serif", Size = 9, Weight = FontWeight.Bold, Color = Colors.Red }, theme.DataTextFont(9, Colors.Red, FontWeight.Bold));
        Assert.Equal(new Font { Family = "serif", Size = 11, Color = theme.ForegroundText }, theme.DataTextFont(11));
    }

    [Fact]
    public void APlacedTextWithoutAFontOrAColour_TakesTheThemesTextFont() =>
        Assert.Equal(Theme.Default.DataTextFont(), Theme.Default.DataTextFont(null, null));

    [Fact]
    public void ASuppliedFontWithoutAColour_TakesTheColourBesideIt_ElseTheThemes()
    {
        var supplied = new Font { Family = "serif", Size = 20 };

        Assert.Equal(supplied with { Color = Colors.Red }, Theme.Default.DataTextFont(supplied, Colors.Red));
        Assert.Equal(supplied with { Color = Theme.Default.ForegroundText },
            Theme.Default.DataTextFont(supplied, null));
    }

    [Fact]
    public void AColourOnTheSuppliedFont_Wins()
    {
        var supplied = new Font { Family = "serif", Size = 20, Color = Colors.Blue };

        Assert.Equal(supplied, Theme.Default.DataTextFont(supplied, Colors.Red));
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void AnAnnotation_WithoutAFont_IsDrawnAtTheSizeOfTheThemesText(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).Annotate("peak", 5, 5)).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "peak"), SvgMarkupExtensions.WrittenPrecision);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ABarLabel_IsDrawnAtTheSizeOfTheThemesText(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Bar(["a", "b"], [1234.0, 777.0], s => s.ShowLabels = true)).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "1234"), SvgMarkupExtensions.WrittenPrecision);
        Assert.Equal(theme.DataTextFont().Color!.Value.ToHex(), svg.TextReading("1234")["fill"]);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ARadarSpokeName_IsDrawnAtTheSizeOfTheThemesText(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Radar(["alpha", "beta", "gamma"], [1.0, 2.0, 3.0])).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "alpha"), SvgMarkupExtensions.WrittenPrecision);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void APiePercentage_IsDrawnAtTheSizeOfTheThemesText_InTheInkItsThemesWedgeLookNames(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Pie([1.0, 3.0], null, s => { s.AutoPct = "{0:F0}%"; s.Colors = [Colors.Navy, Colors.Navy]; })).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "25%"), SvgMarkupExtensions.WrittenPrecision);
        Assert.Equal(theme.DataTextFontOn(Colors.Navy, theme.DefaultFont.Size, theme.Looks.Wedges.PercentInk).Color!.Value.ToHex(), svg.TextReading("25%")["fill"]);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void AFunnelStageName_IsDrawnAtTheSizeOfTheThemesText(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Funnel(["Visited", "Bought"], [100.0, 40.0])).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "Visited"), SvgMarkupExtensions.WrittenPrecision);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void AGraphNodeName_IsDrawnInTheThemesFont_NotAFixedOne(string themeName)
    {
        var theme = Themes[themeName];
        var nodes = new[] { new GraphNode("alpha", 0, 0), new GraphNode("beta", 1, 1) };
        var edges = new[] { new GraphEdge("alpha", "beta", 1.0, false) };
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, s => s.Layout = GraphLayout.Manual)).ToSvg();

        var tag = svg.TextReading("alpha");
        Assert.Equal(theme.DefaultFont.Size, tag.Number("font-size"), SvgMarkupExtensions.WrittenPrecision);
        Assert.Equal(theme.DefaultFont.Family, tag["font-family"]);
        Assert.Equal(theme.ForegroundText.ToHex(), tag["fill"]);
    }

    /// <summary>A Sankey node name is capped below the theme's size on purpose (the cap keeps a long name inside its band),
    /// so the theme's size does not grow it past that cap.</summary>
    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ASankeyNodeName_KeepsItsCapBelowTheThemesSize(string themeName)
    {
        var theme = Themes[themeName];
        SankeyNode[] nodes = [new("Source"), new("Target")];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Sankey(nodes, [new SankeyLink(0, 1, 5)])).ToSvg();

        Assert.Equal(Math.Min(10, theme.DefaultFont.Size), SizeOf(svg, "Source"), 9);
    }
}
