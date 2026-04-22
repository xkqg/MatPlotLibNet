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
    public void FibonacciRetracement_StoresPrices()
    {
        var fib = new FibonacciRetracement(priceHigh: 200.0, priceLow: 100.0);
        Assert.Equal(200.0, fib.PriceHigh);
        Assert.Equal(100.0, fib.PriceLow);
    }

    [Fact]
    public void DefaultLineWidth_Is1()
    {
        var fib = new FibonacciRetracement(200.0, 100.0);
        Assert.Equal(1.0, fib.LineWidth);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var fib = new FibonacciRetracement(200.0, 100.0);
        Assert.Null(fib.Color);
    }

    [Fact]
    public void DefaultShowLabels_IsTrue()
    {
        var fib = new FibonacciRetracement(200.0, 100.0);
        Assert.True(fib.ShowLabels);
    }

    [Fact]
    public void Levels_HasSevenStandardRatios()
    {
        var fib = new FibonacciRetracement(200.0, 100.0);
        Assert.Equal(7, fib.Levels.Count);
    }

    [Fact]
    public void Levels_ComputedFromHighMinusLow_At0Pct()
    {
        var fib = new FibonacciRetracement(priceHigh: 200.0, priceLow: 100.0);
        // 0% level = priceHigh (top of range)
        Assert.Equal(200.0, fib.Levels[0].Price, precision: 6);
        Assert.Equal(0.0, fib.Levels[0].Ratio, precision: 6);
    }

    [Fact]
    public void Levels_ComputedFromHighMinusLow_At236Pct()
    {
        var fib = new FibonacciRetracement(priceHigh: 200.0, priceLow: 100.0);
        // 23.6% → 200 − 0.236*100 = 176.4
        Assert.Equal(176.4, fib.Levels[1].Price, precision: 4);
        Assert.Equal(0.236, fib.Levels[1].Ratio, precision: 6);
    }

    [Fact]
    public void Levels_ComputedFromHighMinusLow_At382Pct()
    {
        var fib = new FibonacciRetracement(priceHigh: 200.0, priceLow: 100.0);
        // 38.2% → 200 − 0.382*100 = 161.8
        Assert.Equal(161.8, fib.Levels[2].Price, precision: 4);
    }

    [Fact]
    public void Levels_ComputedFromHighMinusLow_At50Pct()
    {
        var fib = new FibonacciRetracement(priceHigh: 200.0, priceLow: 100.0);
        // 50% → 200 − 0.5*100 = 150
        Assert.Equal(150.0, fib.Levels[3].Price, precision: 6);
    }

    [Fact]
    public void Levels_ComputedFromHighMinusLow_At618Pct()
    {
        var fib = new FibonacciRetracement(priceHigh: 200.0, priceLow: 100.0);
        // 61.8% → 200 − 0.618*100 = 138.2
        Assert.Equal(138.2, fib.Levels[4].Price, precision: 4);
    }

    [Fact]
    public void Levels_ComputedFromHighMinusLow_At786Pct()
    {
        var fib = new FibonacciRetracement(priceHigh: 200.0, priceLow: 100.0);
        // 78.6% → 200 − 0.786*100 = 121.4
        Assert.Equal(121.4, fib.Levels[5].Price, precision: 4);
    }

    [Fact]
    public void Levels_ComputedFromHighMinusLow_At100Pct()
    {
        var fib = new FibonacciRetracement(priceHigh: 200.0, priceLow: 100.0);
        // 100% level = priceLow (bottom of range)
        Assert.Equal(100.0, fib.Levels[6].Price, precision: 6);
        Assert.Equal(1.0, fib.Levels[6].Ratio, precision: 6);
    }

    [Fact]
    public void Properties_AreMutable()
    {
        var fib = new FibonacciRetracement(200.0, 100.0);
        fib.Color = Color.FromHex("#FFD700");
        fib.LineWidth = 1.5;
        fib.ShowLabels = false;

        Assert.Equal(Color.FromHex("#FFD700"), fib.Color);
        Assert.Equal(1.5, fib.LineWidth);
        Assert.False(fib.ShowLabels);
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
