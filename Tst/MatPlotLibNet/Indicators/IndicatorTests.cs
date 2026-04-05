// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

public class SmaTests
{
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Sma(prices, 3).Compute();
        Assert.Equal(3, result.Length); // 5 - 3 + 1
    }

    [Fact]
    public void Compute_ReturnsCorrectValues()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Sma(prices, 3).Compute();
        Assert.Equal(20, result[0]); // (10+20+30)/3
        Assert.Equal(30, result[1]); // (20+30+40)/3
        Assert.Equal(40, result[2]); // (30+40+50)/3
    }

    [Fact]
    public void Apply_AddsLineSeriesToAxes()
    {
        var axes = new Axes();
        var indicator = new Sma([10, 20, 30, 40, 50], 3);
        indicator.Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }

    [Fact]
    public void Apply_SetsLabel()
    {
        var axes = new Axes();
        new Sma([10, 20, 30, 40, 50], 3).Apply(axes);
        Assert.Equal("SMA(3)", axes.Series[0].Label);
    }

    [Fact]
    public void Apply_RespectsCustomColor()
    {
        var axes = new Axes();
        new Sma([10, 20, 30, 40, 50], 3) { Color = Color.Red }.Apply(axes);
        Assert.Equal(Color.Red, ((LineSeries)axes.Series[0]).Color);
    }

    [Fact]
    public void ImplementsIIndicator()
    {
        Assert.IsAssignableFrom<IIndicator>(new Sma([1.0], 1));
    }

    [Fact]
    public void ExtendsIndicatorBase()
    {
        Assert.IsAssignableFrom<Indicator>(new Sma([1.0], 1));
    }
}

public class EmaTests
{
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Ema(prices, 3).Compute();
        Assert.Equal(5, result.Length);
    }

    [Fact]
    public void Compute_FirstValueIsSma()
    {
        double[] prices = [10, 20, 30, 40, 50];
        double[] result = new Ema(prices, 3).Compute();
        Assert.Equal(20, result[2]); // SMA of first 3: (10+20+30)/3
    }

    [Fact]
    public void Apply_AddsLineSeriesToAxes()
    {
        var axes = new Axes();
        new Ema([10, 20, 30, 40, 50], 3).Apply(axes);
        Assert.Single(axes.Series);
        Assert.IsType<LineSeries>(axes.Series[0]);
    }
}

public class BollingerBandsTests
{
    [Fact]
    public void Apply_AddsThreeSeriesToAxes()
    {
        var axes = new Axes();
        double[] prices = [10, 20, 30, 25, 35, 40, 30, 45, 50, 55];
        new BollingerBands(prices, 5).Apply(axes);
        // AreaSeries (band fill) + LineSeries (middle SMA)
        Assert.Equal(2, axes.Series.Count);
    }

    [Fact]
    public void Apply_SetsLabel()
    {
        var axes = new Axes();
        double[] prices = [10, 20, 30, 25, 35, 40, 30, 45, 50, 55];
        new BollingerBands(prices, 5).Apply(axes);
        Assert.Contains("BB(5", axes.Series[1].Label!);
    }
}

public class VwapTests
{
    [Fact]
    public void Compute_ReturnsCorrectLength()
    {
        double[] prices = [100, 102, 101, 103];
        double[] volumes = [1000, 1500, 1200, 1800];
        double[] result = new Vwap(prices, volumes).Compute();
        Assert.Equal(4, result.Length);
    }

    [Fact]
    public void Apply_AddsLineSeriesToAxes()
    {
        var axes = new Axes();
        new Vwap([100, 102, 101], [1000, 1500, 1200]).Apply(axes);
        Assert.Single(axes.Series);
    }
}

public class FibonacciRetracementTests
{
    [Fact]
    public void Apply_AddsFiveReferenceLines()
    {
        var axes = new Axes();
        axes.Plot([0, 1, 2], [100, 200, 150]); // need a series for axes to render
        new FibonacciRetracement(100, 200).Apply(axes);
        Assert.Equal(5, axes.ReferenceLines.Count); // 23.6%, 38.2%, 50%, 61.8%, 78.6%
    }

    [Fact]
    public void Apply_LinesAreWithinRange()
    {
        var axes = new Axes();
        axes.Plot([0, 1], [100, 200]);
        new FibonacciRetracement(100, 200).Apply(axes);
        foreach (var line in axes.ReferenceLines)
        {
            Assert.InRange(line.Value, 100, 200);
        }
    }
}

public class AddIndicatorBuilderTests
{
    [Fact]
    public void AddIndicator_FluentChaining()
    {
        var figure = Plt.Create()
            .AddSubPlot(1, 1, 1, ax => ax
                .Plot([1.0, 2.0, 3.0, 4.0, 5.0], [10, 20, 30, 40, 50])
                .AddIndicator(new Sma([10, 20, 30, 40, 50], 3)))
            .Build();

        Assert.Equal(2, figure.SubPlots[0].Series.Count); // original + SMA
    }
}
