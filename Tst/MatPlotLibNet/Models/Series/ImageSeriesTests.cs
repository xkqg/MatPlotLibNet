// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ImageSeries"/> construction, data range, serialization, and rendering.</summary>
public class ImageSeriesTests
{
    private static readonly double[,] SampleData = { { 1, 2, 3 }, { 4, 5, 6 } };

    /// <summary>Verifies that the constructor stores the 2D data array.</summary>
    [Fact]
    public void Constructor_StoresData()
    {
        var series = new ImageSeries(SampleData);
        Assert.Equal(SampleData, series.Data);
    }

    /// <summary>Verifies that ComputeDataRange returns pixel-coordinate bounds (0..cols, 0..rows).</summary>
    [Fact]
    public void ComputeDataRange_ReturnsPixelBounds()
    {
        var series = new ImageSeries(SampleData); // 2 rows, 3 cols
        var range = series.ComputeDataRange(null!);
        Assert.Equal(0.0, range.XMin);
        Assert.Equal(3.0, range.XMax); // cols
        Assert.Equal(0.0, range.YMin);
        Assert.Equal(2.0, range.YMax); // rows
    }

    /// <summary>Verifies that the DTO type is "image".</summary>
    [Fact]
    public void ToSeriesDto_TypeIsImage()
    {
        var series = new ImageSeries(SampleData);
        var dto = series.ToSeriesDto();
        Assert.Equal("image", dto.Type);
    }

    /// <summary>Verifies that explicit VMin/VMax override auto-detection in GetColorBarRange.</summary>
    [Fact]
    public void VMinVMax_OverridesAutoRange()
    {
        var series = new ImageSeries(SampleData) { VMin = -10, VMax = 10 };
        var (min, max) = series.GetColorBarRange();
        Assert.Equal(-10.0, min);
        Assert.Equal(10.0, max);
    }

    /// <summary>Verifies that GetColorBarRange respects VMin/VMax when set, and auto-detects otherwise.</summary>
    [Fact]
    public void GetColorBarRange_RespectsVMinVMax()
    {
        // Auto-detect: data ranges from 1 to 6
        var autoSeries = new ImageSeries(SampleData);
        var (autoMin, autoMax) = autoSeries.GetColorBarRange();
        Assert.Equal(1.0, autoMin);
        Assert.Equal(6.0, autoMax);

        // Partial override: only VMin set
        var partialSeries = new ImageSeries(SampleData) { VMin = 0 };
        var (partialMin, partialMax) = partialSeries.GetColorBarRange();
        Assert.Equal(0.0, partialMin);
        Assert.Equal(6.0, partialMax);
    }

    /// <summary>Verifies that JSON round-trip preserves the data, VMin, and VMax.</summary>
    [Fact]
    public void RoundTrip_PreservesData()
    {
        var fig = new Figure();
        var axes = fig.AddSubPlot();
        var img = axes.Image(SampleData);
        img.VMin = -1;
        img.VMax = 10;

        var serializer = new ChartSerializer();
        var json = serializer.ToJson(fig);
        var restored = serializer.FromJson(json);

        var restoredSeries = restored.SubPlots[0].Series.OfType<ImageSeries>().Single();
        Assert.Equal(2, restoredSeries.Data.GetLength(0));
        Assert.Equal(3, restoredSeries.Data.GetLength(1));
        Assert.Equal(5.0, restoredSeries.Data[1, 1]);
        Assert.Equal(-1.0, restoredSeries.VMin);
        Assert.Equal(10.0, restoredSeries.VMax);
    }

    /// <summary>Verifies that rendering produces SVG output containing rect elements.</summary>
    [Fact]
    public void Render_ProducesSvg()
    {
        var svg = Plt.Create()
            .Image(SampleData)
            .ToSvg();

        Assert.Contains("rect", svg);
    }
}
