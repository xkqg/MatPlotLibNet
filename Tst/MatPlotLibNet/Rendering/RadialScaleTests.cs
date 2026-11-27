// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one radial scale of a polar axes: the radii it runs between (its low end, matplotlib's <c>rmin</c>, and its
/// extent), the radius at its centre (<c>rorigin</c>), which rings it draws and what they say. Under the library's own themes the
/// extent is the farthest radius any series reaches, 5 % of the span from the lower of 0 and the nearest radius further out, or the
/// limit the axes was given, exactly; a series with a radius of its own (a polar heatmap) fixes the extent at least that far
/// without the 5 %. Under the matplotlib themes the scale is ranged as matplotlib's polar axes autoscales its radial axis.</summary>
public class RadialScaleTests
{
    private static PolarLineSeries Line(params double[] r) => new(r, [.. r.Select((_, i) => i * 0.5)]);

    /// <summary>The extent of <paramref name="series"/> under the library's own themes, with the limit the axes was given.</summary>
    private static double ExtentOf(ISeries[] series, double? limit) => RadialScale.RangeOf(series, new Axis { Max = limit }, Theme.Default).Hi;

    // ---- the extent ---------------------------------------------------------------------------------------------------

    [Fact]
    public void WithNoSeries_TheExtentIsOne() =>
        Assert.Equal(1, ExtentOf([], null));

    [Fact]
    public void TheExtent_IsTheFarthestRadiusFiveCentFurtherOut() =>
        Assert.Equal(5.25, ExtentOf([Line(1, 2, 5)], null), 12);

    [Fact]
    public void TheExtent_TakesTheFarthestOfAllTheSeries() =>
        Assert.Equal(21, ExtentOf([Line(1, 2, 5), Line(20)], null), 12);

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ARadiusThatCannotBePlaced_IsLeftOutOfTheExtent(double missing) =>
        Assert.Equal(5.25, ExtentOf([Line(1, missing, 5)], null), 12);

    [Fact]
    public void WithNoRadiusThatCanBePlaced_TheExtentIsOne() =>
        Assert.Equal(1, ExtentOf([Line(double.NaN, double.PositiveInfinity)], null));

    /// <summary>A radius below zero lowers the low end, and the span from there is padded 5 % both ways, as matplotlib's
    /// default style pads it (printed: r -20 and 4 give rlim (-21.2, 5.2)); a scale wholly below zero ends 5 % short of its
    /// nearest radius (printed: r -3 and -1 give (-3.1, -0.9)).</summary>
    [Fact]
    public void ARadiusBelowZero_LowersTheLowEnd_AndTheSpanIsPaddedBothWays()
    {
        var across = RadialScale.RangeOf([Line(-20, 4)], new Axis(), Theme.Default);
        var below = RadialScale.RangeOf([Line(-3, -1)], new Axis(), Theme.Default);

        Assert.Equal(-21.2, across.Lo, 9);
        Assert.Equal(5.2, across.Hi, 9);
        Assert.Equal(-3.1, below.Lo, 9);
        Assert.Equal(-0.9, below.Hi, 9);
    }

    [Fact]
    public void ARadiusAtOrAboveZero_LeavesTheLowEndAtZero() =>
        Assert.Equal(0, RadialScale.RangeOf([Line(1, 2, 5)], new Axis(), Theme.Default).Lo);

    /// <summary>One radius below zero, repeated, is padded by 5 % of its own size both ways, as matplotlib's <c>nonsingular</c>
    /// widens a range of no width.</summary>
    [Fact]
    public void OneRadiusBelowZero_IsPaddedByAPartOfItself()
    {
        var range = RadialScale.RangeOf([Line(-10, -10)], new Axis(), Theme.Default);

        Assert.Equal(-10.5, range.Lo, 9);
        Assert.Equal(-9.5, range.Hi, 9);
    }

    /// <summary>A scale below zero stops at zero: the radial axis of matplotlib's polar axes is sticky at 0 (printed: r -10 to
    /// -0.01 gives (-10.4995, 0)).</summary>
    [Fact]
    public void AScaleBelowZero_StopsAtZero()
    {
        var range = RadialScale.RangeOf([Line(-10, -0.01)], new Axis(), Theme.Default);

        Assert.Equal(0, range.Hi);
    }

