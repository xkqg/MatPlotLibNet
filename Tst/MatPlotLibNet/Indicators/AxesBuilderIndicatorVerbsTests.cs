// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>The indicator verbs of the builder find their prices on the axes, and the entry point applies the indicator the caller made.</summary>
public class AxesBuilderIndicatorVerbsTests
{
    [Fact]
    public void AnIndicatorVerb_WithNoPriceSeriesOnTheAxes_IsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Sma(3)).Build());
    }

    /// <summary>The generic entry point, under the name it shipped with first, applies the indicator the caller made.</summary>
    [Fact]
    public void Indicator_AppliesTheIndicatorTheCallerMade()
    {
        var atr = new Atr(OhlcvBars.Hlc([101, 102, 103, 104, 105, 106, 107], [99, 100, 101, 102, 103, 104, 105], [100, 101, 102, 103, 104, 105, 106]), period: 3);

        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Plot([1.0], [1.0]).Indicator(atr)).Build().SubPlots[0];

        Assert.Equal(2, axes.Series.Count);
    }

    /// <summary>An indicator verb computes over the closes of the last price series, a candlestick's too, even with a series
    /// that holds no prices (a bar chart) added after it.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnIndicatorVerb_ComputesOverTheClosesOfTheLastPriceSeries(bool barAfter)
    {
        double[] closes = [11, 12, 13, 14, 15, 16];

        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            ax.Candlestick([10.0, 11, 12, 13, 14, 15], [12.0, 13, 14, 15, 16, 17], [9.0, 10, 11, 12, 13, 14], closes);
            if (barAfter) ax.Bar(["flag"], [1.0]);
            ax.Sma(3);
        }).Build().SubPlots[0];

        Assert.Equal(15.0, Assert.IsType<LineSeries>(axes.Series[^1]).YData[^1], 1e-12);
    }
}
