// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Cohere</c> verb (matplotlib's <c>cohere</c>): the magnitude-squared coherence of two signals, |Pxy|² / (Pxx ·
/// Pyy), estimated by Welch's method (<c>Welch</c>, held in <c>WelchTests</c>) and drawn as ONE line of (frequency, coherence)
/// on the LINEAR scale between 0 and 1: no decibels. matplotlib refuses a signal shorter than two segments, because the
/// coherence of a single segment is 1 at every frequency whatever the signals are; so does the verb. The numbers marked
/// "matplotlib" were printed by matplotlib 3.11.2 for the two signals below with <c>NFFT=16, Fs=1, noverlap=4</c>.</summary>
public class CohereTests
{
    private const double Tolerance = 1e-9;

    private static readonly double[] X =
    [
        -5, 15, 4, -7, 2, 11, 0, -11, -2, 7, -4, -15, 5, 14, 3, -8,
        1, 10, -1, -12, -3, 6, -5, -5, 4, 13, 2, -9, 0, 9, -2, -13,
    ];

    private static readonly double[] Y =
    [
        0, -5, -10, -3, 4, -1, -6, 1, 8, 3, -2, 5, 12, -6, -11, -4,
        3, -2, -7, 0, 7, 2, -3, 4, 11, 6, -12, -5, 2, -3, -8, -1,
    ];

    /// <summary>Segments of 16 samples that start 12 apart: the 32 samples above hold two, which is the fewest a coherence needs.</summary>
    private static readonly SpectralOptions Sixteen = new() { SampleRate = 1, SegmentLength = 16, Overlap = 4 };

    private static readonly double[] Frequencies = [0, 0.0625, 0.125, 0.1875, 0.25, 0.3125, 0.375, 0.4375, 0.5];

    /// <summary>matplotlib's <c>cohere(X, Y, NFFT=16, Fs=1, noverlap=4)[0]</c>.</summary>
    private static readonly double[] Coherence =
    [
        0.7398273667532683, 0.7641232013324645, 0.8144301759255399, 0.9298316916693179, 0.9906896053191183,
        0.9891154864114439, 0.5066652647894018, 0.5965877425693659, 0.6127959243651422,
    ];

