// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Builders;

namespace MatPlotLibNet.Mcp.Tests;

/// <summary>A picture in a context window tells a model nothing it can quote back. The summary beside it says what
/// was drawn, from how many points, over which range — and it says <c>null</c> where the library has no count to
/// give, because a confident 0 is worse than an honest gap.</summary>
public class ChartSummarizerTests
{
    private static readonly ChartSummarizer Summarizer = new();

    [Fact]
    public void ASummary_NamesTheChartTypeAndTheSeriesCount()
    {
        var figure = Plt.Create().WithTitle("Revenue")
            .Plot([1.0, 2, 3], [4.0, 5, 6], s => s.Label = "Q1")
            .Scatter([1.0, 2], [1.0, 2])
            .Build();

        var summary = Summarizer.Describe(figure);

        Assert.Equal("Revenue", summary.Title);
        Assert.Equal(1, summary.SubPlotCount);
        Assert.Equal(2, summary.Series.Count);
        Assert.Equal("line", summary.Series[0].Discriminator);
        Assert.Equal("Q1", summary.Series[0].Label);
        Assert.Equal("scatter", summary.Series[1].Discriminator);
    }

    [Fact]
    public void ASummary_CarriesThePerSeriesRange_AsTheRendererComputesIt()
    {
        var figure = Plt.Create().Plot([1.0, 2, 3], [10.0, 30, 20]).Build();

        var series = Summarizer.Describe(figure).Series[0];

        Assert.Equal(1, series.XRange!.Value.Min);
        Assert.Equal(3, series.XRange!.Value.Max);
        Assert.Equal(10, series.YRange!.Value.Min);
        Assert.Equal(30, series.YRange!.Value.Max);
    }

    [Fact]
    public void ASummary_CountsThePoints_WhereTheSeriesHasThem()
    {
        var figure = Plt.Create().Plot([1.0, 2, 3, 4], [1.0, 2, 3, 4]).Build();

        Assert.Equal(4, Summarizer.Describe(figure).Series[0].PointCount);
    }

    [Fact]
    public void APointCountWithNoCarrier_IsNullNotZero()
    {
        // A pie has sizes, not points; the library exposes no count that means the same thing, and reporting 0
        // would read as "an empty chart" for a chart that draws perfectly well.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Pie([3.0, 2, 1])).Build();

        Assert.Null(Summarizer.Describe(figure).Series[0].PointCount);
    }

    [Fact]
    public void AFigureWithNoSeries_StillSummarises()
    {
        var summary = Summarizer.Describe(Plt.Create().WithTitle("Empty").Build());

        Assert.Equal("Empty", summary.Title);
        Assert.Empty(summary.Series);
    }

    [Fact]
    public void AMultiAxesFigure_SummarisesEverySubPlot()
    {
        var figure = Plt.Create()
            .AddSubPlot(2, 1, 1, ax => ax.Plot([1.0, 2], [1.0, 2]))
            .AddSubPlot(2, 1, 2, ax => ax.Plot([1.0, 2], [3.0, 4]))
            .Build();

        var summary = Summarizer.Describe(figure);

        Assert.Equal(2, summary.SubPlotCount);
        Assert.Equal(2, summary.Series.Count);
    }

    [Fact]
    public void ASeriesWhoseRangeCannotBeComputed_LosesItsRange_NotTheWholeCall()
    {
        // Candlestick asks Low.Min() and throws on an empty array: one series must not take the summary with it.
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Candlestick([], [], [], [])).Build();

        var series = Summarizer.Describe(figure).Series[0];

        Assert.Equal("candlestick", series.Discriminator);
        Assert.Null(series.XRange);
    }

    [Fact]
    public void TheSummaryReadsAsOneLinePerSeries()
    {
        var figure = Plt.Create().WithTitle("Revenue").Plot([1.0, 2], [3.0, 4], s => s.Label = "Q1").Build();

        string text = Summarizer.Describe(figure).ToText();

        Assert.Contains("Revenue", text);
        Assert.Contains("line", text);
        Assert.Contains("Q1", text);
        Assert.Contains("2 points", text);
    }
}
