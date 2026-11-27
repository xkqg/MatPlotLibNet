// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>
/// The two guide lines an oscillator draws beside its signal (the commodity channel index at +100 and -100, Williams %R at -20
/// and -80) are the indicator's <see cref="IndicatorLevels"/>: one way to recolour them, restyle them, make them thinner or
/// leave them out, for every indicator that draws them, from the callback of the verb the app calls. Left alone they are what
/// they always were: two dashed lines, one pixel wide, in the theme's neutral colour.
/// </summary>
public class IndicatorLevelsTests
{
    private static readonly OhlcvBars Bars = OhlcvBars.Hlc(
        [10.0, 11, 12, 13, 14, 15, 16, 17], [5.0, 6, 7, 8, 9, 10, 11, 12], [7.0, 9, 10, 11, 12, 13, 14, 15]);

    public static TheoryData<string> Names => ["Cci", "WilliamsR"];

    private static double[] ValuesOf(string name) => name == "Cci" ? [100, -100] : [-20, -80];

    /// <summary>Builds the axes the way an app does, through the verb, handing the callback the indicator's levels.</summary>
    private static Axes Through(string name, Action<IndicatorLevels>? configure)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            _ = name == "Cci"
                ? ax.Cci(Bars, 3, c => configure?.Invoke(c.Levels))
                : ax.WilliamsR(Bars, 3, w => configure?.Invoke(w.Levels));
        }).Build();
        return figure.SubPlots[0];
    }

    // ── left alone: what the indicators always drew ───────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Names))]
    public void LeftAlone_TheLevelsAreTwoDashedLines_OnePixelWide_InTheNeutralColour(string name)
    {
        var lines = Through(name, null).ReferenceLines;

        Assert.Equal(ValuesOf(name), lines.Select(l => l.Value));
        Assert.All(lines, line =>
        {
            Assert.Equal(Orientation.Horizontal, line.Orientation);
            Assert.Null(line.Color);
            Assert.Equal(LineStyle.Dashed, line.LineStyle);
            Assert.Equal(1.0, line.LineWidth);
        });
    }

    // ── one way to style them ─────────────────────────────────────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(Names))]
    public void TheUpperAndLowerColours_AreGivenToTheirOwnLine(string name)
    {
        var lines = Through(name, levels =>
        {
            levels.UpperColor = Colors.Red;
            levels.LowerColor = Colors.Green;
        }).ReferenceLines;

        Assert.Equal(Colors.Red, lines[0].Color);
        Assert.Equal(Colors.Green, lines[1].Color);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void OneColourOnly_LeavesTheOtherLineInTheNeutralColour(string name)
    {
        var lines = Through(name, levels => levels.UpperColor = Colors.Red).ReferenceLines;

        Assert.Equal(Colors.Red, lines[0].Color);
        Assert.Null(lines[1].Color);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void TheLineStyleAndWidth_AreGivenToBothLines(string name)
    {
        var lines = Through(name, levels =>
        {
            levels.LineStyle = LineStyle.Dotted;
            levels.LineWidth = 2.5;
        }).ReferenceLines;

        Assert.All(lines, line =>
        {
            Assert.Equal(LineStyle.Dotted, line.LineStyle);
            Assert.Equal(2.5, line.LineWidth);
        });
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void LevelsThatAreNotVisible_AreNotDrawn_AndTheSignalStillIs(string name)
    {
        var axes = Through(name, levels => levels.Visible = false);

        Assert.Empty(axes.ReferenceLines);
        Assert.Single(axes.Series);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void TheLevels_AreDrawnOnEveryAxesTheIndicatorIsAppliedTo_EachWithItsOwnLines(string name)
    {
        var first = new Axes();
        var second = new Axes();
        Action<Axes> apply = name == "Cci"
            ? new Cci(Bars, 3) { Levels = { UpperColor = Colors.Red } }.Apply
            : new WilliamsR(Bars, 3) { Levels = { UpperColor = Colors.Red } }.Apply;

        apply(first);
        apply(second);

        Assert.Equal(2, first.ReferenceLines.Count);
        Assert.Equal(2, second.ReferenceLines.Count);
        Assert.NotSame(first.ReferenceLines[0], second.ReferenceLines[0]);
        Assert.Equal(Colors.Red, second.ReferenceLines[0].Color);
    }

    [Fact]
    public void TheLevelsOfTwoIndicators_AreIndependent()
    {
        var one = new Cci(Bars, 3);
        var other = new Cci(Bars, 3);

        one.Levels.UpperColor = Colors.Red;

        Assert.NotSame(one.Levels, other.Levels);
        Assert.Null(other.Levels.UpperColor);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void AnIndicatorWithNothingToPlot_DrawsNoLevels(string name)
    {
        var axes = new Axes();
        var tooShort = OhlcvBars.Hlc([1.0, 2], [0.5, 1.5], [0.7, 1.7]);

        if (name == "Cci") new Cci(tooShort, 14).Apply(axes); else new WilliamsR(tooShort, 14).Apply(axes);

        Assert.Empty(axes.ReferenceLines);
        Assert.Empty(axes.Series);
    }

    [Fact]
    public void TheLevelsAreStyledFromTheIndicatorsVerbOnTheFigureToo()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Cci(Bars, 3, c => c.Levels.UpperColor = Colors.Red)).Build();

        Assert.Equal(Colors.Red, figure.SubPlots[0].ReferenceLines[0].Color);
    }
}
