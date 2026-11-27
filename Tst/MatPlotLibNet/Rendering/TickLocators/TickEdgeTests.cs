// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>The edge of a ladder of ticks (<see cref="TickEdge"/>): matplotlib's <c>_Edge_integer(step, offset)</c>, a step and the
/// tolerance its offset gives, worked out once and asked for the multiple at or below and at or above a value.</summary>
public class TickEdgeTests
{
    [Fact]
    public void AValueAHairOffAMultiple_CountsAsOnIt()
    {
        // 0.3 / 0.1 = 2.9999999999999996: a plain floor counts two steps, the edge three.
        var edge = TickEdge.Of(0.1, 0);

        Assert.Equal(3, edge.AtOrBelow(0.1 + 0.2));
        Assert.Equal(3, edge.AtOrAbove(0.1 + 0.2));
    }

    [Fact]
    public void WithNoOffset_TheToleranceIsATenBillionthOfAStep()
    {
        Assert.Equal(1e-10, TickEdge.Of(0.5, 0).Tolerance);
    }

    /// <summary>matplotlib 3.11.2 <c>_Edge_integer.closeto</c>: <c>tol = max(1e-10, 10 ** (log10(offset / step) - 12))</c>, at most
    /// 0.4999.</summary>
    [Theory]
    [InlineData(1e-8, 1000.0, 1e-1)]
    [InlineData(1.0, 1000.0, 1e-9)]
    [InlineData(1.0, 1.0, 1e-10)]
    [InlineData(1e-14, 1.0, 0.4999)]
    public void AnOffset_WidensTheTolerance_AsMatplotlibsEdgeDoes(double step, double offset, double expected)
    {
        Assert.Equal(expected, TickEdge.Of(step, offset).Tolerance, 12);
    }

    [Fact]
    public void TheMultiple_IsTheWholeCountTimesTheStep()
    {
        var edge = TickEdge.Of(0.1, 0);

        Assert.Equal(3 * 0.1, edge.MultipleAtOrAbove(0.25));
        Assert.Equal(2 * 0.1, edge.MultipleAtOrBelow(0.25));
    }

    /// <summary>The library's locator, and a multiple locator, over a range whose low end is 0.1 + 0.2 = 0.30000000000000004: the tick 0.3
    /// stands at the low end of the range, not a step past it.</summary>
    [Theory]
    [InlineData(true, 1.3)]
    [InlineData(false, 1.0)]
    public void ALocator_KeepsATickAHairInsideTheLowEnd(bool library, double high)
    {
        ITickLocator locator = library ? new AutoLocator(10) : new MultipleLocator(0.1);

        double[] ticks = locator.Locate(0.1 + 0.2, high);

        Assert.Equal(0.3, ticks[0]);
    }

    [Fact]
    public void TheLibrarysRoundedRange_HoldsALowEndAHairBelowAMultiple()
    {
        // Five ten-billionths of a step below 2 lies below 2: the range rounds out to the multiple below, so it holds the value.
        double lo = 2 - 5e-10;

        var (roundedLo, _) = new AutoLocator(5).ExpandToNiceBounds(lo, 7);

        Assert.True(roundedLo <= lo, $"{roundedLo} does not hold {lo}");
    }

    [Fact]
    public void TheTicksOfA3DAxis_StartAtTheMultipleAtOrAboveTheLowEnd_WithTheEdgeTolerance()
    {
        // Five ten-billionths of a step above 0.3 lies above 0.3, so the first tick at or above it is 0.4.
        double[] ticks = new TickRequest(new Range1D(0.3 + 5e-11, 1.2), 400, 10, AlongX: true, Theme.Default,
            TickAxisKind.Cube).Majors();

        Assert.Equal(0.4, ticks[0]);
    }
}
