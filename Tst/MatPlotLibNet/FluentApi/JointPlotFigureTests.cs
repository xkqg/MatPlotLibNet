// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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

    [Fact]
    public void Build_NoHue_CenterPanelHasOneScatterSeries()
    {
        var fig = new JointPlotFigure(X, Y).Build().Build();
        Assert.Single(fig.SubPlots[1].Series);
        Assert.IsType<ScatterSeries>(fig.SubPlots[1].Series[0]);
    }

    [Fact]
    public void Build_NoHue_TopPanelHasOneHistSeries()
    {
        var fig = new JointPlotFigure(X, Y).Build().Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<HistogramSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Build_NoHue_RightPanelHasOneHistSeries()
    {
        var fig = new JointPlotFigure(X, Y).Build().Build();
        Assert.Single(fig.SubPlots[2].Series);
        Assert.IsType<HistogramSeries>(fig.SubPlots[2].Series[0]);
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

    [Fact]
    public void Build_WithHue_CenterScatterHasOneSeriesPerHueGroup()
    {
        int groups = Hue.Distinct().Count();
        var fig = new JointPlotFigure(X, Y) { Hue = Hue }.Build().Build();
        Assert.Equal(groups, fig.SubPlots[1].Series.Count);
    }

    [Fact]
    public void Build_WithHue_TopHistHasOneSeriesPerHueGroup()
    {
        int groups = Hue.Distinct().Count();
        var fig = new JointPlotFigure(X, Y) { Hue = Hue }.Build().Build();
        Assert.Equal(groups, fig.SubPlots[0].Series.Count);
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
