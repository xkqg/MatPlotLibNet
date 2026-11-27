// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Styling;

/// <summary>A line that names no width is drawn as wide as its theme says, and the matplotlib themes say what matplotlib's
/// <c>lines.linewidth</c> says, in points at 100 dpi. matplotlib 3.11.2, <c>rcParams['lines.linewidth']</c>: 1.5 in the default
/// style, 1.0 in the classic one (<c>plt.style.context('classic')</c>); a Line2D made with no width takes it, so the v2 line is
/// 1.5 * 100 / 72 = 2.08 px and the classic one 1.39 px. Every other theme keeps the 1.5 px the library has always drawn. A
/// width the author gives (1.5 included) and a width the theme's cycler names win over it, as an explicit <c>lw</c> and a
/// prop_cycle's linewidth win over the rcParam in matplotlib; and a style's <c>lines.linewidth</c> is matplotlib's points too,
/// laid over the theme.</summary>
public class ThemeLineWidthTests
{
    private static readonly double[] X = [0.0, 1.0, 2.0, 3.0];
    private static readonly double[] Y = [1.0, 2.0, 1.5, 3.0];

    private static XDocument Doc(Theme theme, Action<AxesBuilder> build) =>
        Plt.Create().WithTheme(theme).WithSize(600, 400).AddSubPlot(1, 1, 1, build).ToSvg().Xml();

    private static string LineWidth(XDocument doc, Color colour) =>
        (string)Assert.Single(doc.Each("polyline"), e => (string?)e.Attribute("stroke") == colour.ToHex())
            .Attribute("stroke-width")!;
    [Theory]
    [InlineData("MatplotlibV2", "2.08")]
    [InlineData("MatplotlibClassic", "1.39")]
    [InlineData("Default", "1.5")]
    [InlineData("Dark", "1.5")]
    public void ALineThatNamesNoWidth_IsAsWideAsItsThemeSays(string name, string expected)
    {
        var theme = name.AsTheme();

        Assert.Equal(expected, LineWidth(Doc(theme, ax => ax.Plot(X, Y)), theme.CycleColors[0]));
    }

    [Fact]
    public void TheMatplotlibThemes_SayLinesLinewidth_InPixels()
    {
        Assert.Equal(1.5.PointsInPixels, Theme.MatplotlibV2.Looks.Lines.Width);
        Assert.Equal(1.0.PointsInPixels, Theme.MatplotlibClassic.Looks.Lines.Width);
        Assert.Null(Theme.Default.Looks.Lines.Width);
    }

    [Fact]
    public void AWidthTheAuthorGives_IsDrawnAsItIs()
    {
        var theme = Theme.MatplotlibV2;

        Assert.Equal("3", LineWidth(Doc(theme, ax => ax.Plot(X, Y, s => s.LineWidth = 3)), theme.CycleColors[0]));
    }

    /// <summary>The theme's width stands for a line given no width: a kind whose own width is another (a regression line, 2)
    /// keeps it, and a width that was given, 1.5 included, is drawn as given.</summary>
    [Fact]
    public void AKindOfAnotherWidth_KeepsIt_AndAGivenWidthIsDrawnAsGiven()
    {
        var slot = Theme.MatplotlibV2.SeriesSlot(0);

        Assert.Equal(2.0, slot.LineWidthOf(null, LineWidthRule.TwoPixelLine));
        Assert.Equal(1.5.PointsInPixels, slot.LineWidthOf(LineWidthRule.Line));
        Assert.Equal(1.5, slot.LineWidthOf(1.5, LineWidthRule.Line));
    }

    /// <summary>The lines the library draws in its own look, with no counterpart in matplotlib's: they keep a line's resting width,
    /// 1.5 px, whatever width the theme names (here a style's <c>lines.linewidth</c> of 4 pt, 5.56 px, laid over the library's
    /// theme).</summary>
    public static TheoryData<string> OwnLookLines => ["quiver", "quiver key", "3-D quiver", "barbs", "violin mean", "box"];

