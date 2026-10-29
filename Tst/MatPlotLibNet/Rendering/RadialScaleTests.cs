// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickFormatters;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>The one radial scale of a polar axes: how far its rings reach (the extent), which rings it draws and what they
/// say. The extent is the farthest radius any series reaches, 5 % further out, or the limit the axes was given, exactly;
/// a series with a radius of its own (a polar heatmap) fixes the extent at least that far without the 5 %.</summary>
public class RadialScaleTests
{
    private static PolarLineSeries Line(params double[] r) => new(r, [.. r.Select((_, i) => i * 0.5)]);

    // ---- the extent ---------------------------------------------------------------------------------------------------

    [Fact]
    public void WithNoSeries_TheExtentIsOne() =>
        Assert.Equal(1, RadialScale.ExtentOf([], null));

    [Fact]
    public void TheExtent_IsTheFarthestRadiusFiveCentFurtherOut() =>
        Assert.Equal(5.25, RadialScale.ExtentOf([Line(1, 2, 5)], null), 12);

    [Fact]
    public void TheExtent_TakesTheFarthestOfAllTheSeries() =>
        Assert.Equal(21, RadialScale.ExtentOf([Line(1, 2, 5), Line(20)], null), 12);

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ARadiusThatCannotBePlaced_IsLeftOutOfTheExtent(double missing) =>
        Assert.Equal(5.25, RadialScale.ExtentOf([Line(1, missing, 5)], null), 12);

    [Fact]
    public void WithNoRadiusThatCanBePlaced_TheExtentIsOne() =>
        Assert.Equal(1, RadialScale.ExtentOf([Line(double.NaN, double.PositiveInfinity)], null));

    [Fact]
    public void ARadiusBelowZero_ReachesNowhere_AndIsLeftOutOfTheExtent()
    {
        Assert.Equal(4.2, RadialScale.ExtentOf([Line(-20, 4)], null), 12);
        Assert.Equal(1, RadialScale.ExtentOf([Line(-3, -1)], null));
    }

    [Fact]
    public void ARadiusOfZero_ReachesNowhere() =>
        Assert.Equal(1, RadialScale.ExtentOf([Line(0, 0)], null));

    [Fact]
    public void ALimitTheAxesWasGiven_IsTheExtentExactly() =>
        Assert.Equal(7, RadialScale.ExtentOf([Line(1, 2, 20)], 7));

    [Fact]
    public void AHiddenSeries_StillCountsTowardsTheExtent()
    {
        var hidden = Line(20);
        hidden.Visible = false;

        Assert.Equal(21, RadialScale.ExtentOf([Line(1), hidden], null), 12);
    }

    [Fact]
    public void ASeriesWithARadiusOfItsOwn_FixesTheExtentAtLeastThatFar_WithoutTheFiveCent()
    {
        var heatmap = new PolarHeatmapSeries(new double[4, 3], 4, 3) { RMax = 10 };

        Assert.Equal(10, RadialScale.ExtentOf([heatmap], null));
        Assert.Equal(10, RadialScale.ExtentOf([heatmap, Line(4)], null));         // 4.2 does not reach 10
        Assert.Equal(21, RadialScale.ExtentOf([heatmap, Line(20)], null), 12);    // 21 is past it
    }

    /// <summary>A polar renderer invoked directly, with no axes around it, scales itself: its own extent and no rings.</summary>
    [Fact]
    public void ASeriesWithNoAxesAroundIt_MakesTheScaleOfItsOwn()
    {
        var scale = RadialScale.Alone(Line(1, 2, 5));

        Assert.Equal(5.25, scale.Extent, 12);
        Assert.Empty(scale.Rings);
        Assert.Empty(scale.Labels);
    }

    // ---- the rings ------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheRings_StandAtRoundNumbers_NotAtFifthsOfTheExtent()
    {
        var scale = RadialScale.For([Line(10)], new Axis(), maxRadiusPx: 340, tickFontPx: 13);

        Assert.Equal(10.5, scale.Extent, 12);
        Assert.Equal([2.0, 4, 6, 8, 10], scale.Rings);
        Assert.Equal(["2", "4", "6", "8", "10"], scale.Labels);
    }

