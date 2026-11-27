// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="ImageSeries"/> construction, data range, serialization, and rendering.</summary>
public class ImageSeriesTests
{
    private static readonly double[,] SampleData = { { 1, 2, 3 }, { 4, 5, 6 } };

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

    /// <summary>An image of one value has a range of no width: under the library's own themes its colour bar runs from the value to one
    /// above it, the range its cells are coloured over.</summary>
    [Fact]
    public void GetColorBarRange_OfAConstantImage_RunsFromTheValueToOneAboveIt() =>
        Assert.Equal(new(5, 6), new ImageSeries(new double[,] { { 5, 5 } }).GetColorBarRange());

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Verifies that rendering produces SVG output containing rect elements.
        new DrawnToken("Render_ProducesSvg", () => Plt.Create()
            .Image(SampleData)
            .ToSvg(), "rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
