// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="PolarLineSeries"/> default properties and construction.</summary>
public class PolarLineSeriesTests
{
    /// <summary>Verifies that the constructor stores R and Theta data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] r = [1, 2, 3], theta = [0, 1, 2];
        var series = new PolarLineSeries(r, theta);
        Assert.Equal(r, series.R);
        Assert.Equal(theta, series.Theta);
    }

    /// <summary>Verifies that LineStyle defaults to Solid.</summary>
    [Fact]
    public void DefaultLineStyle_IsSolid()
    {
        var series = new PolarLineSeries([1.0], [0.0]);
        Assert.Equal(LineStyle.Solid, series.LineStyle);
    }

    /// <summary>Verifies that LineWidth defaults to 1.5.</summary>
    [Fact]
    public void DefaultLineWidth_Is1Point5()
    {
        var series = new PolarLineSeries([1.0], [0.0]);
        Assert.Equal(1.5, series.LineWidth);
    }
}
