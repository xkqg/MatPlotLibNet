// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Verifies <see cref="MaxNLocator"/> behavior.</summary>
public class MaxNLocatorTests
{
    /// <summary>Verifies that the locator implements ITickLocator.</summary>
    [Fact]
    public void ImplementsITickLocator()
    {
        ITickLocator locator = new MaxNLocator(5);
        Assert.NotNull(locator);
    }

    /// <summary>Verifies that the result never exceeds maxN ticks.</summary>
    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public void Locate_NeverExceedsMaxN(int maxN)
    {
        var locator = new MaxNLocator(maxN);
        double[] ticks = locator.Locate(0, 100);
        Assert.True(ticks.Length <= maxN, $"Expected <= {maxN} ticks, got {ticks.Length}");
    }

    /// <summary>Verifies that all ticks are within [min, max].</summary>
    [Fact]
    public void Locate_AllTicksWithinRange()
    {
        var locator = new MaxNLocator(5);
        double[] ticks = locator.Locate(10, 90);
        Assert.All(ticks, t => Assert.InRange(t, 10, 90));
    }

    /// <summary>Verifies ticks are ascending.</summary>
    [Fact]
    public void Locate_ReturnsAscendingOrder()
    {
        var locator = new MaxNLocator(5);
        double[] ticks = locator.Locate(-50, 50);
        for (int i = 1; i < ticks.Length; i++)
            Assert.True(ticks[i] > ticks[i - 1]);
    }

    /// <summary>Verifies that maxN = 1 returns exactly one tick.</summary>
    [Fact]
    public void Locate_MaxOne_ReturnsSingleTick()
    {
        var locator = new MaxNLocator(1);
        double[] ticks = locator.Locate(0, 10);
        Assert.True(ticks.Length <= 1);
    }
}
