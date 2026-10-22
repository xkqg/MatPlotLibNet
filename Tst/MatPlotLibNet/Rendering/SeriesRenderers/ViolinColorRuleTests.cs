// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.SeriesRenderers;

/// <summary>A violin's colour has one rule, read by the body, by the lines drawn over it and by the legend swatch: the
/// colour the series names, else the colour its theme gives a violin (a theme can: matplotlib's classic style paints
/// every violin yellow with red lines), else the theme's colour at the series' slot. Measured before the fix under the
/// classic theme: the body was #BFBF00 whatever colour the series named, and the swatch was #0000FF (no colour set) or
/// #4C78A8 (colour set), a colour the body did not wear.</summary>
public class ViolinColorRuleTests
{
    private static readonly Color ThemeBody = Color.FromHex("#112233");
    private static readonly Color ThemeStats = Color.FromHex("#445566");
    private static readonly Color Named = Color.FromHex("#4C78A8");

    private static readonly Theme ViolinColoursTheme =
        new("violin colours", Theme.Default) { ViolinBodyColor = ThemeBody, ViolinStatsColor = ThemeStats };

    private static readonly double[] Sample = [4.1, 4.8, 5.0, 5.3, 5.5, 5.9, 6.2, 6.4, 6.8, 7.1, 7.7, 8.4];

    private static string Render(Theme theme, Color? named) =>
        Plt.Create().WithTheme(theme).WithSize(500, 350)
            .AddSubPlot(1, 1, 1, ax => ax.Violin([Sample], v => { v.Label = "Violin"; v.Color = named; }))
            .Build().ToSvg();

    private static string Hex(Color color) => color.ToHex().ToUpperInvariant()[..7];

    private static string BodyFill(string svg) =>
        Regex.Match(svg, @"<polygon points=""[^""]+"" fill=""(#[0-9A-F]{6})""").Groups[1].Value;

    private static string BodyEdge(string svg) =>
        Regex.Match(svg, @"<polygon points=""[^""]+"" fill=""#[0-9A-F]{6}"" fill-opacity=""[0-9.eE+-]+"" stroke=""(#[0-9A-F]{6})""").Groups[1].Value;

    /// <summary>The bar from the lowest to the highest observation: the first line 1.5 points wide, which no tick or spine is.</summary>
    private static string StatsLine(string svg) =>
        Regex.Match(svg, @"<line [^>]*stroke=""(#[0-9A-F]{6})"" stroke-width=""1\.5"" />").Groups[1].Value;

    /// <summary>The legend's swatch for the violin: the last plain filled rectangle (the legend frame carries an opacity).</summary>
    private static string Swatch(string svg) =>
        Regex.Matches(svg, @"<rect [^>]*fill=""(#[0-9A-F]{6})"" />").Last().Groups[1].Value;

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void TheBody_TheLinesOverIt_AndTheLegendSwatch_WearOneRule(bool themeGivesViolinColours, bool seriesNamesAColour)
    {
        var theme = themeGivesViolinColours ? ViolinColoursTheme : Theme.Default;
        var slot = theme.CycleColors[0];
        var body = seriesNamesAColour ? Named : themeGivesViolinColours ? ThemeBody : slot;
        var lines = seriesNamesAColour ? Named : themeGivesViolinColours ? ThemeStats : slot;

        string svg = Render(theme, seriesNamesAColour ? Named : null);

        Assert.Equal(Hex(body), BodyFill(svg));
        Assert.Equal(Hex(body), BodyEdge(svg));
        Assert.Equal(Hex(lines), StatsLine(svg));
        Assert.Equal(Hex(body), Swatch(svg));
    }

    /// <summary>The case that was measured: matplotlib's classic style, whose violins are yellow with red lines.</summary>
    [Fact]
    public void UnderTheClassicTheme_ANamedColourDyesTheWholeViolin_AndTheSwatchShowsIt()
    {
        string plain = Render(Theme.MatplotlibClassic, null);
        string named = Render(Theme.MatplotlibClassic, Named);

        Assert.Equal("#BFBF00", BodyFill(plain));
        Assert.Equal("#BFBF00", Swatch(plain));
        Assert.Equal("#FF0000", StatsLine(plain));
        Assert.Equal("#4C78A8", BodyFill(named));
        Assert.Equal("#4C78A8", Swatch(named));
        Assert.Equal("#4C78A8", StatsLine(named));
    }
}
