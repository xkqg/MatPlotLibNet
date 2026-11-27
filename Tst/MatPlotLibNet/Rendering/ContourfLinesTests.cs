// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>A filled contour draws no iso-lines under the matplotlib themes: matplotlib 3.11.2's <c>contourf</c> makes one
/// <c>QuadContourSet</c> of filled <c>PathCollection</c>s whose <c>edgecolor</c> is set to <c>"none"</c> (<c>ContourSet.__init__</c>:
/// <c>self.set(edgecolor="none")</c>, and <c>"linewidths is ignored by contourf"</c>), so its picture has bands and no line. The
/// library drew a 0.5 px line along every level in the band colours, and still does under its own themes. A caller that names
/// <c>ShowLines</c> gets what it named under every theme.</summary>
public sealed class ContourfLinesTests
{
    private static readonly double[] Axis = [0.0, 1.0, 2.0];
    private static readonly double[,] Z = { { 0, 1, 2 }, { 1, 2, 3 }, { 2, 3, 4 } };

    private static string Svg(Theme theme, Action<ContourfSeries>? configure = null) =>
        Plt.Create().WithTheme(theme).AddSubPlot(1, 1, 1, ax => ax.Contourf(Axis, Axis, Z, configure)).ToSvg();

    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    public void UnderTheMatplotlibThemes_ALineIsNotDrawn_WhenShowLinesIsNotGiven(string name) =>
        Assert.DoesNotContain("<polyline", Svg(name.AsTheme()), StringComparison.Ordinal);

    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    [InlineData("default")]
    public void AGivenShowLinesTrue_DrawsTheLines_UnderEveryTheme(string name) =>
        Assert.Contains("<polyline", Svg(name.AsTheme(), s => s.ShowLines = true), StringComparison.Ordinal);

    [Theory]
    [InlineData("matplotlib-v2")]
    [InlineData("matplotlib-classic")]
    [InlineData("default")]
    public void AGivenShowLinesFalse_DrawsNoLine_UnderEveryTheme(string name) =>
        Assert.DoesNotContain("<polyline", Svg(name.AsTheme(), s => s.ShowLines = false), StringComparison.Ordinal);

    /// <summary>The property reads <see langword="true"/> at rest, as it always did; whether the lines are drawn then is the theme's.</summary>
    [Fact]
    public void ShowLines_ReadsTrueAtRest_AndNothingIsGiven()
    {
        var series = new ContourfSeries(Axis, Axis, Z);

        Assert.True(series.ShowLines);
        Assert.Null(series.GivenShowLines);

        series.ShowLines = true;
        Assert.True(series.GivenShowLines);
    }

    /// <summary>The JSON writes what was given, <see langword="true"/> included, and nothing otherwise, so a figure written before reads
    /// back as it is drawn under the theme it is read with.</summary>
    [Fact]
    public void TheJson_WritesAGivenShowLines_AndNothingOtherwise()
    {
        var resting = new ContourfSeries(Axis, Axis, Z);
        var given = new ContourfSeries(Axis, Axis, Z) { ShowLines = true };
        var off = new ContourfSeries(Axis, Axis, Z) { ShowLines = false };

        Assert.Null(resting.ToSeriesDto().ShowLines);
        Assert.True(given.ToSeriesDto().ShowLines);
        Assert.False(off.ToSeriesDto().ShowLines);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("UnderTheLibrarysOwnTheme_TheLinesAreDrawn_WhenShowLinesIsNotGiven", () => Svg(Theme.Default), "<polyline"),
        new DrawnToken("AGivenShowLines_SurvivesTheJson", () =>
        {
            var back = Plt.Create().WithTheme(Theme.MatplotlibV2)
                .AddSubPlot(1, 1, 1, ax => ax.Contourf(Axis, Axis, Z, s => s.ShowLines = true)).Build().RoundTripped();
            return back.ToSvg();
        }, "<polyline"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
