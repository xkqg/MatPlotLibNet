// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Verifies <see cref="LogLocator"/> behavior.</summary>
public class LogLocatorTests
{
    /// <summary>Verifies that the locator implements ITickLocator.</summary>
    [Fact]
    public void ImplementsITickLocator()
    {
        ITickLocator locator = new LogLocator();
        Assert.NotNull(locator);
    }

    /// <summary>Verifies that ticks are powers of 10 within range.</summary>
    [Fact]
    public void Locate_OneToThousand_YieldsPowersOfTen()
    {
        var locator = new LogLocator();
        double[] ticks = locator.Locate(1, 1000);
        Assert.Contains(1.0, ticks);
        Assert.Contains(10.0, ticks);
        Assert.Contains(100.0, ticks);
        Assert.Contains(1000.0, ticks);
    }

    /// <summary>Verifies that all ticks are within [min, max].</summary>
    [Fact]
    public void Locate_AllTicksWithinRange()
    {
        var locator = new LogLocator();
        double[] ticks = locator.Locate(0.1, 10000);
        Assert.All(ticks, t => Assert.InRange(t, 0.1, 10000));
    }

    /// <summary>Verifies that ticks are ascending.</summary>
    [Fact]
    public void Locate_ReturnsAscendingOrder()
    {
        var locator = new LogLocator();
        double[] ticks = locator.Locate(0.01, 1000);
        for (int i = 1; i < ticks.Length; i++)
            Assert.True(ticks[i] > ticks[i - 1]);
    }

    /// <summary>Verifies that sub-decade ranges include at least one tick.</summary>
    [Fact]
    public void Locate_SubDecadeRange_ReturnsAtLeastOneTick()
    {
        var locator = new LogLocator();
        double[] ticks = locator.Locate(1, 5);
        Assert.NotEmpty(ticks);
    }

    /// <summary>Verifies that all ticks are strictly positive (log requires positive values).</summary>
    [Fact]
    public void Locate_AllTicksStrictlyPositive()
    {
        var locator = new LogLocator();
        double[] ticks = locator.Locate(0.001, 10);
        Assert.All(ticks, t => Assert.True(t > 0));
    }
}
