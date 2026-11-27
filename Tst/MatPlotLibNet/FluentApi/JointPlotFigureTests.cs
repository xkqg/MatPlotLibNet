// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="JointPlotFigure"/> — faceted joint plot preset.</summary>
public class JointPlotFigureTests
{
    private static readonly double[] X   = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
    private static readonly double[] Y   = [1.2, 2.1, 3.3, 4.0, 5.1, 6.2];
    private static readonly string[] Hue = ["A", "A", "B", "B", "C", "C"];

    // Panel order: [0] top marginal (X hist), [1] center scatter, [2] right marginal (Y hist)

    [Fact]
    public void Build_NoHue_HasThreeSubplots()
    {
        var fig = new JointPlotFigure(X, Y).Build().Build();
        Assert.Equal(3, fig.SubPlots.Count);
    }

    [Theory]
    [InlineData(1, typeof(ScatterSeries))]
    [InlineData(0, typeof(HistogramSeries))]
    [InlineData(2, typeof(HistogramSeries))]
    public void Build_NoHue_EveryPanelHasOneSeriesOfItsKind(int panel, Type kind)
    {
        var fig = new JointPlotFigure(X, Y).Build().Build();
        Assert.Single(fig.SubPlots[panel].Series);
        Assert.IsType(kind, fig.SubPlots[panel].Series[0]);
    }

    [Fact]
    public void Build_WithTitle_SetsFigureTitle()
    {
        var fig = new JointPlotFigure(X, Y) { Title = "Joint" }.Build().Build();
        Assert.Equal("Joint", fig.Title);
    }

    [Fact]
    public void Build_WithExplicitBins_PropagatesToBothMarginals()
    {
        var fig = new JointPlotFigure(X, Y) { Bins = 15 }.Build().Build();
        var topHist   = (HistogramSeries)fig.SubPlots[0].Series[0];
        var rightHist = (HistogramSeries)fig.SubPlots[2].Series[0];
        Assert.Equal(15, topHist.Bins);
        Assert.Equal(15, rightHist.Bins);
    }

    [Theory]
    [InlineData(1)] // the centre scatter
    [InlineData(0)] // the top histogram
    public void Build_WithHue_EachPanelHasOneSeriesPerHueGroup(int panel)
    {
        int groups = Hue.Distinct().Count();
        var fig = new JointPlotFigure(X, Y) { Hue = Hue }.Build().Build();
        Assert.Equal(groups, fig.SubPlots[panel].Series.Count);
    }

    [Fact]
    public void Build_WithHue_ScatterLabelsMatchHueKeys()
    {
        var expectedLabels = Hue.Distinct().Order().ToArray();
        var fig = new JointPlotFigure(X, Y) { Hue = Hue }.Build().Build();
        var actualLabels = fig.SubPlots[1].Series.Select(s => s.Label).Order().ToArray();
        Assert.Equal(expectedLabels, actualLabels);
    }
}
