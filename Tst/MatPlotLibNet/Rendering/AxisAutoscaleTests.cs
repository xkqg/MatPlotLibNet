// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>What an axis does with its range (<see cref="AxisAutoscale"/>): seeded from its limits, a set limit exact, put on its
/// scale, padded in the space it is drawn in, held to the sticky edges, ended by the theme's mode, turned the way it is drawn. The
/// public members of <see cref="Range1D"/> that take an axis forward here, so the value and the policy have one owner each.</summary>
public class AxisAutoscaleTests
{
    [Fact]
    public void TheSeed_IsTheAxisLimits_AndAnUnsetSideIsEmpty()
    {
        Assert.Equal(Range1D.Empty, new AxisAutoscale(new Axis(), isX: true, Theme.Default).Seed);
        Assert.Equal(new Range1D(2, double.MinValue), new AxisAutoscale(new Axis { Min = 2 }, isX: true, Theme.Default).Seed);
        Assert.Equal(Range1D.FromAxis(new Axis { Min = 2, Max = 5 }), new AxisAutoscale(new Axis { Min = 2, Max = 5 }, isX: false, Theme.Default).Seed);
    }

    [Fact]
    public void ASetLimit_IsTheRangeDrawn_AndAFreeSideStaysOffTheOtherSide()
    {
        Assert.Equal(new Range1D(1.5, 2.5), new AxisAutoscale(new Axis { Min = 1.5, Max = 2.5 }, false, Theme.Default).WithExplicitLimits(new Range1D(1, 3)));
        Assert.Equal(new Range1D(1, 3), new AxisAutoscale(new Axis(), false, Theme.Default).WithExplicitLimits(new Range1D(1, 3)));
        Assert.Equal(new Range1D(5, 6), new AxisAutoscale(new Axis { Min = 5 }, false, Theme.Default).WithExplicitLimits(new Range1D(1, 3)));
        Assert.Equal(new Range1D(-1, 0), new AxisAutoscale(new Axis { Max = 0 }, false, Theme.Default).WithExplicitLimits(new Range1D(1, 3)));
    }

    /// <summary>Every scale pads in the space it is drawn in: on a linear and a date axis that is the value itself, to the bit of the
    /// width times the margin; a log axis pads in decades.</summary>
    [Theory]
    [InlineData(AxisScale.Linear)]
    [InlineData(AxisScale.Date)]
    public void ALinearAxis_IsPaddedByTheShareOfItsWidth_ToTheBit(AxisScale scale)
    {
        var range = new Range1D(0.1, 7.3);
        double pad = range.Width * 0.05;

        var padded = new AxisAutoscale(new Axis { Scale = scale }, false, Theme.Default).Padded(range, 0.05);

        Assert.Equal(BitConverter.DoubleToInt64Bits(0.1 - pad), BitConverter.DoubleToInt64Bits(padded.Lo));
        Assert.Equal(BitConverter.DoubleToInt64Bits(7.3 + pad), BitConverter.DoubleToInt64Bits(padded.Hi));
    }

    [Fact]
    public void ALogAxis_IsPaddedInDecades_AndASetSideStays()
    {
        var padded = new AxisAutoscale(new Axis { Scale = AxisScale.Log }, false, Theme.Default).Padded(new Range1D(10, 1000), 0.05);
        var held = new AxisAutoscale(new Axis { Scale = AxisScale.Log, Min = 10 }, false, Theme.Default).Padded(new Range1D(10, 1000), 0.05);

        Assert.Equal(Math.Pow(10, 0.9), padded.Lo, 12);
        Assert.Equal(Math.Pow(10, 3.1), padded.Hi, 12);
        Assert.Equal(10, held.Lo);
    }

