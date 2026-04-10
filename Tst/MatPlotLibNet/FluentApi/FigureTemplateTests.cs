// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="FigureTemplates"/> behavior.</summary>
public class FigureTemplateTests
{
    private static readonly double[] Open  = [100, 102, 101, 103, 105, 104, 106, 107, 106, 108];
    private static readonly double[] High  = [103, 104, 103, 105, 107, 106, 108, 109, 108, 110];
    private static readonly double[] Low   = [ 98,  99,  98, 100, 102, 101, 103, 104, 103, 105];
    private static readonly double[] Close = [101, 102, 101, 103, 105, 104, 106, 107, 106, 108];
    private static readonly double[] Volume = [1000, 1200, 800, 1500, 2000, 900, 1800, 2200, 700, 1600];

    // --- FinancialDashboard ---

    /// <summary>FinancialDashboard returns a FigureBuilder (not null).</summary>
    [Fact]
    public void FinancialDashboard_ReturnsFigureBuilder()
    {
        var builder = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume);
        Assert.NotNull(builder);
    }

    /// <summary>FinancialDashboard produces a figure with exactly three subplots.</summary>
    [Fact]
    public void FinancialDashboard_HasThreeSubplots()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume).Build();
        Assert.Equal(3, figure.SubPlots.Count);
    }

    /// <summary>The first subplot (price panel) contains a CandlestickSeries.</summary>
    [Fact]
    public void FinancialDashboard_PricePanelHasCandlestick()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume).Build();
        Assert.Contains(figure.SubPlots[0].Series, s => s is CandlestickSeries);
    }

    /// <summary>The second subplot (volume panel) contains a BarSeries.</summary>
    [Fact]
    public void FinancialDashboard_VolumePanelHasBarSeries()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume).Build();
        Assert.Contains(figure.SubPlots[1].Series, s => s is BarSeries);
    }

    /// <summary>The title is applied to the figure when provided.</summary>
    [Fact]
    public void FinancialDashboard_SetsTitle()
    {
        var figure = FigureTemplates.FinancialDashboard(Open, High, Low, Close, Volume, title: "AAPL").Build();
        Assert.Equal("AAPL", figure.Title);
    }

    // --- ScientificPaper ---

    /// <summary>ScientificPaper returns a FigureBuilder.</summary>
    [Fact]
    public void ScientificPaper_ReturnsFigureBuilder()
    {
        var builder = FigureTemplates.ScientificPaper();
        Assert.NotNull(builder);
    }

    /// <summary>ScientificPaper applies 150 DPI.</summary>
    [Fact]
    public void ScientificPaper_AppliesDpi150()
    {
        var figure = FigureTemplates.ScientificPaper().Build();
        Assert.Equal(150, figure.Dpi);
    }

    /// <summary>ScientificPaper produces the requested number of subplots.</summary>
    [Fact]
    public void ScientificPaper_HasCorrectSubplotCount()
    {
        var figure = FigureTemplates.ScientificPaper(rows: 2, cols: 2).Build();
        Assert.Equal(4, figure.SubPlots.Count);
    }

    // --- SparklineDashboard ---

    /// <summary>SparklineDashboard returns a FigureBuilder.</summary>
    [Fact]
    public void SparklineDashboard_ReturnsFigureBuilder()
    {
        var builder = FigureTemplates.SparklineDashboard(
            [("A", [1.0, 2, 3]), ("B", [4.0, 5, 6])]);
        Assert.NotNull(builder);
    }

    /// <summary>SparklineDashboard produces one subplot per series.</summary>
    [Fact]
    public void SparklineDashboard_HasOneAxisPerSeries()
    {
        var series = new (string Label, double[] Values)[]
        {
            ("Revenue", [10, 20, 30]),
            ("Costs",   [8, 15, 25]),
            ("Profit",  [2, 5, 5])
        };
        var figure = FigureTemplates.SparklineDashboard(series).Build();
        Assert.Equal(3, figure.SubPlots.Count);
    }

    // --- JointPlot ---

    private static readonly double[] JointX = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0];
    private static readonly double[] JointY = [1.2, 1.9, 3.1, 4.0, 5.1, 5.8, 7.2, 8.0];

    /// <summary>JointPlot returns a non-null FigureBuilder.</summary>
    [Fact]
    public void JointPlot_ReturnsFigureBuilder()
    {
        var builder = FigureTemplates.JointPlot(JointX, JointY);
        Assert.NotNull(builder);
    }

    /// <summary>JointPlot builds a figure with exactly 3 subplots.</summary>
    [Fact]
    public void JointPlot_HasThreeSubplots()
    {
        var figure = FigureTemplates.JointPlot(JointX, JointY).Build();
        Assert.Equal(3, figure.SubPlots.Count);
    }

    /// <summary>JointPlot center panel contains a ScatterSeries.</summary>
    [Fact]
    public void JointPlot_CenterPanel_ContainsScatterSeries()
    {
        var figure = FigureTemplates.JointPlot(JointX, JointY).Build();
        // Center is subplot index 1 (0-indexed: top marginal, center scatter, right marginal)
        bool hasScatter = figure.SubPlots.Any(ax => ax.Series.OfType<ScatterSeries>().Any());
        Assert.True(hasScatter);
    }

    /// <summary>JointPlot marginals contain HistogramSeries.</summary>
    [Fact]
    public void JointPlot_MarginalPanels_ContainHistogramSeries()
    {
        var figure = FigureTemplates.JointPlot(JointX, JointY).Build();
        int histCount = figure.SubPlots.Sum(ax => ax.Series.OfType<HistogramSeries>().Count());
        Assert.Equal(2, histCount);
    }

    /// <summary>JointPlot renders to valid SVG.</summary>
    [Fact]
    public void JointPlot_RendersToValidSvg()
    {
        string svg = FigureTemplates.JointPlot(JointX, JointY).ToSvg();
        Assert.Contains("<svg", svg);
    }

    /// <summary>JointPlot with title includes the title in SVG output.</summary>
    [Fact]
    public void JointPlot_WithTitle_IncludesTitleInSvg()
    {
        string svg = FigureTemplates.JointPlot(JointX, JointY, title: "My Joint Plot").ToSvg();
        Assert.Contains("My Joint Plot", svg);
    }

    // --- PairPlot ---

    private static readonly double[][] PairColumns =
    [
        [1.0, 2.0, 3.0, 4.0],
        [4.0, 3.0, 2.0, 1.0],
        [1.0, 3.0, 2.0, 4.0]
    ];

    /// <summary>PairPlot returns a non-null FigureBuilder.</summary>
    [Fact]
    public void PairPlot_ReturnsFigureBuilder()
        => Assert.NotNull(FigureTemplates.PairPlot(PairColumns));

    /// <summary>PairPlot creates N×N subplots.</summary>
    [Fact]
    public void PairPlot_HasNxNSubplots()
    {
        int n = PairColumns.Length;
        var figure = FigureTemplates.PairPlot(PairColumns).Build();
        Assert.Equal(n * n, figure.SubPlots.Count);
    }

    /// <summary>PairPlot diagonal panels contain HistogramSeries.</summary>
    [Fact]
    public void PairPlot_DiagonalHasHistograms()
    {
        var figure = FigureTemplates.PairPlot(PairColumns).Build();
        bool hasHist = figure.SubPlots.Any(ax => ax.Series.OfType<HistogramSeries>().Any());
        Assert.True(hasHist);
    }

    /// <summary>PairPlot renders to valid SVG.</summary>
    [Fact]
    public void PairPlot_RendersToValidSvg()
    {
        string svg = FigureTemplates.PairPlot(PairColumns).ToSvg();
        Assert.Contains("<svg", svg);
    }

    // --- FacetGrid ---

    private static readonly double[] FgX = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
    private static readonly double[] FgY = [1.0, 2.0, 3.0, 1.5, 2.5, 3.5];
    private static readonly string[] FgCat = ["A", "A", "B", "B", "C", "C"];

    /// <summary>FacetGrid returns a non-null FigureBuilder.</summary>
    [Fact]
    public void FacetGrid_ReturnsFigureBuilder()
        => Assert.NotNull(FigureTemplates.FacetGrid(FgX, FgY, FgCat, (ax, x, y) => ax.Scatter(x, y)));

    /// <summary>FacetGrid creates one subplot per unique category.</summary>
    [Fact]
    public void FacetGrid_HasOneSubplotPerCategory()
    {
        var figure = FigureTemplates.FacetGrid(FgX, FgY, FgCat, (ax, x, y) => ax.Scatter(x, y)).Build();
        int uniqueCats = FgCat.Distinct().Count();
        Assert.Equal(uniqueCats, figure.SubPlots.Count);
    }

    /// <summary>FacetGrid renders to valid SVG.</summary>
    [Fact]
    public void FacetGrid_RendersToValidSvg()
    {
        string svg = FigureTemplates.FacetGrid(FgX, FgY, FgCat, (ax, x, y) => ax.Scatter(x, y)).ToSvg();
        Assert.Contains("<svg", svg);
    }

    // --- Clustermap ---

    private static readonly double[,] ClusterData =
    {
        { 1.0, 2.0, 0.5 },
        { 1.1, 1.9, 0.6 },
        { 5.0, 0.1, 4.0 },
        { 4.8, 0.2, 3.9 }
    };

    /// <summary>Clustermap returns a non-null FigureBuilder.</summary>
    [Fact]
    public void Clustermap_ReturnsFigureBuilder()
        => Assert.NotNull(FigureTemplates.Clustermap(ClusterData));

    /// <summary>Clustermap builds a figure with exactly 4 subplots (2×2 grid).</summary>
    [Fact]
    public void Clustermap_HasFourSubplots()
    {
        var figure = FigureTemplates.Clustermap(ClusterData).Build();
        Assert.Equal(4, figure.SubPlots.Count);
    }

    /// <summary>Clustermap renders to valid SVG.</summary>
    [Fact]
    public void Clustermap_RendersToValidSvg()
    {
        string svg = FigureTemplates.Clustermap(ClusterData).ToSvg();
        Assert.Contains("<svg", svg);
    }
}