    [Fact]
    public void ALimitThatIsNotANumber_IsLeftAside()
    {
        var range = RadialScale.RangeOf([Line(1, 2, 5)], new Axis { Min = double.NaN, Max = double.PositiveInfinity }, Theme.Default);

        Assert.Equal(0, range.Lo);
        Assert.Equal(5.25, range.Hi, 12);
    }

    [Fact]
    public void LimitsOfNoLength_LeaveTheWorkedOutScale()
    {
        var range = RadialScale.RangeOf([Line(1, 2, 5)], new Axis { Min = 3, Max = 3 }, Theme.Default);

        Assert.Equal(0, range.Lo);
        Assert.Equal(5.25, range.Hi, 12);
    }

    [Fact]
    public void UnderTheMatplotlibThemes_AnAxesWithNothingOnIt_RunsFromZeroToOne()
    {
        // matplotlib: a polar axes with no data has rlim (0, 1) (RadialLocator.nonsingular of the initial limits).
        var range = RadialScale.RangeOf([], new Axis(), Theme.MatplotlibV2);

        Assert.Equal(0, range.Lo);
        Assert.Equal(1, range.Hi);
    }

    [Fact]
    public void ALowEndTheAxesWasGiven_IsTheLowEndExactly()
    {
        // matplotlib: ax.set_ylim(-5, 10) on a polar axes gives rlim (-5, 10).
        var range = RadialScale.RangeOf([Line(1, 5, 9)], new Axis { Min = -5, Max = 10 }, Theme.Default);

        Assert.Equal(-5, range.Lo);
        Assert.Equal(10, range.Hi);
    }

    [Fact]
    public void ARadiusOfZero_ReachesNowhere() =>
        Assert.Equal(1, ExtentOf([Line(0, 0)], null));

    [Fact]
    public void ALimitTheAxesWasGiven_IsTheExtentExactly() =>
        Assert.Equal(7, ExtentOf([Line(1, 2, 20)], 7));

    [Fact]
    public void AHiddenSeries_StillCountsTowardsTheExtent()
    {
        var hidden = Line(20);
        hidden.Visible = false;

        Assert.Equal(21, ExtentOf([Line(1), hidden], null), 12);
    }

    [Fact]
    public void ASeriesWithARadiusOfItsOwn_FixesTheExtentAtLeastThatFar_WithoutTheFiveCent()
    {
        var heatmap = new PolarHeatmapSeries(new double[4, 3], 4, 3) { RMax = 10 };

        Assert.Equal(10, ExtentOf([heatmap], null));
        Assert.Equal(10, ExtentOf([heatmap, Line(4)], null));         // 4.2 does not reach 10
        Assert.Equal(21, ExtentOf([heatmap, Line(20)], null), 12);    // 21 is past it
    }

    // ---- matplotlib's polar autoscale under the matplotlib themes ---------------------------------------------------------
    // Printed with matplotlib 3.11.2: plt.style.use(style); ax = fig.add_subplot(projection='polar'); ax.plot(theta, r) (or bar,
    // scatter, pcolormesh); fig.canvas.draw(); ax.get_ylim(), ax.get_rorigin(). The radial axis is sticky at 0
    // (RadialAxis.sticky_edges.y = [0]), its locator's view_limits take min(0, vmin), v2 pads 5 % of the data's span and classic
    // rounds out to its ticks.
    private static double[] Spread(double from, double to, int count) => [.. Enumerable.Range(0, count).Select(i => from + (to - from) * i / (count - 1))];

    [Theory]
    [InlineData("v2", -20.85, -2.15)]
    [InlineData("classic", -20.0, -2.0)]
    public void RadiiFromMinus20ToMinus3_RangeTheScaleAsMatplotlib(string theme, double min, double max)
    {
        var scale = RadialScale.For([Line(Spread(-20, -3, 10))], new Axis(), 340, 13, theme: theme.AsTheme());

        Assert.Equal(min, scale.Min, 9);
        Assert.Equal(max, scale.Extent, 9);
        Assert.Equal(min, scale.Origin, 9);     // rorigin follows rmin when it was not set
    }

