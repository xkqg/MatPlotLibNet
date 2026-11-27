// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Indicators;

/// <summary>Verifies <see cref="Macd"/> behavior.</summary>
public class MacdTests
{
    private static readonly double[] Prices = [26.0, 26.5, 26.3, 26.8, 27.0, 27.2, 27.5, 27.3, 27.8, 28.0,
        28.2, 28.5, 28.3, 28.8, 29.0, 28.7, 29.2, 29.5, 29.3, 29.8, 30.0, 30.2, 30.5, 30.3, 30.8, 31.0];

    /// <summary>A MACD shifted by an offset (half a bar on a panel under a candlestick chart, whose bars stand at the centres of their
    /// slots) shifts its histogram with its lines: each bar of the histogram stands under the signal value it is the difference to.</summary>
    [Fact]
    public void Apply_ShiftsTheHistogramByTheOffset_AsItShiftsTheLines()
    {
        var axes = new Axes();

        new Macd(Prices, 3, 6, 4) { Offset = 0.5 }.Apply(axes);

        Assert.Equal(((LineSeries)axes.Series[1]).XData, ((BarSeries)axes.Series[2]).XCoordinate);
    }
}