    [Fact]
    public void ARangeIsPutOnItsScale_WithTheSmallestPositiveValueForALogitAxis()
    {
        Assert.Equal(new Range1D(0.2, 1 - 0.2), new AxisAutoscale(new Axis { Scale = AxisScale.Logit }, false, Theme.Default).OnScale(new Range1D(0, 1), 0.2));
        Assert.Equal(new Range1D(0.002, 2), new AxisAutoscale(new Axis { Scale = AxisScale.Log }, false, Theme.Default).OnScale(new Range1D(0, 2), 0.2));
        Assert.Equal(new Range1D(0, 2), new AxisAutoscale(new Axis(), false, Theme.Default).OnScale(new Range1D(0, 2), 0.2));
    }

    [Fact]
    public void AStickyEdge_TakesBackWhatThePaddingPushedPastIt_UnlessALimitIsSet()
    {
        var free = new AxisAutoscale(new Axis(), false, Theme.Default);
        var limited = new AxisAutoscale(new Axis { Min = -0.5 }, false, Theme.Default);

        Assert.Equal(new Range1D(0, 10.5), free.ClampSticky(new Range1D(-0.5, 10.5), 0, null, new Range1D(0, 10)));
        Assert.Equal(new Range1D(-0.5, 10.5), free.ClampSticky(new Range1D(-0.5, 10.5), 0, null, new Range1D(-3, 10)));
        Assert.Equal(new Range1D(-0.5, 10.5), limited.ClampSticky(new Range1D(-0.5, 10.5), 0, null, new Range1D(0, 10)));
        Assert.Equal(new Range1D(-0.5, 10), free.ClampSticky(new Range1D(-0.5, 10.5), null, 10, new Range1D(0, 10)));
    }

    /// <summary>An axis whose scale brings a locator keeps its range under the library's nice bounds; a date scale brings one only
    /// along X, so a date Y axis is widened like a linear one.</summary>
    [Fact]
    public void TheNiceBounds_StandAsideForAScaleThatBringsItsLocator()
    {
        var unpadded = new Range1D(-37, 88);
        var range = new Range1D(-43.25, 94.25);

        Assert.Equal(range, new AxisAutoscale(new Axis { Scale = AxisScale.SymLog }, false, Theme.Default).ExpandedToNiceBounds(range, unpadded, false, new AutoLocator()));
        Assert.Equal(range, new AxisAutoscale(new Axis { Scale = AxisScale.Date }, true, Theme.Default).ExpandedToNiceBounds(range, unpadded, false, new AutoLocator()));
        Assert.NotEqual(range, new AxisAutoscale(new Axis { Scale = AxisScale.Date }, false, Theme.Default).ExpandedToNiceBounds(range, unpadded, false, new AutoLocator()));
        Assert.NotEqual(range, new AxisAutoscale(new Axis(), false, Theme.Default).ExpandedToNiceBounds(range, unpadded, false, new AutoLocator()));
        Assert.Equal(range, new AxisAutoscale(new Axis(), false, Theme.Default).ExpandedToNiceBounds(range, unpadded, true, new AutoLocator()));
        Assert.Equal(range, new AxisAutoscale(new Axis { Margin = 0 }, false, Theme.Default).ExpandedToNiceBounds(range, unpadded, false, new AutoLocator()));
        Assert.Equal(range, new AxisAutoscale(new Axis(), false, Theme.Default).ExpandedToNiceBounds(range, new Range1D(3, 3), false, new AutoLocator()));
    }

    [Fact]
    public void TheThemesMode_EndsTheRange()
    {
        var auto = new AxisAutoscale(new Axis(), true, Theme.Default);
        var range = new Range1D(0.3, 7.2);

        Assert.Equal(range, auto.Autolimited(range, AutolimitRule.Data, null, range, false));
        Assert.Equal(new Range1D(0, 8), auto.Autolimited(range, AutolimitRule.RoundNumbers, null, range, false));
        Assert.Equal(range, auto.Autolimited(range, AutolimitRule.RoundNumbers, null, range, false, namesCategories: true));
        Assert.Equal(auto.ExpandedToNiceBounds(range, range, false, new AutoLocator()), auto.Autolimited(range, AutolimitRule.Library, null, range, false));
    }

