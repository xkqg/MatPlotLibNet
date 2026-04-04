// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

public class ScatterSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2], y = [3, 4];
        var series = new ScatterSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    [Fact]
    public void DefaultMarker_IsCircle()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Equal(MarkerStyle.Circle, series.Marker);
    }

    [Fact]
    public void DefaultAlpha_IsOne()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Equal(1.0, series.Alpha);
    }

    [Fact]
    public void Sizes_DefaultNull()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        Assert.Null(series.Sizes);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new ScatterSeries([1.0], [2.0]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(ScatterSeries), visitor.LastVisited);
    }
}
