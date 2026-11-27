// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>What every locator that is asked for the ticks of a range owes it: the ticks stand inside the range, and they rise. Each
/// row is one locator with the range it was asked about.</summary>
public class LocatedTicksTests
{
    /// <summary>A locator and the range it locates for.</summary>
    public static TheoryData<ITickLocator, double, double> Located => new()
    {
        { new AutoLocator(), -3.7, 8.4 },
        { new AutoLocator(), 0, 10 },
        { new LogLocator(), 0.1, 10000 },
        { new LogLocator(), 0.01, 1000 },
        { new MaxNLocator(5), 10, 90 },
        { new MaxNLocator(5), -50, 50 },
        { new MultipleLocator(5), 3, 22 },
        { new MultipleLocator(3), 1, 15 },
    };

    /// <summary>Verifies that all ticks are within [min, max].</summary>
    [Theory]
    [MemberData(nameof(Located))]
    public void Locate_AllTicksWithinRange(ITickLocator locator, double min, double max) =>
        Assert.All(locator.Locate(min, max), t => Assert.InRange(t, min, max));

    /// <summary>Verifies that ticks are ascending.</summary>
    [Theory]
    [MemberData(nameof(Located))]
    public void Locate_ReturnsAscendingOrder(ITickLocator locator, double min, double max)
    {
        double[] ticks = locator.Locate(min, max);

        for (int i = 1; i < ticks.Length; i++)
            Assert.True(ticks[i] > ticks[i - 1]);
    }

    /// <summary>The ranges a locator must give a tick for: a normal range, and a sub-decade one on a log axis.</summary>
    public static TheoryData<ITickLocator, double, double> RangesWithATick => new()
    {
        { new AutoLocator(), 0, 10 },
        { new LogLocator(), 1, 5 },
    };

    /// <summary>Verifies that at least one tick is returned for a normal range and for a sub-decade one.</summary>
    [Theory]
    [MemberData(nameof(RangesWithATick))]
    public void Locate_ReturnsAtLeastOneTick(ITickLocator locator, double min, double max) => Assert.NotEmpty(locator.Locate(min, max));
}
