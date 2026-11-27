// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.Svg;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

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

    /// <summary>Verifies that all ticks are strictly positive (log requires positive values).</summary>
    [Fact]
    public void Locate_AllTicksStrictlyPositive()
    {
        var locator = new LogLocator();
        double[] ticks = locator.Locate(0.001, 10);
        Assert.All(ticks, t => Assert.True(t > 0));
    }

    /// <summary>min ≤ 0 coercion arm.</summary>
    [Fact]
    public void LogLocator_MinLessThanZero_CoercesToTinyPositive()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(-5, 100);
        Assert.NotEmpty(ticks);
        Assert.All(ticks, t => Assert.True(t > 0));
    }

    /// <summary>max ≤ min returns [min] short-circuit.</summary>
    [Fact]
    public void LogLocator_MaxLessThanMin_ReturnsSingleMin()
    {
        var loc = new LogLocator();
        var ticks = loc.Locate(10, 5);
        Assert.Single(ticks);
        Assert.Equal(10, ticks[0]);
    }
}
