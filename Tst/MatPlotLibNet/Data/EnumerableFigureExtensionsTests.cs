// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Tests.Data;

/// <summary>Verifies <see cref="EnumerableFigureExtensions"/> builds correct figures from typed sequences.</summary>
public class EnumerableFigureExtensionsTests
{
    private record Point(string Group, double X, double Y);

    // ── Line ────────────────────────────────────────────────────────────────

    [Fact]
    public void Line_NoHue_ProducesOneSeries()
    {
        var data = new[] { new Point("A", 1, 10), new Point("A", 2, 20) };
        var figure = data.Line(p => p.X, p => p.Y).Build();
        Assert.Single(figure.SubPlots[0].Series);
    }

    [Fact]
    public void Line_NoHue_SeriesIsLineSeries()
    {
        var data = new[] { new Point("A", 1, 10), new Point("A", 2, 20) };
        var figure = data.Line(p => p.X, p => p.Y).Build();
        Assert.IsType<LineSeries>(figure.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Line_WithHue_OneSeriesPerGroup()
    {
        var data = new[]
        {
            new Point("A", 1, 10), new Point("B", 2, 20),
            new Point("C", 3, 30), new Point("A", 4, 40),
        };
        var figure = data.Line(p => p.X, p => p.Y, hue: p => p.Group).Build();
        Assert.Equal(3, figure.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Line_WithHue_LabelsMatchGroupKeys()
    {
        var data = new[]
        {
            new Point("Alpha", 1, 10), new Point("Beta", 2, 20), new Point("Alpha", 3, 30),
        };
        var figure = data.Line(p => p.X, p => p.Y, hue: p => p.Group).Build();
        var labels = figure.SubPlots[0].Series.Select(s => s.Label).ToArray();
        Assert.Contains("Alpha", labels);
        Assert.Contains("Beta",  labels);
    }

    [Fact]
    public void Line_WithHue_DistinctColorsPerGroup()
    {
        var data = new[]
        {
            new Point("A", 1, 1), new Point("B", 2, 2), new Point("C", 3, 3),
        };
        var figure = data.Line(p => p.X, p => p.Y, hue: p => p.Group).Build();
        var colors = figure.SubPlots[0].Series.OfType<LineSeries>().Select(s => s.Color).ToArray();
        Assert.Equal(3, colors.Distinct().Count());
    }

    [Fact]
    public void Line_WithHue_CustomPalette_FirstGroupUsesFirstColor()
    {
        var red = Color.FromHex("#FF0000");
        var data = new[] { new Point("A", 1, 1), new Point("B", 2, 2) };
        var figure = data.Line(p => p.X, p => p.Y, hue: p => p.Group, palette: [red]).Build();
        var first = (LineSeries)figure.SubPlots[0].Series[0];
        Assert.Equal(red, first.Color);
    }

    [Fact]
    public void Line_ReturnsChainableFigureBuilder()
    {
        var data = new[] { new Point("A", 1, 1) };
        var builder = data.Line(p => p.X, p => p.Y);
        // Verify chaining works — WithTitle returns same builder type
        var figure = builder.WithTitle("Test").Build();
        Assert.Equal("Test", figure.Title);
    }

    // ── Scatter ─────────────────────────────────────────────────────────────

    [Fact]
    public void Scatter_NoHue_ProducesOneScatterSeries()
    {
        var data = new[] { new Point("A", 1, 10), new Point("A", 2, 20) };
        var figure = data.Scatter(p => p.X, p => p.Y).Build();
        Assert.IsType<ScatterSeries>(figure.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Scatter_WithHue_OneSeriesPerGroup()
    {
        var data = new[]
        {
            new Point("A", 1, 1), new Point("B", 2, 2), new Point("A", 3, 3),
        };
        var figure = data.Scatter(p => p.X, p => p.Y, hue: p => p.Group).Build();
        Assert.Equal(2, figure.SubPlots[0].Series.Count);
    }

    // ── Hist ────────────────────────────────────────────────────────────────

    [Fact]
    public void Hist_NoHue_ProducesOneHistogramSeries()
    {
        var data = new[] { new Point("A", 0, 1.0), new Point("A", 0, 2.0) };
        var figure = data.Hist(p => p.Y).Build();
        Assert.IsType<HistogramSeries>(figure.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Hist_WithHue_OneHistogramPerGroup()
    {
        var data = new[]
        {
            new Point("A", 0, 1.0), new Point("B", 0, 2.0), new Point("A", 0, 3.0),
        };
        var figure = data.Hist(p => p.Y, hue: p => p.Group).Build();
        Assert.Equal(2, figure.SubPlots[0].Series.Count);
    }

    [Fact]
    public void Hist_DefaultBins_Is30()
    {
        var data = new[] { new Point("A", 0, 1.0), new Point("A", 0, 2.0) };
        var figure = data.Hist(p => p.Y).Build();
        var hist = (HistogramSeries)figure.SubPlots[0].Series[0];
        Assert.Equal(30, hist.Bins);
    }
}