    [Theory]
    [InlineData("v2", 1.0, 10.0, 0.0, 10.45)]
    [InlineData("classic", 1.0, 10.0, 0.0, 10.0)]
    [InlineData("v2", -5.0, 5.0, -5.5, 5.5)]
    [InlineData("classic", -5.0, 5.0, -6.0, 6.0)]
    [InlineData("v2", -20.0, 4.0, -21.2, 5.2)]
    [InlineData("classic", -20.0, 4.0, -20.0, 5.0)]
    [InlineData("v2", -10.0, -0.01, -10.4995, 0.0)]
    [InlineData("v2", 0.0, 0.0, -0.055, 0.055)]
    [InlineData("classic", 0.0, 0.0, -0.06, 0.06)]
    public void ALineIsRangedAsMatplotlibRangesIt(string theme, double from, double to, double min, double max)
    {
        var range = RadialScale.RangeOf([Line(Spread(from, to, 10))], new Axis(), theme.AsTheme());

        Assert.Equal(min, range.Lo, 9);
        Assert.Equal(max, range.Hi, 9);
    }

    [Theory]
    [InlineData("v2", 0.0, 5.25)]
    [InlineData("classic", 0.0, 5.0)]
    public void BarsAreRangedAsMatplotlibRangesThem(string theme, double min, double max)
    {
        var range = RadialScale.RangeOf([Bar(1, 3, 5)], new Axis(), theme.AsTheme());

        Assert.Equal(min, range.Lo, 9);
        Assert.Equal(max, range.Hi, 9);
    }

    [Theory]
    [InlineData("v2", -2.35, 5.35)]
    [InlineData("classic", -2.0, 5.0)]
    public void ABarBelowZero_LowersTheLowEnd_AsMatplotlibDrawsIt(string theme, double min, double max)
    {
        var range = RadialScale.RangeOf([Bar(-2, 3, 5)], new Axis(), theme.AsTheme());

        Assert.Equal(min, range.Lo, 9);
        Assert.Equal(max, range.Hi, 9);
    }

    [Theory]
    [InlineData("v2", 0.0, 9.35)]
    [InlineData("classic", 0.0, 10.0)]
    public void AScatterIsRangedWithTheScatterMarginOfTheTheme(string theme, double min, double max)
    {
        // matplotlib's classic scatter raises the margins to 0.05 (Axes.scatter in classic mode); the caller hands that margin over.
        var styling = theme.AsTheme();
        var scatter = new PolarScatterSeries([2, 5, 9], [0, 1, 2]);
        var axes = new Axes { CoordinateSystem = CoordinateSystem.Polar };
        axes.AddSeries(scatter);

        var range = RadialScale.RangeOf([scatter], new Axis(), styling, axes.ScatterMarginIn(styling));

        Assert.Equal(min, range.Lo, 9);
        Assert.Equal(max, range.Hi, 9);
    }

    [Fact]
    public void AHeatmapIsRangedToItsOwnRadius_UnderTheMatplotlibThemes()
    {
        var heatmap = new PolarHeatmapSeries(new double[4, 3], 4, 3) { RMax = 10 };

        var range = RadialScale.RangeOf([heatmap], new Axis(), Theme.MatplotlibV2);

        Assert.Equal(0, range.Lo);
        Assert.Equal(10, range.Hi);
    }

    // ---- the origin -------------------------------------------------------------------------------------------------------

    [Fact]
    public void AnOriginTheAxesWasGiven_StandsAtTheCentre_AndTheLowEndStaysWhereItWas()
    {
        // matplotlib: ax.plot(theta, [1, 5, 9]); ax.set_rorigin(-3) gives rlim (0, 9.4) and rorigin -3 under v2; the ring at 0 is drawn,
        // as it stands off the centre.
        var scale = RadialScale.For([Line(1, 5, 9)], new Axis(), 340, 13, theme: Theme.MatplotlibV2, origin: -3);

        Assert.Equal(0, scale.Min);
        Assert.Equal(9.4, scale.Extent, 9);
        Assert.Equal(-3, scale.Origin);
        Assert.Contains(0.0, scale.Rings);
    }

    /// <summary>Under the matplotlib themes the rings are the ticks of the theme's locator over the scale, as matplotlib's polar
    /// axes rings itself with its radial axis' AutoLocator (printed for r -20 to -3: v2 -20, -17.5, ..., -2.5 inside (-20.85,
    /// -2.15); classic -18, -16, ..., -2 inside (-20, -2)), so the classic scale, rounded out to those ticks, ends on a ring.</summary>
    [Theory]
    [InlineData("v2", new[] { -20.0, -17.5, -15, -12.5, -10, -7.5, -5, -2.5 })]
    [InlineData("classic", new[] { -18.0, -16, -14, -12, -10, -8, -6, -4, -2 })]
    public void UnderTheMatplotlibThemes_TheRingsAreTheThemesTicks(string theme, double[] rings)
    {
        var scale = RadialScale.For([Line(Spread(-20, -3, 10))], new Axis(), 340, 13, theme: theme.AsTheme());

        Assert.Equal(rings, scale.Rings);
    }

