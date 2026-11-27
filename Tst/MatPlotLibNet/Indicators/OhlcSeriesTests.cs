// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Indicators;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Indicators;

public class OhlcSeriesTests
{
    private static readonly double[] O = [10, 11, 12, 13];
    private static readonly double[] H = [15, 16, 17, 18];
    private static readonly double[] L = [ 8,  9, 10, 11];
    private static readonly double[] C = [12, 13, 14, 15];

    [Fact]
    public void PriceData_ReturnsClose()
    {
        IPriceSeries cs = new CandlestickSeries(O, H, L, C);
        IPriceSeries obs = new OhlcBarSeries(O, H, L, C);
        Assert.Same(C, cs.PriceData);
        Assert.Same(C, obs.PriceData);
    }
}
