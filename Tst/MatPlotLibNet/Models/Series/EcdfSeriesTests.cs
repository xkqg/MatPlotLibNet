// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="EcdfSeries"/> construction, computation, serialization, and rendering.</summary>
public class EcdfSeriesTests
{
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

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        //  Verifies that rendering produces SVG output containing a polyline.
        new DrawnToken("Render_ProducesSvg", () => Plt.Create()
            .Ecdf([1.0, 2.0, 3.0, 4.0, 5.0])
            .ToSvg(), "polyline"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();
}
