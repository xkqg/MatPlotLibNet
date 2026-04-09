// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="EcdfSeries"/> construction, computation, serialization, and rendering.</summary>
public class EcdfSeriesTests
{
    /// <summary>Verifies that the constructor stores the raw data.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        double[] data = [3.0, 1.0, 2.0];
        var series = new EcdfSeries(data);
        Assert.Equal(data, series.Data);
    }

    /// <summary>Verifies that SortedX contains values in ascending order.</summary>
    [Fact]
    public void SortedX_IsSorted()
    {
        var series = new EcdfSeries([5.0, 1.0, 3.0, 2.0, 4.0]);
        Assert.Equal([1.0, 2.0, 3.0, 4.0, 5.0], series.SortedX);
    }

    /// <summary>Verifies that the last CDF value equals 1.0.</summary>
    [Fact]
    public void CdfY_EndsAtOne()
    {
        var series = new EcdfSeries([10.0, 20.0, 30.0]);
        Assert.Equal(1.0, series.CdfY[^1]);
    }

    /// <summary>Verifies that CdfY has the same length as SortedX.</summary>
    [Fact]
    public void CdfY_HasCorrectLength()
    {
        var series = new EcdfSeries([1.0, 2.0, 3.0, 4.0]);
        Assert.Equal(series.SortedX.Length, series.CdfY.Length);
    }

    /// <summary>Verifies that the Y data range is always 0 to 1.</summary>
    [Fact]
    public void ComputeDataRange_YIsZeroToOne()
    {
        var series = new EcdfSeries([100.0, 200.0, 300.0]);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0.0, range.YMin);
        Assert.Equal(1.0, range.YMax);
    }

    /// <summary>Verifies that the DTO type is "ecdf".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsEcdf()
    {
        var series = new EcdfSeries([1.0, 2.0]);
        var dto = series.ToSeriesDto();
        Assert.Equal("ecdf", dto.Type);
    }

    /// <summary>Verifies that JSON round-trip preserves the data.</summary>
    [Fact]
    public void RoundTrip_PreservesData()
    {
        double[] data = [3.0, 1.0, 4.0, 1.0, 5.0];
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        axes.Ecdf(data);

        var serializer = new ChartSerializer();
        var json = serializer.ToJson(fig);
        var restored = serializer.FromJson(json);

        var restoredSeries = restored.SubPlots[0].Series.OfType<EcdfSeries>().Single();
        Assert.Equal(data, restoredSeries.Data);
    }

    /// <summary>Verifies that rendering produces SVG output containing a polyline.</summary>
    [Fact]
    public void Render_ProducesSvg()
    {
        var svg = Plt.Create()
            .Ecdf([1.0, 2.0, 3.0, 4.0, 5.0])
            .ToSvg();

        Assert.Contains("polyline", svg);
    }
}
