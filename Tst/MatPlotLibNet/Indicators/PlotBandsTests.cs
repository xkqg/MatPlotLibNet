// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

public class PlotBandsTests
{
    [Fact]
    public void BollingerBands_PlotBands_SetsAlpha()
    {
        double[] prices = Enumerable.Range(0, 30).Select(i => 100.0 + i % 10).ToArray();
        var bb = new BollingerBands(prices, 10);
        bb.Alpha = 0.3;
        var axes = new Axes();
        bb.Apply(axes);
        var fill = axes.Series.OfType<AreaSeries>().First();
        Assert.Equal(0.3, fill.Alpha);
    }
}
