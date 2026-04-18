// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="SwarmplotSeries"/> default properties, construction, and serialization.</summary>
public class SwarmplotSeriesTests
{
    [Fact]
    public void Constructor_StoresDatasets()
    {
        double[][] data = [[1.0, 2.0], [3.0, 4.0]];
        var series = new SwarmplotSeries(data);
        Assert.Equal(data, series.Datasets);
    }

    [Fact]
    public void MarkerSize_DefaultsTo5()
    {
        var series = new SwarmplotSeries([]);
        Assert.Equal(5.0, series.MarkerSize);
    }

    [Fact]
    public void Alpha_DefaultsTo0p8()
    {
        var series = new SwarmplotSeries([]);
        Assert.Equal(0.8, series.Alpha);
    }

    [Fact]
    public void ToSeriesDto_ReturnsTypeSwarmplot()
    {
        var series = new SwarmplotSeries([[1.0, 2.0]]);
        Assert.Equal("swarmplot", series.ToSeriesDto().Type);
    }

}
