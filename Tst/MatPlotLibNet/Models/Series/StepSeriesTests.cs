// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

public class StepSeriesTests
{
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2, 3], y = [4, 5, 6];
        var series = new StepSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    [Fact]
    public void DefaultStepPosition_IsPost()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Equal(StepPosition.Post, series.StepPosition);
    }

    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Null(series.Color);
    }

    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Equal(LineStyle.Solid, series.LineStyle);
    }

    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Equal(1.5, series.LineWidth);
    }

    [Fact]
    public void DefaultMarker_IsNull()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Null(series.Marker);
    }

    [Fact]
    public void Accept_CallsCorrectVisitorMethod()
    {
        var series = new StepSeries([1.0], [2.0]);
        var visitor = new TestSeriesVisitor();
        series.Accept(visitor, null!);
        Assert.Equal(nameof(StepSeries), visitor.LastVisited);
    }
}
