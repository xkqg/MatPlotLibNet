// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Where the free end of an axis scaled to its data stops, which the theme says (matplotlib's
/// <c>axes.autolimit_mode</c>). matplotlib 3.11.2, <c>_AxesBase.autoscale_view.handle_single_axis</c>: the data limits, the margin,
/// the sticky edges, then <c>locator.view_limits(x0, x1)</c>; <c>MaxNLocator.view_limits</c> returns
/// <c>self._raw_ticks(dmin, dmax)[[0, -1]]</c> when <c>rcParams['axes.autolimit_mode'] == 'round_numbers'</c> and the range as
/// it is otherwise. <c>plt.style.use('classic')</c> sets <c>'round_numbers'</c>, the default style <c>'data'</c>. The library's
/// other themes keep the nice bounds they have always drawn.</summary>
public sealed class AutolimitModeTests
{
    private static Axes With(params ISeries[] series)
    {
        var axes = new Axes();
        foreach (var s in series) axes.AddSeries(s);
        return axes;
    }

    private static Axes Bars() => With(new BarSeries([0.0, 1, 2], [3, 5.1, 2]));

    private static Axes Line() => With(new LineSeries([0, 1, 2], [1, 5.1, 3]));

    /// <summary>The classic style rounds the free end beside a sticky edge: <c>ax.bar([0, 1, 2], [3, 5.1, 2])</c> draws Y from 0
    /// to 6 and X from -0.5 to 2.5 (the sticky foot stays at 0; the top rounds to the tick above 5.1), where the library stopped
    /// at 5.1 and left the bar labels no room.</summary>
    [Fact]
    public void Classic_RoundsTheFreeEndOfABarChart_BesideItsStickyFoot()
    {
        var range = Bars().ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(0, 6).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-9);
        new Range1D(-0.5, 2.5).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-9);
    }

    /// <summary>An axis whose ticks name categories keeps its range: matplotlib places them with a <c>StrCategoryLocator</c>
    /// (or a verb's <c>FixedLocator</c>), whose <c>view_limits</c> is the base locator's and rounds nothing.
    /// <c>ax.bar(['a', 'b', 'c'], [3, 5.1, 2])</c> under the classic style draws X from -0.4 to 2.4 and Y from 0 to 6; the library
    /// stands category k on k + 0.5, so its bars span 0.1 to 2.9.</summary>
    [Fact]
    public void Classic_KeepsTheRangeOfAnAxisThatNamesCategories()
    {
        var range = With(new BarSeries(["a", "b", "c"], [3, 5.1, 2])).ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(0.1, 2.9).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-9);
        new Range1D(0, 6).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-9);
    }

    /// <summary>The classic style rounds a line's range out to its outermost ticks: <c>ax.plot([0, 1, 2], [1, 5.1, 3])</c> draws Y
    /// from 1 to 5.5 and X from 0 to 2 (margins 0).</summary>
    [Fact]
    public void Classic_RoundsALineOutToItsOutermostTicks()
    {
        var range = Line().ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(1, 5.5).AssertCloseTo(new Range1D(range.YMin, range.YMax), 1e-9);
        new Range1D(0, 2).AssertCloseTo(new Range1D(range.XMin, range.XMax), 1e-9);
    }

    /// <summary>The matplotlib default style ends an axis where the data and its margin end: the line draws Y from 0.795 to 5.305 and X from
    /// -0.1 to 2.1, the bars Y from 0 to 5.355, not out to the next tick. The library's own themes keep the range they have always drawn:
    /// the margin, widened to the nice bounds of the data where those reach further (the line to 6), and left alone beside a sticky edge.</summary>
    [Theory]
    [InlineData("matplotlib-v2", 5.305)]
    [InlineData("default", 6.0)]
    public void TheDefaultStyles_EndAnAxisWhereTheDataAndItsMarginEnd_OrAtTheirNiceBounds(string themeName, double lineYMax)
    {
        var theme = themeName.AsTheme();
        var line = Line().ComputeDataRanges(theme);
        var bars = Bars().ComputeDataRanges(theme);

        new Range1D(0.795, lineYMax).AssertCloseTo(new Range1D(line.YMin, line.YMax), 1e-9);
        new Range1D(-0.1, 2.1).AssertCloseTo(new Range1D(line.XMin, line.XMax), 1e-9);
        new Range1D(0, 5.355).AssertCloseTo(new Range1D(bars.YMin, bars.YMax), 1e-9);
    }

    /// <summary>Under the classic style a log axis rounds out to whole decades (<c>LogLocator.view_limits</c>:
    /// <c>_decade_less_equal</c>, <c>_decade_greater_equal</c>): a line over 3 .. 450 draws Y from 1 to 1000. A symmetric-log axis
    /// does the same on either side of zero (<c>SymmetricalLogLocator.view_limits</c>): -3 .. 50 draws -10 to 100.</summary>
    [Fact]
    public void Classic_RoundsALogAxisToWholeDecades_OnEitherSideOfZero()
    {
        var log = With(new LineSeries([0, 1, 2], [3, 450, 20]));
        log.YAxis.Scale = AxisScale.Log;
        var symlog = With(new LineSeries([0, 1, 2], [-3, 50, 20]));
        symlog.YAxis.Scale = AxisScale.SymLog;

        var logRange = log.ComputeDataRanges(Theme.MatplotlibClassic);
        var symRange = symlog.ComputeDataRanges(Theme.MatplotlibClassic);

        new Range1D(1, 1000).AssertCloseTo(new Range1D(logRange.YMin, logRange.YMax), 1e-9);
        new Range1D(-10, 100).AssertCloseTo(new Range1D(symRange.YMin, symRange.YMax), 1e-9);
    }

    /// <summary>A symmetric-log end at zero stays at zero, a negative one takes the decade below it, a positive one the decade
    /// above (<c>_decade_less_equal</c>, <c>_decade_greater_equal</c>).</summary>
    [Theory]
    [InlineData(-0.02, 3, -0.1, 10)]
    [InlineData(0, 40, 0, 100)]
    [InlineData(-0.5, 0, -1, 0)]
    [InlineData(-50, -0.3, -100, -0.1)]
    public void Classic_SymLogDecades(double lo, double hi, double expectedLo, double expectedHi)
    {
        var axis = new Axis { Scale = AxisScale.SymLog };

        var rounded = new AxisAutoscale(axis, isX: false, Theme.Default).RoundedOutToTicks(new Range1D(lo, hi), TickLadder.Classic);

        new Range1D(expectedLo, expectedHi).AssertCloseTo(new Range1D(rounded.Lo, rounded.Hi), 1e-9);
    }

    /// <summary>A logit axis and a date axis keep the range of their data, as matplotlib's logit and date locators round
    /// nothing; an axis with a limit keeps what it has, and so do an axis whose locator rounds nothing (a <c>FixedLocator</c>, whose
    /// <c>view_limits</c> is the base locator's) and a log range that reaches 0 or below, which has no decade (a 3-D axis is not
    /// lifted to positive values first). A locator that rounds ends the axis where it says (<c>MultipleLocator(3)</c>: 0 .. 3).</summary>
    [Fact]
    public void Classic_LeavesLogitDateALimitAndAFixedLocatorAlone()
    {
        var logit = new Axis { Scale = AxisScale.Logit };
        var date = new Axis { Scale = AxisScale.Date };
        var limited = new Axis { Max = 5.1 };
        var fixedTicks = new Axis { TickLocator = new FixedLocator([0.5]) };
        var located = new Axis { TickLocator = new MatPlotLibNet.Rendering.TickLocators.MultipleLocator(3) };
        var range = new Range1D(0.2, 0.9);

        Assert.Equal(range, new AxisAutoscale(logit, isX: false, Theme.Default).RoundedOutToTicks(range, TickLadder.Classic));
        Assert.Equal(range, new AxisAutoscale(date, isX: false, Theme.Default).RoundedOutToTicks(range, TickLadder.Classic));
        Assert.Equal(range, new AxisAutoscale(limited, isX: false, Theme.Default).RoundedOutToTicks(range, TickLadder.Classic));
        Assert.Equal(range, new AxisAutoscale(fixedTicks, isX: false, Theme.Default).RoundedOutToTicks(range, TickLadder.Classic));
        Assert.Equal(new Range1D(0, 3), new AxisAutoscale(located, isX: false, Theme.Default).RoundedOutToTicks(range, TickLadder.Classic));
        Assert.Equal(new Range1D(-1, 5), new AxisAutoscale(new Axis { Scale = AxisScale.Log }, isX: false, Theme.Default).RoundedOutToTicks(new Range1D(-1, 5), TickLadder.Classic));
    }

    /// <summary>The ends of matplotlib's classic ticks over a 2-D range (<c>MaxNLocator._raw_ticks</c> with the classic
    /// <c>AutoLocator</c>'s nine bins and steps 1, 2, 5 and 10), the same rule a 3-D axis rounds by: in this mode nine steps
    /// have to reach the top, which already asks for a step longer than the raw one of either (a 2-D axis' width over nine, a
    /// 3-D axis' 23/24 of that). 1 .. 17.5 ends at 18, a step of 2 (matplotlib's classic ticks 0, 2, .., 18).</summary>
    [Theory]
    [InlineData(1, 5.1, 1, 5.5)]
    [InlineData(1, 17.5, 0, 18)]
    [InlineData(1000.2, 1003.7, 1000, 1004)]
    [InlineData(-0.4, 2.4, -0.5, 2.5)]
    public void Classic_TwoD_RoundsToTheEndsOfMatplotlibsClassicTicks(double lo, double hi, double expectedLo, double expectedHi)
    {
        var rounded = new AxisAutoscale(new Axis(), isX: false, Theme.Default).RoundedOutToTicks(new Range1D(lo, hi), TickLadder.Classic);

        new Range1D(expectedLo, expectedHi).AssertCloseTo(new Range1D(rounded.Lo, rounded.Hi), 1e-9);
    }

    /// <summary>A range too narrow for its magnitude is widened by a trillionth of each end before it is rounded out, as
    /// <c>MaxNLocator.view_limits</c> asks <c>_nonsingular(dmin, dmax, expander=1e-12, tiny=1e-13)</c> first (matplotlib 3.11.2,
    /// <c>plt.style.use('classic')</c>; <c>AutoLocator().view_limits(1e6, 1e6 + 1e-8)</c> is 999999.999999 .. 1000000.000001):
    /// the interval is at most 1e-13 of the larger end, so each end moves by 1e-12 of itself, and the ends of the ticks over
    /// that are the limits. A range that is wide enough is not touched.</summary>
    [Theory]
    [InlineData(1e6, 1e6 + 1e-8, 999999.999999, 1000000.000001)]
    [InlineData(1000.0, 1000.00000000005, 999.999999999, 1000.000000001)]
    [InlineData(1.7e9, 1.7e9 + 1e-5, 1699999999.9984999, 1700000000.0015001)]
    [InlineData(-1000000.00000001, -1e6, -1000000.000001, -999999.999999)]
    [InlineData(1e6, 1e6 + 1e-3, 1000000.0, 1000000.001)]
    public void Classic_TwoD_WidensARangeTooNarrowForItsMagnitudeBeforeRoundingOut(double lo, double hi, double expectedLo, double expectedHi)
    {
        var rounded = new AxisAutoscale(new Axis(), isX: false, Theme.Default).RoundedOutToTicks(new Range1D(lo, hi), TickLadder.Classic);

        Assert.Equal(expectedLo, rounded.Lo, Math.Abs(expectedLo) * 1e-13);
        Assert.Equal(expectedHi, rounded.Hi, Math.Abs(expectedHi) * 1e-13);
    }

    /// <summary>The theme carries the mode: classic rounds to round numbers, the default style keeps the data's, the library's
    /// own themes name none; a theme derived from one and the theme's JSON keep it.</summary>
    [Fact]
    public void TheThemeCarriesTheMode_ThroughADerivedThemeAndTheJson()
    {
        Assert.Equal(AutolimitMode.RoundNumbers, Theme.MatplotlibClassic.Looks.Range.Autolimit);
        Assert.Equal(AutolimitMode.Data, Theme.MatplotlibV2.Looks.Range.Autolimit);
        Assert.Null(Theme.Default.Looks.Range.Autolimit);
        Assert.Equal(AutolimitMode.RoundNumbers, Theme.MatplotlibClassic.Cloned().Looks.Range.Autolimit);
        Assert.Equal(AutolimitMode.Data, Theme.MatplotlibV2.ThroughDto().Looks.Range.Autolimit);
        Assert.Equal(AutolimitMode.RoundNumbers, Theme.MatplotlibClassic.ThroughDto().Looks.Range.Autolimit);
        Assert.Null(ThemeDto.From(Theme.Default).AutolimitMode);
    }
}
