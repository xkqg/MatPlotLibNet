// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Verifies <see cref="MultipleLocator"/> behavior.</summary>
public class MultipleLocatorTests
{
    /// <summary>Verifies that the locator implements ITickLocator.</summary>
    [Fact]
    public void ImplementsITickLocator()
    {
        ITickLocator locator = new MultipleLocator(5);
        Assert.NotNull(locator);
    }

    /// <summary>Verifies that all ticks are exact multiples of the base.</summary>
    [Theory]
    [InlineData(5.0, 0, 20)]
    [InlineData(2.5, 0, 10)]
    [InlineData(0.1, 0, 0.5)]
    public void Locate_AllTicksAreMultiplesOfBase(double baseValue, double min, double max)
    {
        var locator = new MultipleLocator(baseValue);
        double[] ticks = locator.Locate(min, max);
        // Use ratio-based check: t/base should be very close to an integer
        Assert.All(ticks, t =>
        {
            double ratio = t / baseValue;
            double remainder = Math.Abs(ratio - Math.Round(ratio));
            Assert.True(remainder < 1e-6, $"Tick {t} is not a multiple of {baseValue} (ratio remainder = {remainder})");
        });
    }

    /// <summary>Verifies that all ticks are within [min, max].</summary>
    [Fact]
    public void Locate_AllTicksWithinRange()
    {
        var locator = new MultipleLocator(5);
        double[] ticks = locator.Locate(3, 22);
        Assert.All(ticks, t => Assert.InRange(t, 3, 22));
    }

    /// <summary>Verifies that base=5, range [0,20] yields 0,5,10,15,20.</summary>
    [Fact]
    public void Locate_Base5_ZeroToTwenty_YieldsExpected()
    {
        var locator = new MultipleLocator(5);
        double[] ticks = locator.Locate(0, 20);
        Assert.Equal([0, 5, 10, 15, 20], ticks);
    }

    /// <summary>Verifies ticks are ascending.</summary>
    [Fact]
    public void Locate_ReturnsAscendingOrder()
    {
        var locator = new MultipleLocator(3);
        double[] ticks = locator.Locate(1, 15);
        for (int i = 1; i < ticks.Length; i++)
            Assert.True(ticks[i] > ticks[i - 1]);
    }

    /// <summary>Verifies that negative ranges work correctly.</summary>
    [Fact]
    public void Locate_NegativeRange_WorksCorrectly()
    {
        var locator = new MultipleLocator(5);
        double[] ticks = locator.Locate(-10, 10);
        Assert.Contains(-10.0, ticks);
        Assert.Contains(0.0, ticks);
        Assert.Contains(10.0, ticks);
    }
}