    private static readonly Dictionary<string, Action<AxesBuilder>> OwnLookCharts = new()
    {
        ["quiver"] = ax => ax.Quiver([0.0, 1], [0.0, 1], [1.0, 0.5], [0.5, 1], s => s.Scale = 0.5),
        ["quiver key"] = ax => ax.QuiverKey(0.5, 0.5, 1, "1 unit"),
        ["3-D quiver"] = ax => ax.Quiver3D([0.0, 1], [0.0, 1], [0.0, 1], [1.0, 0.5], [0.5, 1], [1.0, 1]),
        ["barbs"] = ax => ax.Barbs([0.0, 1], [0.0, 1], [65.0, 15], [0.0, 90]),
        ["violin mean"] = ax => ax.Violin([[1.0, 2, 2, 3, 4]], s => { s.ShowMeans = true; s.ShowExtrema = false; }),
        ["box"] = ax => ax.BoxPlot([[1.0, 2, 2, 3, 4]]),
    };

    [Theory]
    [MemberData(nameof(OwnLookLines))]
    public void ALineOfTheLibrarysOwnLook_KeepsTheRestingWidth_WhateverWidthTheThemeNames(string chart)
    {
        var parameters = new RcParams();
        parameters.Set(RcParamKeys.LinesLineWidth, 4.0);
        var theme = parameters.ThemeOver(Theme.Default);

        var widths = Doc(theme, OwnLookCharts[chart]).Descendants()
            .Select(e => (string?)e.Attribute("stroke-width"))
            .Where(w => w is not null)
            .ToList();

        Assert.Contains("1.5", widths);
        Assert.DoesNotContain("5.56", widths);
    }

    /// <summary>A step histogram's outline is a patch's edge in matplotlib (<c>Axes.hist</c> draws it with <c>fill</c>, a
    /// <c>Polygon</c>), so it is as wide as <c>patch.linewidth</c>. matplotlib 3.11.2, printed:
    /// <c>ax.hist([1, 2, 2, 3], histtype='step')[2][0].get_linewidth()</c> is 1.0 (points) under <c>plt.style.use('default')</c>
    /// and under <c>'classic'</c>: 1.39 px at 100 dpi. The library's own themes keep the 1.5 px they have always drawn.</summary>
    [Theory]
    [InlineData("MatplotlibV2", "1.39")]
    [InlineData("MatplotlibClassic", "1.39")]
    [InlineData("Default", "1.5")]
    [InlineData("Dark", "1.5")]
    public void AStepHistogramsOutline_IsAsWideAsThePatchEdgeItsThemeNames(string name, string expected)
    {
        var theme = name.AsTheme();
        var doc = Doc(theme, ax => ax.Hist([1.0, 2, 2, 3, 3, 3], 3, s => { s.HistType = HistType.Step; s.Color = Colors.Red; }));

        Assert.Equal(expected, LineWidth(doc, Colors.Red));
    }

    /// <summary>A style's <c>lines.linewidth</c> is a line's width, not a patch's: a step histogram's outline keeps the 1.5 px of the
    /// library's own theme under a style of 4 pt.</summary>
    [Fact]
    public void AStepHistogramsOutline_IsNotWidenedByAStylesLineWidth()
    {
        var parameters = new RcParams();
        parameters.Set(RcParamKeys.LinesLineWidth, 4.0);
        var doc = Doc(parameters.ThemeOver(Theme.Default), ax => ax.Hist([1.0, 2, 2, 3, 3, 3], 3, s => { s.HistType = HistType.Step; s.Color = Colors.Red; }));

        Assert.Equal("1.5", LineWidth(doc, Colors.Red));
    }

