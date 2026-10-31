// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using System.Text.RegularExpressions;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Serialization;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Tests.Rendering;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Csd</c> verb (matplotlib's <c>csd</c>): the cross spectral density of two signals, estimated by Welch's method
/// (<c>Welch</c>, held in <c>WelchTests</c>) and drawn as ONE line of (frequency, 10·log10 of the MAGNITUDE of the cross
/// density), as matplotlib draws it: the magnitude, not its square. The numbers marked "matplotlib" were printed by matplotlib
/// 3.11.2 for the two signals below with <c>NFFT=16, Fs=1, noverlap=4</c>.</summary>
public class CsdTests
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

    private static readonly SpectralOptions Sixteen = new() { SampleRate = 1, SegmentLength = 16, Overlap = 4 };

    private static readonly double[] Frequencies = [0, 0.0625, 0.125, 0.1875, 0.25, 0.3125, 0.375, 0.4375, 0.5];

    /// <summary>matplotlib's <c>10 * np.log10(np.abs(csd(X, Y, NFFT=16, Fs=1, noverlap=4)[0]))</c>.</summary>
    private static readonly double[] Decibels =
    [
        7.9074972286111045, 16.734114992703265, 13.418136274606553, 17.607414571221337, 24.778349562021084,
        20.080097917968416, 0.12803239165599697, 0.317085429804546, -2.6883582979439433,
    ];

    /// <summary>The same for <c>(X, 3 * X)</c>.</summary>
    private static readonly double[] TripledDecibels =
    [
        13.04028591574194, 21.324927691144772, 20.744782234758116, 24.54363975314218, 31.49214743632856,
        27.390766011249447, 11.77947787794347, 10.41716029883268, 6.979441713504012,
    ];

    private static Figure Of(double[] x, double[] y, SpectralOptions options = default, Action<LineSeries>? configure = null) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.Csd(x, y, options, configure)).Build();

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
        Assert.Equal(ComposedChartShapes.CsdByHand(false).ToSvg(), ComposedChartShapes.Named("csd").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.CsdByHand(true).ToSvg(), ComposedChartShapes.Named("csd").Build(true).ToSvg());
    }

    // ---- what is drawn --------------------------------------------------------------------------------------------------

    [Fact]
    public void TheChart_IsOneLineSeries_OfTheFrequenciesAndTheDecibelsOfTheMagnitude_AsMatplotlibPlotsThem()
    {
        var line = OnlySeriesOf(Of(X, Y, Sixteen));

        AssertClose(Frequencies, line.XData);
        AssertClose(Decibels, line.YData);
    }

    [Fact]
    public void ThreeTimesTheSignal_IsTenLogarithmsOfThreeMore_BecauseItIsTheMagnitudeNotItsSquare()
    {
        // |Pxy| of (X, 3X) is three times |Pxx|: 10·log10(3) = 4.77 dB above the power spectrum of X. Drawing the squared
        // magnitude would put it 9.54 dB above.
        var tripled = OnlySeriesOf(Of(X, [.. X.Select(v => 3 * v)], Sixteen));
        var power = OnlySeriesOf(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Psd(X, Sixteen)).Build());

        AssertClose(TripledDecibels, tripled.YData);
        for (int i = 0; i < power.YData.Length; i++)
        {
            Assert.Equal(10 * Math.Log10(3), tripled.YData[i] - power.YData[i], 1e-9);
        }
    }

    [Fact]
    public void TheCrossDensityOfASignalWithItself_IsItsPowerSpectrum()
    {
        var cross = OnlySeriesOf(Of(X, X, Sixteen));
        var power = OnlySeriesOf(Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Psd(X, Sixteen)).Build());

        AssertClose(power.YData, cross.YData);
    }

    [Fact]
    public void SwappingTheTwoSignals_DrawsTheSameMagnitude()
    {
        // conj(Csd(x, y)) = Csd(y, x): the same magnitude, so the same line.
        AssertClose(OnlySeriesOf(Of(X, Y, Sixteen)).YData, OnlySeriesOf(Of(Y, X, Sixteen)).YData);
    }

    // ---- no cross power is a gap, never minus infinity -----------------------------------------------------------------

    [Fact]
    public void ABinWithNoCrossPower_IsAGap_AndNeverMinusInfinity()
    {
        // A constant against a constant under a rectangular window: all the cross power is in the first bin, the others are exactly zero.
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 8, Window = SpectralWindow.Rectangular };
        double[] ones = [1, 1, 1, 1, 1, 1, 1, 1];
        var figure = Of(ones, [.. ones.Select(v => 2 * v)], options);

        var line = OnlySeriesOf(figure);

        Assert.Equal(10 * Math.Log10(16), line.YData[0], Tolerance);
        Assert.All(line.YData.Skip(1), value => Assert.True(double.IsNaN(value), $"expected a gap, got {value:R}"));
        Assert.DoesNotContain(double.NegativeInfinity, line.YData);
        Assert.DoesNotContain("Infinity", figure.ToSvg(), StringComparison.Ordinal);
        Assert.DoesNotContain("NaN", figure.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void ASignalOfZeros_HasNoCrossPowerAnywhere_SoNothingIsDrawn()
    {
        var figure = Of(new double[16], X[..16], new SpectralOptions { SegmentLength = 16 });

        Assert.All(OnlySeriesOf(figure).YData, value => Assert.True(double.IsNaN(value)));
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
    public void AMissingSample_InOneSegmentOfEitherSignal_LeavesThatSegmentOut_AndTheRestIsStillDrawn()
    {
        double[] x = [.. X, .. X], y = [.. Y, .. Y], gappyX = [.. x], gappyY = [.. y];
        gappyX[5] = double.NaN;    // the first of four segments
        gappyY[40] = double.NaN;   // the third
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 16 };

        var drawn = OnlySeriesOf(Of(gappyX, gappyY, options));
        var rest = OnlySeriesOf(Of([.. x[16..32], .. x[48..]], [.. y[16..32], .. y[48..]], options));

        Assert.Equal(9, drawn.YData.Count(double.IsFinite));
        AssertClose(rest.YData, drawn.YData);
    }

    // ---- the options reach the estimator ---------------------------------------------------------------------------------

    private static readonly Dictionary<string, SpectralOptions> OptionsThatChangeTheEstimate = new()
    {
        ["sample rate"] = new SpectralOptions { SampleRate = 1000, SegmentLength = 16, Overlap = 4 },
        ["window"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, Window = SpectralWindow.Blackman },
        ["detrend"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, Detrend = SpectralDetrend.Mean },
        ["overlap"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 8 },
        ["segment length"] = new SpectralOptions { SampleRate = 1, SegmentLength = 10 },
        ["padding"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, PadTo = 32 },
        ["centre frequency"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, CenterFrequency = 100 },
        ["scaling"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, Scaling = SpectralScaling.Spectrum },
    };

    public static TheoryData<string> OptionNames { get; } = new(OptionsThatChangeTheEstimate.Keys);

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void TheOptions_ReachTheEstimator(string name)
    {
        var options = OptionsThatChangeTheEstimate[name];
        CrossSpectralDensity estimate = Welch.Csd(X, Y, options);
        double[] expected = [.. estimate.Power.Select(power => 10 * Math.Log10(Complex.Abs(power)))];

        var line = OnlySeriesOf(Of(X, Y, options));

        Assert.True(estimate.Power.All(power => Complex.Abs(power) > 0), $"{name}: the test needs a signal with cross power in every bin");
        AssertClose(estimate.Frequencies, line.XData);
        AssertClose(expected, line.YData);
        Assert.False(line.XData.SequenceEqual(Frequencies) && line.YData.SequenceEqual(Decibels), $"{name}: the options drew nothing the defaults do not");
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
        var retitled = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Csd(X, Y, Sixteen).SetXLabel("Hz").SetYLabel("dB")).Build().SubPlots[0];

        Assert.Equal("Frequency", axes.XAxis.Label);
        Assert.Equal("Cross Spectrum Magnitude (dB)", axes.YAxis.Label);
        Assert.Equal("Hz", retitled.XAxis.Label);
        Assert.Equal("dB", retitled.YAxis.Label);
    }

    [Fact]
    public void TheVerb_SetsNoAxisLimits_AndKeepsTheCallersOwn()
    {
        var axes = Of(X, Y, Sixteen).SubPlots[0];
        var kept = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetYLim(-40, 40).Csd(X, Y, Sixteen)).Build().SubPlots[0];

        Assert.False(axes.HasExplicitLimit);
        Assert.Null(axes.XAxis.Min);
        Assert.Null(axes.XAxis.Max);
        Assert.Null(axes.YAxis.Min);
        Assert.Null(axes.YAxis.Max);
        Assert.Equal(-40.0, kept.YAxis.Min);
        Assert.Equal(40.0, kept.YAxis.Max);
    }

    // ---- the series -----------------------------------------------------------------------------------------------------

    [Fact]
    public void TheLine_IsOneLegendEntry()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Csd(X, Y, Sixteen, line => line.Label = "x with y")
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
            .Csd(X, Y, Sixteen)
            .Plot([0.0, 0.5], [0.0, 10.0])).Build();
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
            var same = ax.Csd(X, Y, Sixteen);
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
        var refused = Assert.Throws<ArgumentException>(() => Of(X, Y[..20], Sixteen));

        Assert.Equal("y", refused.ParamName);
        Assert.Contains("32", refused.Message, StringComparison.Ordinal);
        Assert.Contains("20", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASegmentLongerThanTheSignals_IsRefusedByTheNameOfTheOptions_AndTheDefaultIs256()
    {
        var refused = Assert.Throws<ArgumentException>(() => Of(X, Y));

        Assert.Equal("options", refused.ParamName);
        Assert.Contains("256", refused.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(16, 16, 0)]
    [InlineData(16, 20, 0)]
    [InlineData(16, 0, 12)]
    public void OptionsThatDoNotFitTheSignals_AreRefusedByTheNameOfTheOptions(int segment, int overlap, int padTo)
    {
        var options = new SpectralOptions { SegmentLength = segment, Overlap = overlap, PadTo = padTo };

        Assert.Equal("options", Assert.Throws<ArgumentException>(() => Of(X, Y, options)).ParamName);
    }

    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Throws<ArgumentException>(() => ax.Csd([], [], Sixteen));
            Assert.Throws<ArgumentException>(() => ax.Csd(X, Y));
            Assert.Throws<ArgumentException>(() => ax.Csd(X, Y[..5], Sixteen));
            Assert.Throws<ArgumentNullException>(() => ax.Csd(null!, Y, Sixteen));
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
        Assert.Null(axes.XAxis.Label);
        Assert.Null(axes.YAxis.Label);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.Csd(X, Y, Sixteen)).ParamName);
    }
}