    [Fact]
    public void TheRingsStandBetweenTheEnds_NoneAtTheCentre()
    {
        var scale = RadialScale.For([Line(Spread(-20, -3, 10))], new Axis(), 340, 13, theme: Theme.MatplotlibV2);

        Assert.NotEmpty(scale.Rings);
        Assert.All(scale.Rings, ring => Assert.InRange(ring, scale.Min + 1e-9, scale.Extent));
    }

    [Fact]
    public void ARingBelowZero_IsLabelledWithTheTrueMinusUnderATheme_ThatWritesIt()
    {
        var scale = RadialScale.For([Line(Spread(-20, -3, 10))], new Axis(), 340, 13, theme: Theme.MatplotlibV2);

        Assert.StartsWith("−", scale.Labels[0]);
    }

    /// <summary>A polar renderer invoked directly, with no axes around it, scales itself: its own extent and no rings.</summary>
    [Fact]
    public void ASeriesWithNoAxesAroundIt_MakesTheScaleOfItsOwn()
    {
        var scale = RadialScale.Alone(Line(1, 2, 5), Theme.Default);

        Assert.Equal(5.25, scale.Extent, 12);
        Assert.Empty(scale.Rings);
        Assert.Empty(scale.Labels);
    }

    // ---- the rings ------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheRings_StandAtRoundNumbers_NotAtFifthsOfTheExtent()
    {
        var scale = RadialScale.For([Line(10)], new Axis(), maxRadiusPx: 340, tickFontPx: 13, theme: Theme.Default);

        Assert.Equal(10.5, scale.Extent, 12);
        Assert.Equal([2.0, 4, 6, 8, 10], scale.Rings);
        Assert.Equal(["2", "4", "6", "8", "10"], scale.Labels);
    }

    [Fact]
    public void ThereIsNoRingAtTheCentre()
    {
        var scale = RadialScale.For([Line(10)], new Axis(), 340, 13, Theme.Default);

        Assert.DoesNotContain(0.0, scale.Rings);
    }

    [Fact]
    public void ARingStandsAtTheExtentItselfWhenThatIsARoundNumber()
    {
        var heatmap = new PolarHeatmapSeries(new double[4, 3], 4, 3) { RMax = 10 };

        var scale = RadialScale.For([heatmap], new Axis(), 340, 13, Theme.Default);

        Assert.Equal(10, scale.Extent);
        Assert.Equal(10, scale.Rings[^1]);
    }

    [Fact]
    public void TheRings_FollowALocatorTheRadialAxisWasGiven()
    {
        // matplotlib: ax.yaxis.set_major_locator(MultipleLocator(3)) on a polar axes rings it at 3, 6 and 9.
        var axis = new Axis { TickLocator = new MultipleLocator(3) };

        var scale = RadialScale.For([Line(10)], axis, maxRadiusPx: 340, tickFontPx: 13, theme: Theme.Default);

        Assert.Equal([3.0, 6, 9], scale.Rings);
        Assert.Equal(["3", "6", "9"], scale.Labels);
    }

    /// <summary>A locator the radial axis was given is told how many labels fit along it, as a 2-D axis tells it: the radial axis is
    /// matplotlib's <c>YAxis</c>, whose <c>get_tick_space</c> is the axes' height (the circle's, twice the radius) over two label sizes,
    /// and <c>LogLocator.tick_values</c> reads <c>np.clip(self.axis.get_tick_space(), 2, 9)</c>. A radius of 30 px with 10 px labels
    /// leaves room for 3: matplotlib 3.11.2's <c>LogLocator(numticks=3).tick_values(1, 1e12)</c> in view is 1, 1e6, 1e12, and the
    /// ring at the centre is left out.</summary>
    [Fact]
    public void AGivenLocator_IsToldTheTickSpaceOfTheCirclesHeight()
    {
        var axis = new Axis { TickLocator = new LogLocator() };

        var scale = RadialScale.Over(new Range1D(1, 1e12), axis, maxRadiusPx: 30, tickFontPx: 10, Theme.MatplotlibV2);

        Assert.Equal([1e6, 1e12], scale.Rings);
    }

