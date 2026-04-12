// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using Microsoft.Data.Analysis;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.DataFrame;

/// <summary>Verifies <see cref="DataFrameFigureExtensions"/> Line, Scatter, and Hist methods.</summary>
public class DataFrameFigureExtensionsTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Microsoft.Data.Analysis.DataFrame MakeXYDf(
        int[] xs, double[] ys, string[]? hues = null)
    {
        var cols = new List<DataFrameColumn>
        {
            new PrimitiveDataFrameColumn<int>("x", xs),
            new PrimitiveDataFrameColumn<double>("y", ys)
        };
        if (hues is not null)
            cols.Add(new StringDataFrameColumn("group", hues));
        return new Microsoft.Data.Analysis.DataFrame(cols);
    }

    // ── Line — no hue ─────────────────────────────────────────────────────────

    [Fact]
    public void Line_NoHue_ProducesOneLineSeries()
    {
        var df = MakeXYDf([1, 2, 3, 4], [10.0, 20.0, 30.0, 40.0]);
        var fig = df.Line("x", "y").Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<LineSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Line_NoHue_PreservesRowCount()
    {
        var df = MakeXYDf([1, 2, 3, 4], [10.0, 20.0, 30.0, 40.0]);
        var fig = df.Line("x", "y").Build();
        var s = (LineSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(4, s.XData.Length);
    }

    [Fact]
    public void Line_WithHue_OneSeriesPerGroup()
    {
        var df = MakeXYDf([1, 2, 3, 4], [1.0, 2.0, 3.0, 4.0], ["A", "A", "B", "B"]);
        var fig = df.Line("x", "y", hue: "group").Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Line_WithHue_LabelsMatchGroupKeys()
    {
        var df = MakeXYDf([1, 2, 3, 4], [1.0, 2.0, 3.0, 4.0], ["A", "A", "B", "B"]);
        var fig = df.Line("x", "y", hue: "group").Build();
        var labels = fig.SubPlots[0].Series.Select(s => s.Label).ToHashSet();
        Assert.Contains("A", labels);
        Assert.Contains("B", labels);
    }

    [Fact]
    public void Line_WithHue_CustomPalette_FirstGroupUsesFirstColor()
    {
        var red = new Color(255, 0, 0);
        var blue = new Color(0, 0, 255);
        var df = MakeXYDf([1, 2, 3, 4], [1.0, 2.0, 3.0, 4.0], ["A", "A", "B", "B"]);
        var fig = df.Line("x", "y", hue: "group", palette: [red, blue]).Build();
        var firstSeries = (LineSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(red, firstSeries.Color);
    }

    [Fact]
    public void Line_UnknownColumn_ThrowsArgumentException()
    {
        var df = MakeXYDf([1, 2], [1.0, 2.0]);
        var ex = Assert.Throws<ArgumentException>(() => df.Line("x", "missing"));
        Assert.Contains("missing", ex.Message);
    }

    [Fact]
    public void Line_ReturnsChainableFigureBuilder()
    {
        var df = MakeXYDf([1, 2, 3], [1.0, 2.0, 3.0]);
        var fig = df.Line("x", "y").WithTitle("My Chart").Build();
        Assert.Equal("My Chart", fig.Title);
    }

    // ── Scatter ───────────────────────────────────────────────────────────────

    [Fact]
    public void Scatter_NoHue_ProducesOneScatterSeries()
    {
        var df = MakeXYDf([1, 2, 3], [1.0, 2.0, 3.0]);
        var fig = df.Scatter("x", "y").Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<ScatterSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Scatter_WithHue_OneSeriesPerGroup()
    {
        var df = MakeXYDf([1, 2, 3, 4], [1.0, 2.0, 3.0, 4.0], ["A", "A", "B", "B"]);
        var fig = df.Scatter("x", "y", hue: "group").Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }

    // ── Hist ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Hist_NoHue_ProducesOneHistogramSeries()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", [1.0, 2.0, 3.0, 4.0, 5.0]));
        var fig = df.Hist("val").Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<HistogramSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Hist_DefaultBins_Is30()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", Enumerable.Range(0, 50).Select(i => (double)i).ToArray()));
        var fig = df.Hist("val").Build();
        var s = (HistogramSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(30, s.Bins);
    }

    [Fact]
    public void Hist_ExplicitBins_Used()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", Enumerable.Range(0, 50).Select(i => (double)i).ToArray()));
        var fig = df.Hist("val", bins: 10).Build();
        var s = (HistogramSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(10, s.Bins);
    }

    [Fact]
    public void Hist_WithHue_OneSeriesPerGroup()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", [1.0, 2.0, 3.0, 4.0]),
            new StringDataFrameColumn("grp", ["A", "A", "B", "B"]));
        var fig = df.Hist("val", hue: "grp").Build();
        Assert.Equal(2, fig.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Hist_WithHue_OverlappingAlpha()
    {
        var df = new Microsoft.Data.Analysis.DataFrame(
            new PrimitiveDataFrameColumn<double>("val", [1.0, 2.0, 3.0, 4.0]),
            new StringDataFrameColumn("grp", ["A", "A", "B", "B"]));
        var fig = df.Hist("val", hue: "grp").Build();
        foreach (var s in fig.SubPlots[0].Series.Cast<HistogramSeries>())
            Assert.Equal(0.7, s.Alpha, 5);
    }
}
