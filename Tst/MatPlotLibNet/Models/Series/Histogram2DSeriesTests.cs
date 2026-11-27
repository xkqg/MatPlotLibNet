// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Indicators;
using MatPlotLibNet.Interaction;
using MatPlotLibNet.Models.Streaming;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Rendering.TickLocators;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Styling.ColorMaps;
using MatPlotLibNet.Styling;
using MatPlotLibNet;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>Verifies <see cref="Histogram2DSeries"/> construction, data range, serialization, and rendering.</summary>
public class Histogram2DSeriesTests
{
    private static readonly double[] SampleX = [1.0, 2.0, 3.0, 4.0, 5.0, 1.5, 2.5, 3.5, 4.5, 2.0];
    private static readonly double[] SampleY = [10.0, 20.0, 30.0, 40.0, 50.0, 15.0, 25.0, 35.0, 45.0, 20.0];

    /// <summary>Verifies that ComputeDataRange returns the min/max of the input X and Y data.</summary>
    [Fact]
    public void ComputeDataRange_MatchesInputRange()
    {
        var series = new Histogram2DSeries(SampleX, SampleY);
        var range = series.ComputeDataRange(null!);
        Assert.Equal(1.0, range.XMin);
        Assert.Equal(5.0, range.XMax);
        Assert.Equal(10.0, range.YMin);
        Assert.Equal(50.0, range.YMax);
    }

    /// <summary>Verifies that the default number of bins is 20 for both axes.</summary>
    [Fact]
    public void ABinCountLeftOut_IsTwentyAlongEachAxis()
    {
        var series = new Histogram2DSeries(SampleX, SampleY);
        Assert.Equal(20, series.BinsX);
        Assert.Equal(20, series.BinsY);
    }

    /// <summary>Verifies that GetColorBarRange returns non-negative values.</summary>
    [Fact]
    public void GetColorBarRange_ReturnsNonNegative()
    {
        var series = new Histogram2DSeries(SampleX, SampleY, 5, 5);
        var (min, max) = series.GetColorBarRange();
        Assert.True(min >= 0);
        Assert.True(max >= min);
    }

    // ---- a histogram has bins ---------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(5, 0)]
    [InlineData(5, -1)]
    public void ANumberOfBinsBelowOne_IsRefused_WhenTheSeriesIsBuilt(int binsX, int binsY)
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Histogram2DSeries(SampleX, SampleY, binsX, binsY));

        Assert.Contains("of a 2-D histogram along the", ex.Message);
        Assert.Contains("must be >= 1", ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ANumberOfBinsBelowOne_IsRefused_ByTheVerb(int bins) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Axes().Histogram2D(SampleX, SampleY, bins));

    [Fact]
    public void ANumberOfBinsBelowOne_IsRefused_WhenItIsSet_AndTheSeriesKeepsItsBins()
    {
        var series = new Histogram2DSeries(SampleX, SampleY, 7, 9);

        Assert.Throws<ArgumentOutOfRangeException>(() => series.BinsX = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.BinsY = -3);

        Assert.Equal(7, series.BinsX);
        Assert.Equal(9, series.BinsY);
    }

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("OneBinAlongEachAxis_IsTheSmallestHistogram_AndDraws", () => Plt.Create().Histogram2D(SampleX, SampleY, 1).ToSvg(), "rect"),
        //  Verifies that rendering produces SVG output containing rect elements.
        new DrawnToken("Render_ProducesSvg", () => Plt.Create()
            .Histogram2D(SampleX, SampleY, 5)
            .ToSvg(), "rect"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Theory]
    [InlineData("bins")]
    [InlineData("binsY")]
    public void ADocumentWithNoBinsInOneAxis_IsRefusedWhenItIsRead(string member)
    {
        string json = $$"""{"subPlots":[{"series":[{"type":"histogram2d","xData":[1,2],"yData":[1,2],"bins":4,"binsY":4}]}]}"""
            .Replace($"\"{member}\":4", $"\"{member}\":0");

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new ChartSerializer().FromJson(json));

        Assert.Contains("of a 2-D histogram along the", ex.Message);
        Assert.Contains("must be >= 1", ex.Message);
    }

    /// <summary>A side of one value is binned as <c>numpy.histogram2d</c> bins it, over the value and half a unit either side:
    /// <c>histogram2d([3, 3, 3], [1, 2, 3], bins=20)</c> (numpy 2.5.3) counts X bin 10 at Y bins 0, 10 and 19, and the same the other
    /// way round.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Histogram2DSeries_ASideOfOneValue_IsCountedInTheMiddleBin(bool flatX)
    {
        double[] flat = [3.0, 3, 3], spread = [1.0, 2, 3];
        var counts = (flatX ? new Histogram2DSeries(flat, spread) : new Histogram2DSeries(spread, flat)).ComputeBinCounts();

        foreach (int across in new[] { 0, 10, 19 })
            Assert.Equal(1, flatX ? counts[across, 10] : counts[10, across]);
    }

    [Fact]
    public void Histogram2DSeries_ComputeBinCounts_EmptyData_ReturnsAllZeros()
    {
        var s = new Histogram2DSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var counts = s.ComputeBinCounts();
        Assert.Equal(0, counts.Cast<int>().Sum());
    }

    [Fact]
    public void Histogram2DSeries_ComputeDataRange_EmptyData_ReturnsNullRange()
    {
        var s = new Histogram2DSeries((Vec)Array.Empty<double>(), (Vec)Array.Empty<double>());
        var dr = s.ComputeDataRange(null!);
        Assert.Null(dr.XMin);
    }

    /// <summary>Histogram2DSeries.GetColorBarRange: with a single bin (BinsX=1, BinsY=1) every data point falls into the same
    /// cell, so the counts have no width and the range is opened one wide from the count, as every colour range of no width is under
    /// every theme (<c>ColorMapping.Opened</c>).</summary>
    [Fact]
    public void Histogram2DSeries_GetColorBarRange_SingleBin_MinEqualsMax_IsOpenedOneWide()
    {
        var s = new Histogram2DSeries([1.0, 2.0, 3.0], [1.0, 2.0, 3.0], binsX: 1, binsY: 1);
        var range = s.GetColorBarRange();
        Assert.Equal(3, range.Min);
        Assert.Equal(4, range.Max);
    }

    /// <summary>Histogram2DSeries.GetColorBarRange: `X.Length == 0 || Y.Length == 0`
    /// short-circuit — first OR arm.</summary>
    [Fact]
    public void Histogram2D_EmptyX_ReturnsSentinelColorBarRange()
    {
        var s = new Histogram2DSeries(Array.Empty<double>(), Array.Empty<double>(), binsX: 5, binsY: 5);
        Assert.Equal(new MatPlotLibNet.Numerics.MinMaxRange(0.0, 1.0), s.GetColorBarRange());
    }
}
