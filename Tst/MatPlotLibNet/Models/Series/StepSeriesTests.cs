// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="StepSeries"/> default properties and construction.</summary>
public class StepSeriesTests
{
    /// <summary>Verifies that the constructor stores X and Y data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] x = [1, 2, 3], y = [4, 5, 6];
        var series = new StepSeries(x, y);
        Assert.Equal(x, series.XData);
        Assert.Equal(y, series.YData);
    }

    /// <summary>Verifies that StepPosition defaults to Post.</summary>
    [Fact]
    public void DefaultStepPosition_IsPost()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Equal(StepPosition.Post, series.StepPosition);
    }

    /// <summary>Verifies that Color defaults to null.</summary>
    [Fact]
    public void DefaultColor_IsNull()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Null(series.Color);
    }

    /// <summary>Verifies that LineStyle defaults to Solid.</summary>
    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Equal(LineStyle.Solid, series.LineStyle);
    }

    /// <summary>Verifies that LineWidth defaults to 1.5.</summary>
    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Equal(1.5, series.LineWidth);
    }

    /// <summary>Verifies that Marker defaults to null.</summary>
    [Fact]
    public void DefaultMarker_IsNull()
    {
        var series = new StepSeries([1.0], [2.0]);
        Assert.Null(series.Marker);
    }
}
