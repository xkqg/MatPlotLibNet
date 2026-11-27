// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering;

/// <summary>Under the classic theme an axis that names a locator of its own ends where that locator rounds it out to, as matplotlib's
/// <c>autoscale_view</c> asks the axis' major locator for its <c>view_limits</c> under <c>axes.autolimit_mode = 'round_numbers'</c>
/// (matplotlib 3.11.2, read with inspect): <c>MultipleLocator.view_limits</c> takes <c>_edge.le(dmin - offset) * step + offset</c> and
/// <c>_edge.ge(dmax - offset) * step + offset</c>, one step further each way when the two meet; <c>MaxNLocator.view_limits</c> (and so
/// <c>AutoLocator</c>'s) the ends of <c>_raw_ticks</c>; <c>LogLocator.view_limits</c> and <c>SymmetricalLogLocator.view_limits</c> the
/// decades at or beyond the ends; the base <c>Locator.view_limits</c>, which <c>FixedLocator</c>, <c>NullLocator</c> and the date
/// locators keep, the range as it is. Printed under <c>plt.style.use('classic')</c> for a line over x 0.3 .. 7.2:
/// <c>MultipleLocator(2.5)</c> (0, 7.5), <c>MultipleLocator(2.5, offset=0.5)</c> (-2, 8), <c>FixedLocator</c> (0.3, 7.2),
/// <c>MaxNLocator(4)</c> and <c>AutoLocator()</c> (0, 8); a log axis with <c>LogLocator()</c> over 2 .. 300 (1, 1000); a symlog axis with
/// <c>SymmetricalLogLocator(linthresh=1)</c> over -30 .. 300 (-100, 1000). The library kept the range of every axis with a locator of
/// its own.</summary>
public sealed class LocatorRoundsOutTests
{
    private static readonly Range1D Data = new(0.3, 7.2);

    private static Range1D RoundedOut(ITickLocator locator, Range1D? data = null, AxisScale scale = AxisScale.Linear) =>
        new AxisAutoscale(new Axis { TickLocator = locator, Scale = scale }, isX: false, Theme.Default).RoundedOutToTicks((data ?? Data), TickLadder.Classic);

    [Fact]
    public void AMultipleLocator_RoundsOutToItsMultiples() => new Range1D(0, 7.5).AssertCloseTo(RoundedOut(new MultipleLocator(2.5)), 1e-9);

    [Fact]
    public void AMultipleLocatorWithAnOffset_RoundsOutToTheShiftedMultiples() =>
        new Range1D(-2, 8).AssertCloseTo(RoundedOut(new MultipleLocator(2.5, offset: 0.5)), 1e-9);

    [Fact]
    public void AMultipleLocatorWhoseEndsMeet_TakesOneMoreStepEachWay()
    {
        // Both ends within the tolerance of the multiple 2.5: matplotlib's "if vmin == vmax: vmin -= 1; vmax += 1".
        new Range1D(1.5, 3.5).AssertCloseTo(RoundedOut(new MultipleLocator(2.5), new Range1D(2.5 - 1e-12, 2.5 + 1e-12)), 1e-9);
    }

    [Fact]
    public void AMaxNLocatorAndAnAutoLocator_RoundOutToTheEndsOfTheirTicks()
    {
        new Range1D(0, 8).AssertCloseTo(RoundedOut(new MaxNLocator(4)), 1e-9);
        new Range1D(0, 8).AssertCloseTo(RoundedOut(new AutoLocator()), 1e-9);
    }

    [Fact]
    public void AMaxNLocator_RoundsOutAtTheStepItsTicksAreLaidBy()
    {
        // The library's MaxNLocator caps the number of ticks: over 0 .. 10 five targets lay six ticks by 2, so four are taken (by
        // 2.5); one target lays a single tick at 5 over 0.3 .. 7.2, and the range rounds out to its multiples 0 and 10.
        new Range1D(0, 10).AssertCloseTo(RoundedOut(new MaxNLocator(5), new Range1D(0, 10)), 1e-9);
        new Range1D(0, 10).AssertCloseTo(RoundedOut(new MaxNLocator(1)), 1e-9);
    }

    [Fact]
    public void ALogLocator_RoundsOutToWholeDecades() =>
        new Range1D(1, 1000).AssertCloseTo(RoundedOut(new LogLocator(), new Range1D(2, 300), AxisScale.Log), 1e-9);

    [Fact]
    public void ALogLocatorOverARangeThatReachesZero_KeepsIt() =>
        new Range1D(0, 300).AssertCloseTo(RoundedOut(new LogLocator(), new Range1D(0, 300), AxisScale.Log), 1e-9);

    [Fact]
    public void ASymlogLocator_RoundsOutToTheDecadesOnEitherSideOfZero() =>
        new Range1D(-100, 1000).AssertCloseTo(RoundedOut(new SymlogLocator(1), new Range1D(-30, 300), AxisScale.SymLog), 1e-9);

    [Fact]
    public void AFixedLocator_KeepsTheRange() => new Range1D(0.3, 7.2).AssertCloseTo(RoundedOut(new FixedLocator([1, 2, 5])), 1e-9);

    [Fact]
    public void ALocatorFromOutsideTheLibrary_KeepsTheRange_AsMatplotlibsBaseLocatorDoes() =>
        new Range1D(0.3, 7.2).AssertCloseTo(RoundedOut(new TestTickLocator((min, max) => [.. Enumerable.Range((int)Math.Ceiling(min), (int)Math.Floor(max) - (int)Math.Ceiling(min) + 1).Select(i => (double)i)])), 1e-9);

    [Fact]
    public void ALimitOfTheAxis_StillWinsOverItsLocator()
    {
        var axis = new Axis { TickLocator = new MultipleLocator(2.5), Max = 7.2 };

        new Range1D(0.3, 7.2).AssertCloseTo(new AxisAutoscale(axis, isX: false, Theme.Default).RoundedOutToTicks(Data, TickLadder.Classic), 1e-9);
    }

    [Fact]
    public void Classic_ALineWhoseXAxisNamesAMultipleLocator_EndsAtItsMultiples()
    {
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.3, 7.2], [0, 1]));
        axes.XAxis.TickLocator = new MultipleLocator(2.5);

        var range = axes.ComputeDataRanges(Theme.MatplotlibClassic);

        Assert.Equal(0, range.XMin, 9);
        Assert.Equal(7.5, range.XMax, 9);
    }

    [Fact]
    public void V2_KeepsTheDataEnds_WhateverTheLocator()
    {
        // axes.autolimit_mode = 'data': view_limits rounds nothing, and the margin pads the data.
        var axes = new Axes();
        axes.AddSeries(new LineSeries([0.3, 7.2], [0, 1]));
        axes.XAxis.TickLocator = new MultipleLocator(2.5);

        var range = axes.ComputeDataRanges(Theme.MatplotlibV2);

        Assert.Equal(0.3 - 0.345, range.XMin, 9);
        Assert.Equal(7.2 + 0.345, range.XMax, 9);
    }
}