    private static Figure Of(double[] x, double[] y, SpectralOptions options = default, Action<LineSeries>? configure = null) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.Cohere(x, y, options, configure)).Build();

    private static LineSeries OnlySeriesOf(Figure figure) => Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));

    private static void AssertClose(IReadOnlyList<double> expected, IReadOnlyList<double> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.True(Math.Abs(expected[i] - actual[i]) <= Tolerance, $"[{i}]: expected {expected[i]:R}, got {actual[i]:R}");
        }
    }

    // ---- the shape the contracts hold ---------------------------------------------------------------------------------

    [Fact]
    public void TheVerb_DrawsExactlyWhatTheHandRecipeDraws()
    {
        Assert.Equal(ComposedChartShapes.CohereByHand(false).ToSvg(), ComposedChartShapes.Named("cohere").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.CohereByHand(true).ToSvg(), ComposedChartShapes.Named("cohere").Build(true).ToSvg());
    }

    // ---- what is drawn --------------------------------------------------------------------------------------------------

    [Fact]
    public void TheChart_IsOneLineSeries_OfTheFrequenciesAndTheirCoherence_AsMatplotlibPlotsThem()
    {
        var line = OnlySeriesOf(Of(X, Y, Sixteen));

        AssertClose(Frequencies, line.XData);
        AssertClose(Coherence, line.YData);
    }

    [Fact]
    public void TheCoherence_IsLinear_BetweenZeroAndOne_NotInDecibels()
    {
        Assert.All(OnlySeriesOf(Of(X, Y, Sixteen)).YData, value => Assert.InRange(value, 0.0, 1.0));
    }

    [Fact]
    public void ASignalWithItself_HasCoherenceOneAtEveryFrequency()
    {
        AssertClose([.. Enumerable.Repeat(1.0, 9)], OnlySeriesOf(Of(X, X, Sixteen)).YData);
    }

    [Fact]
    public void ASignalWithAScaledCopyOfItself_HasCoherenceOne_TheScaleIsNoPartOfIt()
    {
        AssertClose([.. Enumerable.Repeat(1.0, 9)], OnlySeriesOf(Of(X, [.. X.Select(v => -3 * v)], Sixteen)).YData);
    }

    [Fact]
    public void ABinWithNoPowerInEitherSignal_HasNoCoherence_AGapAndNeverZero()
    {
        // A constant under a rectangular window has power in the first bin only; the others are 0 / 0.
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 8, Window = SpectralWindow.Rectangular };
        double[] ones = [.. Enumerable.Repeat(1.0, 16)];
        var figure = Of(ones, ones, options);

        var line = OnlySeriesOf(figure);

        Assert.Equal(1.0, line.YData[0], Tolerance);
        Assert.All(line.YData.Skip(1), value => Assert.True(double.IsNaN(value), $"expected a gap, got {value:R}"));
        Assert.DoesNotContain("NaN", figure.ToSvg(), StringComparison.Ordinal);
    }

    // ---- missing samples ----------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AMissingSample_InEitherSignal_InEverySegment_LeavesNothingToDraw_ButTheFrequencies(bool inTheFirst)
    {
        double[] x = [.. X], y = [.. Y];
        (inTheFirst ? x : y)[13] = double.NaN;   // samples 12..15 are in both segments

        var figure = Of(x, y, Sixteen);
        var line = OnlySeriesOf(figure);

        AssertClose(Frequencies, line.XData);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        Assert.DoesNotContain("NaN", figure.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingSample_InOneSegment_LeavesThatSegmentOut_AndTheRestIsStillDrawn()
    {
        double[] x = [.. X, .. X], y = [.. Y, .. Y], gappy = [.. x];
        gappy[5] = double.NaN;   // the first of four segments
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 16 };

        var drawn = OnlySeriesOf(Of(gappy, y, options));
        var rest = OnlySeriesOf(Of(x[16..], y[16..], options));

        Assert.Equal(9, drawn.YData.Count(double.IsFinite));
        AssertClose(rest.YData, drawn.YData);
    }

    // ---- the options reach the estimator ---------------------------------------------------------------------------------

    private static readonly Dictionary<string, SpectralOptions> OptionsThatChangeTheEstimate = new()
    {
        ["sample rate"] = new SpectralOptions { SampleRate = 1000, SegmentLength = 16, Overlap = 4 },
        ["window"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, Window = SpectralWindow.Rectangular },
        ["detrend"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, Detrend = SpectralDetrend.Mean },
        ["overlap"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 8 },
        ["segment length"] = new SpectralOptions { SampleRate = 1, SegmentLength = 10 },
        ["padding"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, PadTo = 32 },
        ["centre frequency"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, CenterFrequency = 100 },
    };

    public static TheoryData<string> OptionNames { get; } = new(OptionsThatChangeTheEstimate.Keys);

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void TheOptions_ReachTheEstimator(string name)
    {
        var options = OptionsThatChangeTheEstimate[name];
        SpectralDensity estimate = Welch.Coherence(X, Y, options);

        var line = OnlySeriesOf(Of(X, Y, options));

        AssertClose(estimate.Frequencies, line.XData);
        AssertClose(estimate.Power, line.YData);
        Assert.False(line.XData.SequenceEqual(Frequencies) && line.YData.SequenceEqual(Coherence), $"{name}: the options drew nothing the defaults do not");
    }

    [Fact]
    public void WithoutOptions_TheDefaultsAreMatplotlibs_ASegmentOf256AndASampleRateOf2()
    {
        double[] x = [.. Enumerable.Range(0, 512).Select(i => (double)(i * 7919 % 41 - 20))];
        double[] y = [.. Enumerable.Range(0, 512).Select(i => (double)(i * 104729 % 37 - 18))];

        var line = OnlySeriesOf(Of(x, y));

        Assert.Equal(129, line.XData.Length);
        Assert.Equal(1.0, line.XData[^1]);
    }

    // ---- the axes -------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheAxes_AreTitledWithWhatTheyMeasure_AndATitleSetAfterTheCallWins()
    {
        var axes = Of(X, Y, Sixteen).SubPlots[0];
        var retitled = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Cohere(X, Y, Sixteen).SetXLabel("Hz").SetYLabel("Cxy")).Build().SubPlots[0];

        Assert.Equal("Frequency", axes.XAxis.Label);
        Assert.Equal("Coherence", axes.YAxis.Label);
        Assert.Equal("Hz", retitled.XAxis.Label);
        Assert.Equal("Cxy", retitled.YAxis.Label);
    }

    [Fact]
    public void TheVerb_SetsNoAxisLimits_AndKeepsTheCallersOwn()
    {
        var axes = Of(X, Y, Sixteen).SubPlots[0];
        var kept = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetYLim(0, 2).Cohere(X, Y, Sixteen)).Build().SubPlots[0];

        Assert.False(axes.HasExplicitLimit);
        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.Equal(0.0, kept.YAxis.Min);
        Assert.Equal(2.0, kept.YAxis.Max);
    }

    // ---- the series -----------------------------------------------------------------------------------------------------

    [Fact]
    public void TheLine_IsOneLegendEntry()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Cohere(X, Y, Sixteen, line => line.Label = "x with y")
            .WithLegend()).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Single(Regex.Matches(svg, "data-legend-index="));
        Assert.Contains("x with y", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLine_IsOneColourSlot_TheNextSeriesTakesTheNextColour()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Cohere(X, Y, Sixteen)
            .Plot([0.0, 0.5], [0.0, 1.0])).Build();
        var drawn = new RecordingRenderContext();
        new ChartRenderer().Render(figure, drawn);

        var colours = drawn.OfKind("DrawLines").Select(call => (Color)((dynamic)call.Args!).color).Distinct().ToList();

        Assert.Contains(Theme.Default.CycleColors[0], colours);
        Assert.Contains(Theme.Default.CycleColors[1], colours);
    }

    [Fact]
    public void WhatTheCallbackSets_WinsOverWhatTheVerbDid()
    {
        var line = OnlySeriesOf(Of(X, Y, Sixteen, series => { series.Color = Colors.Red; series.LineWidth = 4; series.LineStyle = LineStyle.Dashed; }));

        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(4.0, line.LineWidth);
        Assert.Equal(LineStyle.Dashed, line.LineStyle);
    }

    [Fact]
    public void TheCallback_RunsOnceOnTheSeriesTheVerbAdded()
    {
        var seen = new List<LineSeries>();

        var figure = Of(X, Y, Sixteen, seen.Add);

        Assert.Same(OnlySeriesOf(figure), Assert.Single(seen));
    }

    [Fact]
    public void TheVerb_ChainsWithTheRestOfTheBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            var same = ax.Cohere(X, Y, Sixteen);
            Assert.Same(ax, same);
        });

        Assert.Single(builder.Build().SubPlots[0].Series);
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of(X, Y, Sixteen, line => line.Label = "x with y");
        double[] gappy = [.. X];
        gappy[13] = double.NaN;
        var gap = Of(gappy, Y, Sixteen);

        Assert.Equal(figure.ToSvg(), ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg());
        Assert.Equal(gap.ToSvg(), ChartSerializerDefault.Current.FromJson(gap.ToJson()).ToSvg());
        Assert.Contains("\"NaN\"", gap.ToJson(), StringComparison.Ordinal);
    }

    // ---- what the verb refuses ----------------------------------------------------------------------------------------

    [Fact]
    public void ASignalOfFewerThanTwoSegments_IsRefusedByTheNameOfTheOptions_AsMatplotlibRefusesIt()
    {
        // matplotlib: cohere(x[:20], x[:20], NFFT=16) raises ValueError. One sample short of 2 * 16 is enough.
        var refused = Assert.Throws<ArgumentException>(() => Of(X[..31], Y[..31], Sixteen));

        Assert.Equal("options", refused.ParamName);
        Assert.Contains("31", refused.Message, StringComparison.Ordinal);
        Assert.Contains("16", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASignalOfExactlyTwoSegments_IsEnough()
    {
        Assert.Equal(9, OnlySeriesOf(Of(X, Y, Sixteen with { Overlap = 0 })).XData.Length);
    }

    [Fact]
    public void ASegmentLongerThanTheSignals_IsRefusedByTheNameOfTheOptions_AndTheDefaultIs256()
    {
        var refused = Assert.Throws<ArgumentException>(() => Of(X, Y));

        Assert.Equal("options", refused.ParamName);
        Assert.Contains("256", refused.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(8, 8, 0)]
    [InlineData(8, 12, 0)]
    [InlineData(8, 0, 6)]
    public void OptionsThatDoNotFitTheSignals_AreRefusedByTheNameOfTheOptions(int segment, int overlap, int padTo)
    {
        var options = new SpectralOptions { SegmentLength = segment, Overlap = overlap, PadTo = padTo };

        Assert.Equal("options", Assert.Throws<ArgumentException>(() => Of(X, Y, options)).ParamName);
    }

    [Fact]
    public void ANullSignal_IsRefusedByItsName()
    {
        Assert.Equal("x", Assert.Throws<ArgumentNullException>(() => Of(null!, Y, Sixteen)).ParamName);
        Assert.Equal("y", Assert.Throws<ArgumentNullException>(() => Of(X, null!, Sixteen)).ParamName);
    }

    [Fact]
    public void AnEmptySignal_IsRefusedByTheNameOfX()
    {
        Assert.Equal("x", Assert.Throws<ArgumentException>(() => Of([], [], Sixteen)).ParamName);
    }

    [Fact]
    public void TwoSignalsOfDifferentLengths_AreRefusedByTheNameOfY()
    {
        double[] longer = [.. X, .. X];

        var refused = Assert.Throws<ArgumentException>(() => Of(longer, Y, Sixteen));

        Assert.Equal("y", refused.ParamName);
    }

    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Throws<ArgumentException>(() => ax.Cohere(X[..31], Y[..31], Sixteen));
            Assert.Throws<ArgumentException>(() => ax.Cohere(X, Y));
            Assert.Throws<ArgumentException>(() => ax.Cohere([], [], Sixteen));
            Assert.Throws<ArgumentNullException>(() => ax.Cohere(null!, Y, Sixteen));
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
        Assert.Null(axes.XAxis.Label);
        Assert.Null(axes.YAxis.Label);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.Cohere(X, Y, Sixteen)).ParamName);
    }
}