    /// <summary>Lines matplotlib draws at <c>lines.linewidth</c>: the curve seaborn 0.13.2 draws on a pair grid's diagonal
    /// (<c>PairGrid.map_diag(kdeplot)</c>, an <c>ax.plot</c> line) and a 3-D stem's stems (<c>Axes3D.stem</c>'s
    /// <c>Line3DCollection</c>) and baseline (a <c>Line3D</c>). matplotlib 3.11.2, printed: <c>g.diag_axes[0].lines[0].get_linewidth()</c>
    /// 1.5 under <c>plt.style.use('default')</c> and 1.0 under <c>'classic'</c>; <c>stem([1, 2], [1, 2], [1, 2])</c> on a 3-D axes,
    /// <c>stemlines.get_linewidth()</c> [1.5] and [1.0], <c>baseline.get_linewidth()</c> 1.5 and 1.0. The library's own themes keep
    /// 1.5 px for the curve and 2.5 px for the stems and the baseline.</summary>
    [Theory]
    [InlineData("pair grid density", "MatplotlibV2", "2.08")]
    [InlineData("pair grid density", "MatplotlibClassic", "1.39")]
    [InlineData("pair grid density", "Default", "1.5")]
    [InlineData("3-D stem", "MatplotlibV2", "2.08")]
    [InlineData("3-D stem", "MatplotlibClassic", "1.39")]
    [InlineData("3-D stem", "Default", "2.5")]
    [InlineData("3-D stem", "Dark", "2.5")]
    public void ALineMatplotlibDrawsAtItsLineWidth_IsAsWideAsTheThemeSays(string chart, string name, string expected)
    {
        var theme = name.AsTheme();
        var colour = theme.CycleColors[0];
        Action<AxesBuilder> build = chart == "3-D stem"
            ? ax => ax.Stem3D([0.0, 1, 2], [0.0, 1, 2], [1.0, 2, 3], s => s.Color = colour)
            : ax => ax.PairGrid([[1.0, 2, 2, 3, 4], [2.0, 1, 3, 3, 5]], s => s.DiagonalKind = PairGridDiagonalKind.Kde);

        var widths = Doc(theme, build).Descendants()
            .Where(e => (string?)e.Attribute("stroke") == colour.ToHex() && e.Attribute("stroke-width") is not null)
            .Select(e => (string)e.Attribute("stroke-width")!)
            .Distinct()
            .ToList();

        Assert.Equal([expected], widths);
    }

    [Fact]
    public void AWidthTheCyclerNames_WinsOverTheThemes()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).WithLineWidths(4.0).Build();
        var theme = Theme.CreateFrom(Theme.MatplotlibV2).WithPropCycler(cycler).Build();

        Assert.Equal("4", LineWidth(Doc(theme, ax => ax.Plot(X, Y)), Colors.Red));
    }

    [Fact]
    public void ACyclerThatNamesNoWidth_LeavesTheThemes()
    {
        var cycler = new PropCyclerBuilder().WithColors(Colors.Red).Build();
        var theme = Theme.CreateFrom(Theme.MatplotlibV2).WithPropCycler(cycler).Build();

        Assert.Equal("2.08", LineWidth(Doc(theme, ax => ax.Plot(X, Y)), Colors.Red));
    }

    [Fact]
    public void AStylesLinesLinewidth_IsMatplotlibsPoints_LaidOverTheTheme()
    {
        var parameters = new RcParams();
        parameters.Set(RcParamKeys.LinesLineWidth, 2.0);

        var theme = parameters.ThemeOver(Theme.MatplotlibV2);

        Assert.Equal(2.0.PointsInPixels, theme.Looks.Lines.Width);
        Assert.Same(Theme.MatplotlibV2.PropCycler, theme.PropCycler);
        Assert.Equal("2.78", LineWidth(Doc(theme, ax => ax.Plot(X, Y)), theme.CycleColors[0]));
    }

    [Fact]
    public void TheThemesLineWidth_SurvivesTheJson()
    {
        var theme = Theme.CreateFrom(Theme.MatplotlibClassic).WithName("classic, renamed").Build();
        var figure = Plt.Create().WithTheme(theme).Plot(X, Y).Build();

        var back = figure.RoundTripped();

        Assert.Equal(1.0.PointsInPixels, back.Theme.Looks.Lines.Width);
        Assert.Equal(figure.ToSvg(), back.ToSvg());
    }

    [Fact]
    public void TheLegendSwatch_IsAsWideAsTheLine()
    {
        var theme = Theme.MatplotlibV2;
        var doc = Doc(theme, ax => ax.Plot(X, Y, s => s.Label = "a").WithLegend());

        var legend = Assert.Single(doc.Each("g"), e => (string?)e.Attribute("class") == "legend");
        var swatch = legend.Descendants().First(e => (string?)e.Attribute("stroke") == theme.CycleColors[0].ToHex());
        Assert.Equal("2.08", (string)swatch.Attribute("stroke-width")!);
    }
}
