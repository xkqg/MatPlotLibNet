// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

public class CandlestickSeriesTests
{
    [Fact]
    public void Constructor_StoresOhlcData()
    {
        double[] o = [10, 12], h = [15, 14], l = [8, 10], c = [13, 11];
        var series = new CandlestickSeries(o, h, l, c);
        Assert.Equal(o, series.Open);
        Assert.Equal(h, series.High);
        Assert.Equal(l, series.Low);
        Assert.Equal(c, series.Close);
    }

    [Fact]
    public void DefaultUpColor_IsGreen()
    {
        var series = new CandlestickSeries([10], [15], [8], [13]);
        Assert.Equal(Color.Green, series.UpColor);
    }

    [Fact]
    public void DefaultDownColor_IsRed()
    {
        var series = new CandlestickSeries([10], [15], [8], [13]);
        Assert.Equal(Color.Red, series.DownColor);
    }

    [Fact]
    public void DefaultBodyWidth_Is0Point6()
    {
        var series = new CandlestickSeries([10], [15], [8], [13]);
        Assert.Equal(0.6, series.BodyWidth);
    }

    [Fact]
    public void DateLabels_DefaultsToNull()
    {
        var series = new CandlestickSeries([10], [15], [8], [13]);
        Assert.Null(series.DateLabels);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new CandlestickSeries([10], [15], [8], [13]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(CandlestickSeries), visitor.LastVisited);
    }
}
