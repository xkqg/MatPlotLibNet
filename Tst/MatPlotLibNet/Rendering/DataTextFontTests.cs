// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>
/// The text a chart draws among its data (annotations, bar labels, radar spokes, pie percentages, funnel stages, graph
/// node names) is as large as the tick labels of the same theme: one role, <see cref="ThemedFontProvider.DataTextFont"/>,
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

    private static string TextTag(string svg, string text)
    {
        var match = Regex.Match(svg, "<text [^>]*>" + Regex.Escape(text) + "</text>");
        Assert.True(match.Success, $"no <text> reading '{text}' in the picture");
        return match.Value;
    }

    private static string Attribute(string tag, string name) =>
        Regex.Match(tag, name + "=\"([^\"]*)\"").Groups[1].Value;

    private static double SizeOf(string svg, string text) =>
        double.Parse(Attribute(TextTag(svg, text), "font-size"), CultureInfo.InvariantCulture);

    private static FigureBuilder On(Theme theme) => Plt.Create().WithSize(600, 450).WithTheme(theme);

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void DataTextFont_IsTheThemesDefaultFont_InTheForegroundColour(string themeName)
    {
        var theme = Themes[themeName];

        var font = ThemedFontProvider.DataTextFont(theme);

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
        var font = ThemedFontProvider.DataTextFont(Theme.Default, null, Colors.Red);

        Assert.Equal(ThemedFontProvider.DataTextFont(Theme.Default) with { Color = Colors.Red }, font);
    }

    [Fact]
    public void APlacedTextWithoutAFontOrAColour_TakesTheThemesTextFont() =>
        Assert.Equal(ThemedFontProvider.DataTextFont(Theme.Default), ThemedFontProvider.DataTextFont(Theme.Default, null, null));

    [Fact]
    public void ASuppliedFontWithoutAColour_TakesTheColourBesideIt_ElseTheThemes()
    {
        var supplied = new Font { Family = "serif", Size = 20 };

        Assert.Equal(supplied with { Color = Colors.Red }, ThemedFontProvider.DataTextFont(Theme.Default, supplied, Colors.Red));
        Assert.Equal(supplied with { Color = Theme.Default.ForegroundText },
            ThemedFontProvider.DataTextFont(Theme.Default, supplied, null));
    }

    [Fact]
    public void AColourOnTheSuppliedFont_Wins()
    {
        var supplied = new Font { Family = "serif", Size = 20, Color = Colors.Blue };

        Assert.Equal(supplied, ThemedFontProvider.DataTextFont(Theme.Default, supplied, Colors.Red));
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void AnAnnotation_WithoutAFont_IsDrawnAtTheSizeOfTheThemesText(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Plot([0.0, 10.0], [0.0, 10.0]).Annotate("peak", 5, 5)).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "peak"), 9);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ABarLabel_IsDrawnAtTheSizeOfTheThemesText(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Bar(["a", "b"], [1234.0, 777.0], s => s.ShowLabels = true)).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "1234"), 9);
        Assert.Equal(ThemedFontProvider.DataTextFont(theme).Color!.Value.ToHex(), Attribute(TextTag(svg, "1234"), "fill"));
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ARadarSpokeName_IsDrawnAtTheSizeOfTheThemesText(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Radar(["alpha", "beta", "gamma"], [1.0, 2.0, 3.0])).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "alpha"), 9);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void APiePercentage_IsDrawnAtTheSizeOfTheThemesText_InWhite(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Pie([1.0, 3.0], null, s => s.AutoPct = "{0:F0}%")).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "25%"), 9);
        Assert.Equal("#FFFFFF", Attribute(TextTag(svg, "25%"), "fill"));
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void AFunnelStageName_IsDrawnAtTheSizeOfTheThemesText(string themeName)
    {
        var theme = Themes[themeName];
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.Funnel(["Visited", "Bought"], [100.0, 40.0])).ToSvg();

        Assert.Equal(theme.DefaultFont.Size, SizeOf(svg, "Visited"), 9);
    }

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void AGraphNodeName_IsDrawnInTheThemesFont_NotAFixedOne(string themeName)
    {
        var theme = Themes[themeName];
        var nodes = new[] { new GraphNode("alpha", 0, 0), new GraphNode("beta", 1, 1) };
        var edges = new[] { new GraphEdge("alpha", "beta", 1.0, false) };
        string svg = On(theme).AddSubPlot(1, 1, 1, ax => ax.NetworkGraph(nodes, edges, s => s.Layout = GraphLayout.Manual)).ToSvg();

        var tag = TextTag(svg, "alpha");
        Assert.Equal(theme.DefaultFont.Size, double.Parse(Attribute(tag, "font-size"), CultureInfo.InvariantCulture), 9);
        Assert.Equal(theme.DefaultFont.Family, Attribute(tag, "font-family"));
        Assert.Equal(theme.ForegroundText.ToHex(), Attribute(tag, "fill"));
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
