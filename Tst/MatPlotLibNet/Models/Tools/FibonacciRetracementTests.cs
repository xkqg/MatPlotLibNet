// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Tools;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Tools;

/// <summary>Verifies <see cref="FibonacciRetracement"/> behavior.</summary>
public class FibonacciRetracementTests
{
    [Fact]
    public void Levels_HasSevenStandardRatios()
    {
        var fib = new FibonacciRetracement(200.0, 100.0);
        Assert.Equal(7, fib.Levels.Count);
    }

    /// <summary>A level is the high less its ratio of the range: 0% is the high (top of the range), 23.6% is 200 - 0.236 * 100 = 176.4, 50% is
    /// 150, and 100% is the low (bottom of the range).</summary>
    [Theory]
    [InlineData(0, 0.0, 200.0)]
    [InlineData(1, 0.236, 176.4)]
    [InlineData(2, 0.382, 161.8)]
    [InlineData(3, 0.5, 150.0)]
    [InlineData(4, 0.618, 138.2)]
    [InlineData(5, 0.786, 121.4)]
    [InlineData(6, 1.0, 100.0)]
    public void Levels_ComputedFromHighMinusLow(int index, double ratio, double price)
    {
        var fib = new FibonacciRetracement(priceHigh: 200.0, priceLow: 100.0);
        Assert.Equal(price, fib.Levels[index].Price, precision: 4);
        Assert.Equal(ratio, fib.Levels[index].Ratio, precision: 6);
    }

    [Fact]
    public void Axes_AddFibonacci_AppendsToCollection()
    {
        var axes = new Axes();
        var fib = axes.AddFibonacci(priceHigh: 200.0, priceLow: 100.0);

        Assert.Single(axes.FibonacciRetracements);
        Assert.Same(fib, axes.FibonacciRetracements[0]);
    }

    [Fact]
    public void Axes_AddFibonacci_MultipleFibsAllStored()
    {
        var axes = new Axes();
        axes.AddFibonacci(200.0, 100.0);
        axes.AddFibonacci(500.0, 300.0);

        Assert.Equal(2, axes.FibonacciRetracements.Count);
    }
}
