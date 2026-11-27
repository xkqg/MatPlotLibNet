// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Tests.Models.Series.Polar;

/// <summary>Verifies the <see cref="PolarHeatmapSeries"/> model.</summary>
public class PolarHeatmapSeriesTests
{
    private static double[,] MakeData(int thetaBins = 4, int rBins = 3)
    {
        var d = new double[thetaBins, rBins];
        for (int t = 0; t < thetaBins; t++)
            for (int r = 0; r < rBins; r++)
                d[t, r] = t * rBins + r;
        return d;
    }

    // ---- GetColorBarRange ---------------------------------------------------

    [Fact]
    public void GetColorBarRange_ReturnsMinAndMax()
    {
        var s = new PolarHeatmapSeries(MakeData(4, 3), 4, 3);
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(0.0, min);
        Assert.Equal(11.0, max); // 4*3 - 1 = 11
    }

    [Fact]
    public void GetColorBarRange_ConstantData_RunsFromTheValueToOneAboveIt()
    {
        var data = new double[2, 2];
        data[0, 0] = data[0, 1] = data[1, 0] = data[1, 1] = 5.0;
        var s = new PolarHeatmapSeries(data, 2, 2);
        var (min, max) = s.GetColorBarRange();
        Assert.Equal(5.0, min);
        Assert.Equal(6.0, max);
    }

    // ---- RadialExtent -------------------------------------------------------

    [Fact]
    public void RadialExtent_RunsFromTheCentreToTheRMaxTheCellsEndAt_ExactlyAndWithNoMargin()
    {
        var s = new PolarHeatmapSeries(MakeData(), 4, 3) { RMax = 5 };

        Assert.Equal(new RadialExtent(0, 5, Sticky: true), s.RadialExtent);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RadialExtent_IsNone_WhenRMaxCannotBeDrawn(double rMax)
    {
        var s = new PolarHeatmapSeries(MakeData(), 4, 3) { RMax = rMax };

        Assert.Null(s.RadialExtent);
    }

    // ---- DataRange ----------------------------------------------------------

    [Fact]
    public void ComputeDataRange_ReturnsNullContribution()
    {
        var s = new PolarHeatmapSeries(MakeData(), 4, 3);
        var contribution = s.ComputeDataRange(null!);
        Assert.Null(contribution.XMin);
        Assert.Null(contribution.XMax);
        Assert.Null(contribution.YMin);
        Assert.Null(contribution.YMax);
    }

    // ---- ToSeriesDto --------------------------------------------------------

    [Fact]
    public void ToSeriesDto_SetsColorMapName_WhenSet()
    {
        var s = new PolarHeatmapSeries(MakeData(), 4, 3)
        {
            ColorMap = ColorMapRegistry.Get("viridis")
        };
        var dto = s.ToSeriesDto();
        Assert.Equal("viridis", dto.ColorMapName);
    }
}