    [Fact]
    public void ThereIsNoRingAtTheCentre()
    {
        var scale = RadialScale.For([Line(10)], new Axis(), 340, 13);

        Assert.DoesNotContain(0.0, scale.Rings);
    }

    [Fact]
    public void ARingStandsAtTheExtentItselfWhenThatIsARoundNumber()
    {
        var heatmap = new PolarHeatmapSeries(new double[4, 3], 4, 3) { RMax = 10 };

        var scale = RadialScale.For([heatmap], new Axis(), 340, 13);

        Assert.Equal(10, scale.Extent);
        Assert.Equal(10, scale.Rings[^1]);
    }

    [Fact]
    public void AShortRadius_AsksForFewerRings()
    {
        var scale = RadialScale.For([Line(10)], new Axis(), maxRadiusPx: 20, tickFontPx: 13);

        Assert.InRange(scale.Rings.Length, 1, 2);
    }

    [Fact]
    public void TheLabels_AreWrittenByTheAxesFormatterWhenItHasOne()
    {
        var axis = new Axis { TickFormatter = new CurrencyFormatterForTest() };

        var scale = RadialScale.For([Line(10)], axis, 340, 13);

        Assert.All(scale.Labels, label => Assert.StartsWith("#", label));
    }

    [Fact]
    public void TheLabelsAreWrittenInOneNumberFormat()
    {
        var scale = RadialScale.For([Line(0.9)], new Axis(), 340, 13);

        Assert.All(scale.Labels, label => Assert.Equal(scale.Labels[0].Length, label.Length));
    }

    [Fact]
    public void TheRingLabels_StandAtTwentyTwoAndAHalfDegrees() =>
        Assert.Equal(22.5, RadialScale.LabelAngleDegrees);

    /// <summary>A formatter that marks what it wrote.</summary>
    private sealed class CurrencyFormatterForTest : ITickFormatter
    {
        public string Format(double value) => "#" + value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
    }

    // ---- a stack of bars -----------------------------------------------------------------------------------------------

    private static PolarBarSeries Bar(params double[] r) => new(r, [.. r.Select((_, i) => i * 0.5)]);

    [Fact]
    public void StackedBars_StandOnTheBarsBeforeThem_AndTheScaleReachesTheTopOfTheStack()
    {
        PolarBarSeries first = Bar(3, 1), second = Bar(2, 4);

        var scale = RadialScale.For([first, second], new Axis(), 340, 13, BarMode.Stacked);

        Assert.Equal([0.0, 0], scale.FootOf(first)!);
        Assert.Equal([3.0, 1], scale.FootOf(second)!);
        Assert.Equal(5.25, scale.Extent, 12);       // the stack of the first wedge: 3 + 2 = 5, higher than 1 + 4
    }

    [Fact]
    public void BarsThatAreNotStacked_HaveNoFeet()
    {
        PolarBarSeries first = Bar(3), second = Bar(2);

        var scale = RadialScale.For([first, second], new Axis(), 340, 13);

        Assert.Null(scale.FootOf(first));
        Assert.Equal(3.15, scale.Extent, 12);
    }

    [Fact]
    public void OneBarSeries_IsNotAStack()
    {
        var only = Bar(3, 4);

        Assert.Null(RadialScale.For([only], new Axis(), 340, 13, BarMode.Stacked).FootOf(only));
    }

    [Fact]
    public void ARadiusThatIsNotDrawn_AddsNothingToAStack()
    {
        PolarBarSeries first = Bar(double.NaN, -3, 2), second = Bar(1, 1, 1);

        var scale = RadialScale.For([first, second], new Axis(), 340, 13, BarMode.Stacked);

        Assert.Equal([0.0, 0, 2], scale.FootOf(second)!);
    }

    [Fact]
    public void ALimitTheAxesWasGiven_IsTheExtentOfAStackToo()
    {
        var scale = RadialScale.For([Bar(3), Bar(2)], new Axis { Max = 9 }, 340, 13, BarMode.Stacked);

        Assert.Equal(9, scale.Extent);
    }
}