    /// <summary>An axis rounds out to the ticks of its own locator first, then to those its scale lays; a set limit and a range with no
    /// width or no finite width are kept.</summary>
    [Fact]
    public void ARoundingOut_ReadsTheAxisLocatorFirst_ThenItsScale()
    {
        var range = new Range1D(0.3, 7.2);

        Assert.Equal(new Range1D(0, 9), new AxisAutoscale(new Axis { TickLocator = new MultipleLocator(3) }, false, Theme.Default).RoundedOutToTicks(range, TickLadder.Classic));
        Assert.Equal(new Range1D(0.1, 10), new AxisAutoscale(new Axis { Scale = AxisScale.Log }, false, Theme.Default).RoundedOutToTicks(range, TickLadder.Classic));
        Assert.Equal(range, new AxisAutoscale(new Axis { Max = 7.2 }, false, Theme.Default).RoundedOutToTicks(range, TickLadder.Classic));
        Assert.Equal(new Range1D(2, 2), new AxisAutoscale(new Axis(), false, Theme.Default).RoundedOutToTicks(new Range1D(2, 2), TickLadder.Classic));
        Assert.Equal(new Range1D(0, double.PositiveInfinity),
            new AxisAutoscale(new Axis(), false, Theme.Default).RoundedOutToTicks(new Range1D(0, double.PositiveInfinity), TickLadder.Classic));
    }

    [Fact]
    public void AnInvertedAxis_IsDrawnHighToLow_Once()
    {
        var inverted = new AxisAutoscale(new Axis { Inverted = true }, false, Theme.Default);

        Assert.Equal(new Range1D(5, 1), inverted.Oriented(new Range1D(1, 5)));
        Assert.Equal(new Range1D(5, 1), inverted.Oriented(new Range1D(5, 1)));
        Assert.Equal(new Range1D(1, 5), new AxisAutoscale(new Axis(), false, Theme.Default).Oriented(new Range1D(1, 5)));
    }

    /// <summary>An axis past the primary: a set limit exact, the range normalised, put on its scale, padded by five per cent and turned
    /// the way it is drawn.</summary>
    [Fact]
    public void AnAxisPastThePrimary_IsDrawnByTheSameSteps()
    {
        Assert.Equal(new Range1D(-0.05, 1.05), new AxisAutoscale(new Axis(), false, Theme.Default).DrawnPastPrimary(Range1D.Empty, null, [], asked: true));
        Assert.Equal(new Range1D(10.5, -0.5), new AxisAutoscale(new Axis { Inverted = true }, false, Theme.Default).DrawnPastPrimary(new Range1D(0, 10), null, [], asked: true));
        Assert.Equal(new Range1D(2, 10.4), new AxisAutoscale(new Axis { Min = 2 }, false, Theme.Default).DrawnPastPrimary(new Range1D(0, 10), null, [], asked: true));
    }

    /// <summary>An empty axis past the primary that nothing asks for keeps (0, 1) under the matplotlib themes (a <c>twinx()</c> with nothing
    /// on it), and is opened by the library's rule and padded under the library's own themes, asked or not.</summary>
    [Fact]
    public void AnEmptyAxisPastThePrimary_NothingAsksFor_KeepsItsFirstViewUnderTheMatplotlibThemesOnly()
    {
        Assert.Equal(new Range1D(0, 1), new AxisAutoscale(new Axis(), false, Theme.MatplotlibV2).DrawnPastPrimary(Range1D.Empty, null, [], asked: false));
        Assert.Equal(new Range1D(1, 0), new AxisAutoscale(new Axis { Inverted = true }, false, Theme.MatplotlibV2).DrawnPastPrimary(Range1D.Empty, null, [], asked: false));
        Assert.Equal(new Range1D(-0.05, 1.05), new AxisAutoscale(new Axis(), false, Theme.Default).DrawnPastPrimary(Range1D.Empty, null, [], asked: false));
    }