    /// <summary>A major tick spacing of the radial axis rings the scale at its multiples, as it places the ticks of a 2-D axis (the
    /// library's <see cref="MultipleLocator"/>): matplotlib 3.11.2 with <c>ax.yaxis.set_major_locator(MultipleLocator(2.5))</c> on a polar
    /// axes of radius 10 prints the rings 2.5, 5, 7.5 and 10.</summary>
    [Fact]
    public void TheRings_FollowTheSpacingOfTheRadialAxisMajorTicks()
    {
        var axis = new Axis();
        axis.MajorTicks = axis.MajorTicks with { Spacing = 2.5 };

        var scale = RadialScale.For([Line(10)], axis, maxRadiusPx: 340, tickFontPx: 13, theme: Theme.Default);

        Assert.Equal([2.5, 5, 7.5, 10], scale.Rings);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("APolarAxesGivenAYTickLocator_DrawsItsRingsThere", () => Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax.PolarPlot([10, 10, 10], [0, 1, 2]).SetYTickLocator(new MultipleLocator(3)))
            .ToSvg(), ">9<") { Absent = [">8<"] },
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void AShortRadius_AsksForFewerRings()
    {
        var scale = RadialScale.For([Line(10)], new Axis(), maxRadiusPx: 20, tickFontPx: 13, theme: Theme.Default);

        Assert.InRange(scale.Rings.Length, 1, 2);
    }

    [Fact]
    public void TheLabels_AreWrittenByTheAxesFormatterWhenItHasOne()
    {
        var axis = new Axis { TickFormatter = new TestTickFormatter(value => "#" + value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)) };

        var scale = RadialScale.For([Line(10)], axis, 340, 13, Theme.Default);

        Assert.All(scale.Labels, label => Assert.StartsWith("#", label));
    }

    [Fact]
    public void TheLabelsAreWrittenInOneNumberFormat()
    {
        var scale = RadialScale.For([Line(0.9)], new Axis(), 340, 13, Theme.Default);

        Assert.All(scale.Labels, label => Assert.Equal(scale.Labels[0].Length, label.Length));
    }

    [Fact]
    public void TheRingLabels_StandAtTwentyTwoAndAHalfDegrees() =>
        Assert.Equal(22.5, RadialScale.LabelAngleDegrees);

    // ---- a stack of bars -----------------------------------------------------------------------------------------------

    private static PolarBarSeries Bar(params double[] r) => new(r, [.. r.Select((_, i) => i * 0.5)]);

    [Fact]
    public void StackedBars_StandOnTheBarsBeforeThem_AndTheScaleReachesTheTopOfTheStack()
    {
        PolarBarSeries first = Bar(3, 1), second = Bar(2, 4);

        var scale = RadialScale.For([first, second], new Axis(), 340, 13, Theme.Default, BarMode.Stacked);

        Assert.Equal([0.0, 0], scale.FootOf(first)!);
        Assert.Equal([3.0, 1], scale.FootOf(second)!);
        Assert.Equal(5.25, scale.Extent, 12);       // the stack of the first wedge: 3 + 2 = 5, higher than 1 + 4
    }

    [Fact]
    public void BarsThatAreNotStacked_HaveNoFeet()
    {
        PolarBarSeries first = Bar(3), second = Bar(2);

        var scale = RadialScale.For([first, second], new Axis(), 340, 13, Theme.Default);

        Assert.Null(scale.FootOf(first));
        Assert.Equal(3.15, scale.Extent, 12);
    }

    [Fact]
    public void OneBarSeries_IsNotAStack()
    {
        var only = Bar(3, 4);

        Assert.Null(RadialScale.For([only], new Axis(), 340, 13, Theme.Default, BarMode.Stacked).FootOf(only));
    }

    [Fact]
    public void ARadiusThatIsNotDrawn_AddsNothingToAStack()
    {
        PolarBarSeries first = Bar(double.NaN, -3, 2), second = Bar(1, 1, 1);

        var scale = RadialScale.For([first, second], new Axis(), 340, 13, Theme.Default, BarMode.Stacked);

        Assert.Equal([0.0, 0, 2], scale.FootOf(second)!);
    }

    [Fact]
    public void ALimitTheAxesWasGiven_IsTheExtentOfAStackToo()
    {
        var scale = RadialScale.For([Bar(3), Bar(2)], new Axis { Max = 9 }, 340, 13, Theme.Default, BarMode.Stacked);

        Assert.Equal(9, scale.Extent);
    }
}
