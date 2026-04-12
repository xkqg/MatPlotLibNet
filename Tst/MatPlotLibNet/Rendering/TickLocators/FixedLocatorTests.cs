// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;

namespace MatPlotLibNet.Tests.Rendering.TickLocators;

/// <summary>Verifies <see cref="FixedLocator"/> behavior.</summary>
public class FixedLocatorTests
{
    /// <summary>Verifies that the locator implements ITickLocator.</summary>
    [Fact]
    public void ImplementsITickLocator()
    {
        ITickLocator locator = new FixedLocator([1, 2, 3]);
        Assert.NotNull(locator);
    }

    /// <summary>Verifies that positions within range are returned.</summary>
    [Fact]
    public void Locate_ReturnsPositionsWithinRange()
    {
        var locator = new FixedLocator([0, 1, 2, 3, 4, 5]);
        double[] ticks = locator.Locate(1, 4);
        Assert.Equal([1, 2, 3, 4], ticks);
    }

    /// <summary>Verifies that positions outside range are excluded.</summary>
    [Fact]
    public void Locate_ExcludesPositionsOutsideRange()
    {
        var locator = new FixedLocator([-10, 0, 5, 15]);
        double[] ticks = locator.Locate(0, 10);
        Assert.DoesNotContain(-10.0, ticks);
        Assert.DoesNotContain(15.0, ticks);
    }

    /// <summary>Verifies that all given positions within range are returned.</summary>
    [Fact]
    public void Locate_AllPositionsInRangeReturned()
    {
        var locator = new FixedLocator([2, 4, 6, 8]);
        double[] ticks = locator.Locate(0, 10);
        Assert.Equal([2, 4, 6, 8], ticks);
    }

    /// <summary>Verifies that boundary positions are included.</summary>
    [Fact]
    public void Locate_BoundaryPositionsIncluded()
    {
        var locator = new FixedLocator([0, 5, 10]);
        double[] ticks = locator.Locate(0, 10);
        Assert.Contains(0.0, ticks);
        Assert.Contains(10.0, ticks);
    }

    /// <summary>Verifies that empty input returns empty output.</summary>
    [Fact]
    public void Locate_EmptyPositions_ReturnsEmpty()
    {
        var locator = new FixedLocator([]);
        double[] ticks = locator.Locate(0, 10);
        Assert.Empty(ticks);
    }

    /// <summary>Verifies that none-in-range returns empty.</summary>
    [Fact]
    public void Locate_NoneInRange_ReturnsEmpty()
    {
        var locator = new FixedLocator([20, 30, 40]);
        double[] ticks = locator.Locate(0, 10);
        Assert.Empty(ticks);
    }
}
