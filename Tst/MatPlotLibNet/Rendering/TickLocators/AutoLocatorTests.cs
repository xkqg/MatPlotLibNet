// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Verifies <see cref="AutoLocator"/> behavior.</summary>
public class AutoLocatorTests
{
    /// <summary>Verifies that the locator implements ITickLocator.</summary>
    [Fact]
    public void ImplementsITickLocator()
    {
        ITickLocator locator = new AutoLocator();
        Assert.NotNull(locator);
    }

    /// <summary>Verifies that ticks are within [min, max].</summary>
    [Fact]
    public void Locate_AllTicksWithinRange()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(0, 10);
        Assert.All(ticks, t => Assert.InRange(t, 0, 10));
    }

    /// <summary>Verifies that at least one tick is returned for a normal range.</summary>
    [Fact]
    public void Locate_NormalRange_ReturnsAtLeastOneTick()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(0, 10);
        Assert.NotEmpty(ticks);
    }

    /// <summary>Verifies that the default target count of 5 produces approximately 5 ticks.</summary>
    [Fact]
    public void Locate_DefaultTargetCount_ProducesReasonableCount()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(0, 10);
        Assert.InRange(ticks.Length, 3, 8);
    }

    /// <summary>Verifies nice-number alignment: 0..10 yields [0, 2, 4, 6, 8, 10].</summary>
    [Fact]
    public void Locate_ZeroToTen_YieldsNiceNumbers()
    {
        var locator = new AutoLocator(5);
        double[] ticks = locator.Locate(0, 10);
        Assert.All(ticks, t => Assert.Equal(0, t % 2, 8));
    }

    /// <summary>Verifies that a zero-width range returns just the single value.</summary>
    [Fact]
    public void Locate_ZeroRange_ReturnsSingleValue()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(5, 5);
        Assert.Single(ticks);
        Assert.Equal(5, ticks[0]);
    }

    /// <summary>Verifies that ticks are in ascending order.</summary>
    [Fact]
    public void Locate_ReturnsAscendingOrder()
    {
        var locator = new AutoLocator();
        double[] ticks = locator.Locate(-3.7, 8.4);
        for (int i = 1; i < ticks.Length; i++)
            Assert.True(ticks[i] > ticks[i - 1]);
    }

    /// <summary>Verifies that a custom target count influences output count.</summary>
    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    public void Locate_CustomTargetCount_InfluencesCount(int target)
    {
        var locator = new AutoLocator(target);
        double[] ticks = locator.Locate(0, 100);
        Assert.InRange(ticks.Length, 2, target + 4);
    }
}