    /// <summary>The opening of a range is the library's rule under the library's own themes and the scale's locator's under the
    /// matplotlib themes; a low end alone is one value; a range set high to low keeps its turn.</summary>
    [Fact]
    public void ARangeIsOpened_ByTheThemesRule()
    {
        var library = new AxisAutoscale(new Axis(), false, Theme.Default);
        var matplotlib = new AxisAutoscale(new Axis(), false, Theme.MatplotlibV2);

        Assert.Equal(new Range1D(0, 1), library.Opened(Range1D.Empty, null));
        Assert.Equal(new Range1D(2.5, 3.5), library.Opened(new Range1D(3, 3), null));
        Assert.Equal(new Range1D(-0.05, 0.05), matplotlib.Opened(Range1D.Empty, null));
        Assert.Equal(new Range1D(2.85, 3.15), matplotlib.Opened(new Range1D(3, double.MinValue), null));
        Assert.Equal(new Range1D(3.15, 2.85), matplotlib.Opened(new Range1D(3, 3) with { }, null).OrientedLike(new Range1D(4, 2)));
        Assert.Equal(new Range1D(5.5, 0.5), matplotlib.Opened(new Range1D(5.5, 0.5), null));
        Assert.Null(library.FirstViewWhenEmpty(Range1D.Empty));
        Assert.Null(matplotlib.FirstViewWhenEmpty(new Range1D(1, 2)));
        Assert.Equal(new Range1D(0, 1), matplotlib.FirstViewWhenEmpty(Range1D.Empty));
    }

    /// <summary>A range with an end set at infinity holds no data the locator could open: matplotlib refuses such a limit
    /// (<c>set_xlim(0, np.inf)</c> raises "Axis limits cannot be NaN or Inf", <c>axes/_base.py</c> 3913), and its
    /// <c>nonsingular</c> reads an end that is not finite as no data at all (-0.05, 0.05). Under every theme the range is kept as it
    /// is, as the library's own rule keeps it, so the rules that measure it (the aspect of the plot cell) see it cannot be measured.</summary>
    [Theory]
    [InlineData(0.0, double.PositiveInfinity)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 2.0)]
    public void ARangeWithAnEndSetAtInfinity_IsKeptAsItIs_UnderEveryTheme(double low, double high)
    {
        var range = new Range1D(low, high);

        Assert.Equal(range, new AxisAutoscale(new Axis(), false, Theme.Default).Opened(range, null));
        Assert.Equal(range, new AxisAutoscale(new Axis(), false, Theme.MatplotlibV2).Opened(range, null));
        Assert.Equal(range, new AxisAutoscale(new Axis(), false, Theme.MatplotlibClassic).Opened(range, null));
    }

    /// <summary>The public members of <see cref="Range1D"/> that take an axis say what the policy says.</summary>
    [Fact]
    public void TheRangesPublicMembers_ForwardToThePolicy()
    {
        var log = new Axis { Scale = AxisScale.Log };
        var range = new Range1D(-5, 250);

        Assert.Equal(new AxisAutoscale(log, false, Theme.Default).Padded(new Range1D(2, 250), 0.05), new Range1D(2, 250).Padded(0.05, log));
        Assert.Equal(ScaleRule.Log.Lifted(range), range.PositiveForLog(log));
        Assert.Equal(new AxisAutoscale(new Axis(), false, Theme.Default).ClampSticky(range, 0, null, new Range1D(0, 10)),
            range.ClampSticky(0, null, new Range1D(0, 10), new Axis()));
        Assert.Equal(new AxisAutoscale(new Axis(), false, Theme.Default).ExpandedToNiceBounds(range, range, false, new AutoLocator()),
            range.ExpandedToNiceBoundsIfAuto(new Axis(), range, false, new AutoLocator()));
    }
}
