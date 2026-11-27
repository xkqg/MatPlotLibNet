// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Models.Series;

/// <summary>A word cloud shows how much each word weighs by how large it is set. It fills its plot without axes around it. Its
/// table gives what the picture can only suggest: each word with its weight and its size, the heaviest first, as the words are
/// read in the SVG. What cannot be drawn (a weight that is not above zero, an empty or repeated word) is refused where the series
/// is built.</summary>
public class WordCloudSeriesTests
{
    private static readonly string[] Words = ["data", "chart", "plot", "axis"];
    private static readonly double[] Weights = [100, 25, 4, 25];

    [Fact]
    public void TheVerb_AddsTheSeries_AndLeavesTheAxesCartesianWithoutAxesAroundIt()
    {
        var axes = new Axes();

        var series = axes.WordCloud(Words, Weights);

        Assert.Same(series, Assert.Single(axes.Series));
        Assert.Equal(CoordinateSystem.Cartesian, axes.CoordinateSystem);
        Assert.True(series.DrawsWithoutAxes);
        Assert.Equal(1, series.ColorSlotSpan);
        Assert.Equal(Words, series.Words);
        Assert.Equal(Weights, series.Weights);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-3.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void AWeightThatIsNotAPositiveNumber_IsRefusedByItsWord(double weight)
    {
        var refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Axes().WordCloud(["fine", "broken"], [2, weight]));

        Assert.Contains("broken", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyWord_AndARepeatedWord_AndALengthThatDiffers_AreRefused()
    {
        Assert.Throws<ArgumentException>(() => new Axes().WordCloud(["a", ""], [1, 2]));
        Assert.Throws<ArgumentException>(() => new Axes().WordCloud(["a", "a"], [1, 2]));
        Assert.Throws<ArgumentException>(() => new Axes().WordCloud(["a", "b"], [1]));
    }

    [Fact]
    public void NoWords_IsAnEmptyCloud()
    {
        var series = new Axes().WordCloud([], []);

        Assert.Empty(series.ToDataTable()!.Rows);
    }

    [Fact]
    public void TheTable_HasEachWordWithItsWeightAndSize_TheHeaviestFirst_TiesInOrdinalOrder()
    {
        var table = new Axes().WordCloud(Words, Weights).ToDataTable();

        Assert.NotNull(table);
        Assert.Equal(["word", "weight", "size"], table!.Columns.Select(c => c.Header));
        // data (100), then chart and axis (25 each; "axis" is before "chart" in ordinal order), then plot (4).
        Assert.Equal(["data", "axis", "chart", "plot"], table.Rows.Select(r => r[0].Text));
        Assert.Equal([100.0, 25.0, 25.0, 4.0], table.Rows.Select(r => r[1].Number));
        // The size is the one the weight gives over the range (W1): 60 for the heaviest, 12 for the lightest.
        Assert.Equal(60.0, table.Rows[0][2].Number, 1e-9);
        Assert.Equal(12.0, table.Rows[3][2].Number, 1e-9);
        Assert.Equal(table.Rows[1][2].Number, table.Rows[2][2].Number);
    }

    [Fact]
    public void ARangeIsContributedToNoAxis()
    {
        var contribution = new Axes().WordCloud(Words, Weights).ComputeDataRange(new TestAxesContext());

        Assert.Equal(new DataRangeContribution(null, null, null, null), contribution);
    }

    // ── what can be set ─────────────────────────────────────────────────────────

    [Fact]
    public void TheSizes_AreARangeAboveZero_SetTogether()
    {
        var series = new Axes().WordCloud(Words, Weights);

        series.Sizes = new Range1D(10, 40);

        Assert.Equal(new Range1D(10, 40), series.Sizes);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Sizes = new Range1D(0, 40));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Sizes = new Range1D(30, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Sizes = new Range1D(double.NaN, 20));
        Assert.Equal(new Range1D(10, 40), series.Sizes);   // a refused range changes nothing
    }
}
