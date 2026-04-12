// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="PairPlotFigure"/> — faceted N×N pair plot preset.</summary>
public class PairPlotFigureTests
{
    private static readonly double[][] Two   = [[1.0, 2.0, 3.0], [3.0, 2.0, 1.0]];
    private static readonly double[][] Three = [[1.0, 2.0, 3.0], [3.0, 2.0, 1.0], [1.0, 3.0, 2.0]];
    private static readonly string[]   Hue   = ["A", "A", "B"];

    [Fact]
    public void Build_TwoColumns_HasFourSubplots()
    {
        var fig = new PairPlotFigure(Two).Build().Build();
        Assert.Equal(4, fig.SubPlots.Count);
    }

    [Fact]
    public void Build_ThreeColumns_HasNineSubplots()
    {
        var fig = new PairPlotFigure(Three).Build().Build();
        Assert.Equal(9, fig.SubPlots.Count);
    }

    [Fact]
    public void Build_NoHue_DiagonalPanelsAreHistograms()
    {
        var fig = new PairPlotFigure(Three).Build().Build();
        // Diagonal panels are [0,0]=0, [1,1]=4, [2,2]=8 (row*n+col with n=3)
        bool hasHist = fig.SubPlots.Any(sp => sp.Series.OfType<HistogramSeries>().Any());
        Assert.True(hasHist);
    }

    [Fact]
    public void Build_NoHue_OffDiagonalPanelsAreScatters()
    {
        var fig = new PairPlotFigure(Three).Build().Build();
        bool hasScatter = fig.SubPlots.Any(sp => sp.Series.OfType<ScatterSeries>().Any());
        Assert.True(hasScatter);
    }

    [Fact]
    public void Build_WithColumnNames_SetsAxisLabels()
    {
        string[] names = ["Alpha", "Beta", "Gamma"];
        var fb = new PairPlotFigure(Three) { ColumnNames = names }.Build();
        var svg = fb.ToSvg();
        Assert.Contains("Alpha", svg);
    }

    [Fact]
    public void Build_WithHue_OffDiagonalHasOneSeriesPerHueGroup()
    {
        int groups = Hue.Distinct().Count();
        var fig = new PairPlotFigure(Two) { Hue = Hue }.Build().Build();
        // Panel [0,1] (row=0,col=1) is off-diagonal — index 1
        Assert.Equal(groups, fig.SubPlots[1].Series.Count);
    }
}
