// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Tests.FluentApi;

/// <summary>Verifies <see cref="QuickPlot"/> one-liner façade methods.</summary>
public class QuickPlotTests
{
    private static readonly double[] X = [1.0, 2.0, 3.0, 4.0];
    private static readonly double[] Y = [10.0, 20.0, 30.0, 40.0];

    public static TheoryData<string, Func<string?, FigureBuilder>, Type> OneLiners => new()
    {
        { "line", title => QuickPlot.Line(X, Y, title: title), typeof(LineSeries) },
        { "scatter", title => QuickPlot.Scatter(X, Y, title: title), typeof(ScatterSeries) },
        { "histogram", title => QuickPlot.Hist(Y, title: title), typeof(HistogramSeries) },
        { "signal", title => QuickPlot.Signal(Y, title: title), typeof(SignalSeries) },
        { "signal over x", title => QuickPlot.SignalXY(X, Y, title: title), typeof(SignalXYSeries) },
    };

    /// <summary>A one-liner draws one series of its kind, and the figure carries the title it is given and none when it is given none.</summary>
    [Theory]
    [MemberData(nameof(OneLiners))]
    public void AOneLiner_DrawsOneSeriesOfItsKind_UnderTheTitleItIsGiven(string title, Func<string?, FigureBuilder> plot, Type kind)
    {
        var untitled = plot(null).Build();

        Assert.IsType(kind, Assert.Single(untitled.SubPlots[0].Series));
        Assert.Null(untitled.Title);
        Assert.Equal(title, plot(title).Build().Title);
    }

    // ── Line ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Line_ReturnsChainableFigureBuilder()
    {
        var fig = QuickPlot.Line(X, Y).WithSize(500, 400).Build();
        Assert.Equal(500, fig.Width);
        Assert.Equal(400, fig.Height);
    }

    // ── Hist ──────────────────────────────────────────────────────────────────

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

    // ── Signal ────────────────────────────────────────────────────────────────

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

    // ── Svg ───────────────────────────────────────────────────────────────────

    /// <summary>What each fact about the markup of this class held, as one row: how it is drawn, the tokens it must carry and the ones it must not.</summary>
    public static TheoryData<DrawnToken> Tokens { get; } = new()
    {
        new DrawnToken("Svg_InvokesConfigureAction_ReturnsValidSvgString", () => QuickPlot.Svg(fb => fb.Plot(X, Y).WithTitle("T")), "<polyline"),
        new DrawnToken("Svg_ActionCanChainBuilderMethods", () => QuickPlot.Svg(fb => fb.Plot(X, Y).WithTitle("Chained").WithSize(400, 300)), "Chained"),
    };

    [Theory]
    [MemberData(nameof(Tokens))]
    public void TheMarkup_CarriesWhatItsRowNames(DrawnToken row) => row.AssertHolds();

    [Fact]
    public void Svg_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => QuickPlot.Svg(null!));
    }
}
