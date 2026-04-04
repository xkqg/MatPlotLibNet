// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

public class StemSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1.0, 2.0], y = [3.0, 4.0];
        var series = new StemSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    [Fact]
    public void DefaultMarker_IsCircle()
    {
        var series = new StemSeries([1.0], [2.0]);
        Assert.Equal(MarkerStyle.Circle, series.Marker);
    }

    [Fact]
    public void DefaultMarkerColor_IsNull()
    {
        var series = new StemSeries([1.0], [2.0]);
        Assert.Null(series.MarkerColor);
    }

    [Fact]
    public void DefaultStemColor_IsNull()
    {
        var series = new StemSeries([1.0], [2.0]);
        Assert.Null(series.StemColor);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new StemSeries([1.0], [2.0]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(StemSeries), visitor.LastVisited);
    }
}
