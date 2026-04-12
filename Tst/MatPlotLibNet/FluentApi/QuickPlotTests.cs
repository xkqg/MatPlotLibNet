// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="QuickPlot"/> one-liner façade methods.</summary>
public class QuickPlotTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0];
    private static readonly double[] Y = [10.0, 20.0, 30.0, 40.0];

    // ── Line ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Line_NoTitle_ReturnsFigureBuilderWithLineSeries()
    {
        var fig = QuickPlot.Line(X, Y).Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<LineSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Line_NoTitle_TitleIsNull()
    {
        var fig = QuickPlot.Line(X, Y).Build();
        Assert.Null(fig.Title);
    }

    [Fact]
    public void Line_WithTitle_SetsTitle()
    {
        var fig = QuickPlot.Line(X, Y, title: "Demo").Build();
        Assert.Equal("Demo", fig.Title);
    }

    [Fact]
    public void Line_ReturnsChainableFigureBuilder()
    {
        var fig = QuickPlot.Line(X, Y).WithSize(500, 400).Build();
        Assert.Equal(500, fig.Width);
        Assert.Equal(400, fig.Height);
    }

    // ── Scatter ───────────────────────────────────────────────────────────────

    [Fact]
    public void Scatter_NoTitle_ReturnsFigureBuilderWithScatterSeries()
    {
        var fig = QuickPlot.Scatter(X, Y).Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<ScatterSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Scatter_WithTitle_SetsTitle()
    {
        var fig = QuickPlot.Scatter(X, Y, title: "Dots").Build();
        Assert.Equal("Dots", fig.Title);
    }

    // ── Hist ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Hist_NoTitle_ReturnsFigureBuilderWithHistogramSeries()
    {
        var data = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var fig = QuickPlot.Hist(data).Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<HistogramSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Hist_DefaultBins_Is10()
    {
        var data = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var fig = QuickPlot.Hist(data).Build();
        var s = (HistogramSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(10, s.Bins);
    }

    [Fact]
    public void Hist_ExplicitBins_Used()
    {
        var data = Enumerable.Range(0, 50).Select(i => (double)i).ToArray();
        var fig = QuickPlot.Hist(data, bins: 20).Build();
        var s = (HistogramSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(20, s.Bins);
    }

    [Fact]
    public void Hist_WithTitle_SetsTitle()
    {
        var data = Enumerable.Range(0, 10).Select(i => (double)i).ToArray();
        var fig = QuickPlot.Hist(data, title: "Distribution").Build();
        Assert.Equal("Distribution", fig.Title);
    }

    // ── Signal ────────────────────────────────────────────────────────────────

    [Fact]
    public void Signal_NoTitle_ReturnsFigureBuilderWithSignalSeries()
    {
        var y = Enumerable.Range(0, 100).Select(i => Math.Sin(i * 0.1)).ToArray();
        var fig = QuickPlot.Signal(y).Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<SignalSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void Signal_DefaultSampleRate_Is1()
    {
        var y = new double[] { 1.0, 2.0, 3.0 };
        var fig = QuickPlot.Signal(y).Build();
        var s = (SignalSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(1.0, s.SampleRate);
    }

    [Fact]
    public void Signal_ExplicitSampleRateAndXStart_Used()
    {
        var y = new double[] { 1.0, 2.0, 3.0 };
        var fig = QuickPlot.Signal(y, sampleRate: 44100.0, xStart: 5.0).Build();
        var s = (SignalSeries)fig.SubPlots[0].Series[0];
        Assert.Equal(44100.0, s.SampleRate);
        Assert.Equal(5.0, s.XStart);
    }

    [Fact]
    public void Signal_WithTitle_SetsTitle()
    {
        var y = new double[] { 1.0, 2.0 };
        var fig = QuickPlot.Signal(y, title: "Audio").Build();
        Assert.Equal("Audio", fig.Title);
    }

    // ── SignalXY ──────────────────────────────────────────────────────────────

    [Fact]
    public void SignalXY_NoTitle_ReturnsFigureBuilderWithSignalXYSeries()
    {
        var fig = QuickPlot.SignalXY(X, Y).Build();
        Assert.Single(fig.SubPlots[0].Series);
        Assert.IsType<SignalXYSeries>(fig.SubPlots[0].Series[0]);
    }

    [Fact]
    public void SignalXY_WithTitle_SetsTitle()
    {
        var fig = QuickPlot.SignalXY(X, Y, title: "XY").Build();
        Assert.Equal("XY", fig.Title);
    }

    // ── Svg ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Svg_InvokesConfigureAction_ReturnsValidSvgString()
    {
        var svg = QuickPlot.Svg(fb => fb.Plot(X, Y).WithTitle("T"));
        Assert.Contains("<polyline", svg);
    }

    [Fact]
    public void Svg_ActionCanChainBuilderMethods()
    {
        var svg = QuickPlot.Svg(fb => fb.Plot(X, Y).WithTitle("Chained").WithSize(400, 300));
        Assert.Contains("Chained", svg);
    }

    [Fact]
    public void Svg_EmptyAction_StillReturnsValidSvg()
    {
        var svg = QuickPlot.Svg(_ => { });
        Assert.StartsWith("<svg", svg);
    }

    [Fact]
    public void Svg_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => QuickPlot.Svg(null!));
    }
}
