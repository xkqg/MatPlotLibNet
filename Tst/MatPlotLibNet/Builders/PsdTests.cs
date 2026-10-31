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

/// <summary>The <c>Psd</c> verb (matplotlib's <c>psd</c>): the power spectral density of a signal, estimated by Welch's method
/// (<c>Welch</c>, held in <c>WelchTests</c>) and drawn as ONE line of (frequency, 10·log10 of the density), so a reader sees the
/// power in decibels. This holds what the verb does with the estimate: the decibel rule (a bin with no power is a gap, never
/// minus infinity), the labels, the options reaching the estimator, and the refusals by the verb's own parameter names. The
/// numbers marked "matplotlib" were printed by matplotlib 3.11.2 for the signal below with <c>NFFT=16, Fs=1, noverlap=4</c>.</summary>
public class PsdTests
{
    private const double Tolerance = 1e-9;

    /// <summary>Thirty-two samples made by integer arithmetic: a tone of period four (10·wave) and a pseudo-random part.</summary>
    private static readonly double[] Signal =
    [
        -5, 15, 4, -7, 2, 11, 0, -11, -2, 7, -4, -15, 5, 14, 3, -8,
        1, 10, -1, -12, -3, 6, -5, -5, 4, 13, 2, -9, 0, 9, -2, -13,
    ];

    /// <summary>Segments of 16 samples that start 12 apart (an overlap of 4), so the signal holds two of them; samples 12 to 15
    /// belong to both.</summary>
    private static readonly SpectralOptions Sixteen = new() { SampleRate = 1, SegmentLength = 16, Overlap = 4 };

    /// <summary>matplotlib's frequencies for those options: k / 16 for k = 0 to 8.</summary>
    private static readonly double[] Frequencies = [0, 0.0625, 0.125, 0.1875, 0.25, 0.3125, 0.375, 0.4375, 0.5];

    /// <summary>matplotlib's <c>10 * np.log10(psd(Signal, NFFT=16, Fs=1, noverlap=4)[0])</c>.</summary>
    private static readonly double[] Decibels =
    [
        8.269073368545317, 16.553715143948146, 15.973569687561493, 19.77242720594556, 26.72093488913194,
        22.619553464052828, 7.008265330746849, 5.645947751636054, 2.2082291663073907,
    ];

    /// <summary>The same with <c>scale_by_freq=False</c>, which matplotlib labels "dB" instead of "dB/Hz".</summary>
    private static readonly double[] SpectrumDecibels =
    [
        -1.7309266314546825, 6.5537151439481445, 5.973569687561493, 9.77242720594556, 16.720934889131936,
        12.619553464052826, -2.9917346692531517, -4.354052248363946, -7.79177083369261,
    ];

    private static Figure Of(double[] signal, SpectralOptions options = default, Action<LineSeries>? configure = null) =>
        Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax.Psd(signal, options, configure)).Build();

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
        Assert.Equal(ComposedChartShapes.PsdByHand(false).ToSvg(), ComposedChartShapes.Named("psd").Build(false).ToSvg());
        Assert.Equal(ComposedChartShapes.PsdByHand(true).ToSvg(), ComposedChartShapes.Named("psd").Build(true).ToSvg());
    }

    // ---- what is drawn --------------------------------------------------------------------------------------------------

    [Fact]
    public void TheChart_IsOneLineSeries_OfTheFrequenciesAndTheirDecibels_AsMatplotlibPlotsThem()
    {
        var line = OnlySeriesOf(Of(Signal, Sixteen));

        AssertClose(Frequencies, line.XData);
        AssertClose(Decibels, line.YData);
    }

    [Fact]
    public void ADensityIsTenTimesTheLogarithmOfThePower_NotTwenty()
    {
        // Doubling the amplitude of the signal quadruples the power, which is 10·log10(4) = 6.0206 dB more at every frequency.
        var once = OnlySeriesOf(Of(Signal, Sixteen));
        var twice = OnlySeriesOf(Of([.. Signal.Select(v => 2 * v)], Sixteen));

        for (int i = 0; i < once.YData.Length; i++)
        {
            Assert.Equal(10 * Math.Log10(4), twice.YData[i] - once.YData[i], 1e-9);
        }
    }

    [Fact]
    public void TheScalingOfTheOptions_ChoosesTheUnitOfTheLabel_AndTheLevelOfTheLine()
    {
        var figure = Of(Signal, Sixteen with { Scaling = SpectralScaling.Spectrum });

        AssertClose(SpectrumDecibels, OnlySeriesOf(figure).YData);
        Assert.Equal("Power Spectral Density (dB)", figure.SubPlots[0].YAxis.Label);
        Assert.Equal("Power Spectral Density (dB/Hz)", Of(Signal, Sixteen).SubPlots[0].YAxis.Label);
    }

    // ---- no power is a gap, never minus infinity -----------------------------------------------------------------------

    [Fact]
    public void ABinWithNoPower_IsAGap_AndNeverMinusInfinity()
    {
        // The rectangular window over a constant signal puts all the power in the first bin: the others are exactly zero.
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 8, Window = SpectralWindow.Rectangular };
        var figure = Of([1, 1, 1, 1, 1, 1, 1, 1], options);

        var line = OnlySeriesOf(figure);

        Assert.Equal(10 * Math.Log10(8), line.YData[0], Tolerance);
        Assert.All(line.YData.Skip(1), value => Assert.True(double.IsNaN(value), $"expected a gap, got {value:R}"));
        Assert.DoesNotContain(double.NegativeInfinity, line.YData);
        Assert.DoesNotContain("Infinity", figure.ToSvg(), StringComparison.Ordinal);
        Assert.DoesNotContain("NaN", figure.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void ASignalOfZeros_HasNoPowerAnywhere_SoNothingIsDrawn()
    {
        var figure = Of(new double[16], new SpectralOptions { SegmentLength = 16 });

        var line = OnlySeriesOf(figure);

        Assert.Equal(9, line.XData.Length);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        Assert.DoesNotContain("NaN", figure.ToSvg(), StringComparison.Ordinal);
    }

    // ---- missing samples ----------------------------------------------------------------------------------------------

    [Fact]
    public void AMissingSample_InEverySegment_LeavesNothingToDraw_ButTheFrequencies()
    {
        double[] gappy = [.. Signal];
        gappy[13] = double.NaN;   // samples 12..15 are in both segments

        var figure = Of(gappy, Sixteen);
        var line = OnlySeriesOf(figure);

        AssertClose(Frequencies, line.XData);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        Assert.DoesNotContain("NaN", figure.ToSvg(), StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingSample_InOneSegment_LeavesThatSegmentOut_AndTheRestIsStillDrawn()
    {
        // Four segments of 16 over 64 samples: the gap at sample 5 is in the first only, so the line is the estimate of the other three.
        double[] longer = [.. Signal, .. Signal];
        double[] gappy = [.. longer];
        gappy[5] = double.NaN;
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 16 };

        var drawn = OnlySeriesOf(Of(gappy, options));
        var rest = OnlySeriesOf(Of(longer[16..], options));

        Assert.Equal(9, drawn.YData.Count(double.IsFinite));
        AssertClose(rest.YData, drawn.YData);
    }

    [Fact]
    public void AnInfinity_IsAMissingSampleToo()
    {
        double[] infinite = [.. Signal];
        infinite[13] = double.PositiveInfinity;

        Assert.All(OnlySeriesOf(Of(infinite, Sixteen)).YData, value => Assert.True(double.IsNaN(value)));
    }

    // ---- the options reach the estimator ---------------------------------------------------------------------------------

    /// <summary>Options that each change the estimate, by name: xunit carries the name, never the options (they hold functions).</summary>
    private static readonly Dictionary<string, SpectralOptions> OptionsThatChangeTheEstimate = new()
    {
        ["sample rate"] = new SpectralOptions { SampleRate = 1000, SegmentLength = 16, Overlap = 4 },
        ["window"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, Window = SpectralWindow.Hamming },
        ["detrend"] = new SpectralOptions { SampleRate = 1, SegmentLength = 16, Overlap = 4, Detrend = SpectralDetrend.Linear },
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
        SpectralDensity estimate = Welch.Psd(Signal, options);
        double[] expected = [.. estimate.Power.Select(power => 10 * Math.Log10(power))];

        var line = OnlySeriesOf(Of(Signal, options));

        Assert.True(estimate.Power.All(power => power > 0), $"{name}: the test needs a signal with power in every bin");
        AssertClose(estimate.Frequencies, line.XData);
        AssertClose(expected, line.YData);
        Assert.False(line.XData.SequenceEqual(Frequencies) && line.YData.SequenceEqual(Decibels), $"{name}: the options drew nothing the defaults do not");
    }

    [Fact]
    public void WithoutOptions_TheDefaultsAreMatplotlibs_ASegmentOf256AndASampleRateOf2()
    {
        double[] long256 = [.. Enumerable.Range(0, 512).Select(i => (double)(i * 7919 % 41 - 20))];

        var line = OnlySeriesOf(Of(long256));

        Assert.Equal(129, line.XData.Length);
        Assert.Equal(0.0, line.XData[0]);
        Assert.Equal(1.0, line.XData[^1]);   // the Nyquist frequency of a sample rate of 2
    }

    // ---- the axes -------------------------------------------------------------------------------------------------------

    [Fact]
    public void TheAxes_AreTitledWithWhatTheyMeasure_AndATitleSetAfterTheCallWins()
    {
        var axes = Of(Signal, Sixteen).SubPlots[0];
        var retitled = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Psd(Signal, Sixteen).SetXLabel("Frequency (Hz)").SetYLabel("Power (dB)")).Build().SubPlots[0];

        Assert.Equal("Frequency", axes.XAxis.Label);
        Assert.Equal("Power Spectral Density (dB/Hz)", axes.YAxis.Label);
        Assert.Equal("Frequency (Hz)", retitled.XAxis.Label);
        Assert.Equal("Power (dB)", retitled.YAxis.Label);
    }

    [Fact]
    public void TheVerb_SetsNoAxisLimits_AndKeepsTheCallersOwn()
    {
        var axes = Of(Signal, Sixteen).SubPlots[0];
        var kept = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.SetYLim(-40, 40).Psd(Signal, Sixteen)).Build().SubPlots[0];

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
            .Psd(Signal, Sixteen, line => line.Label = "signal")
            .WithLegend()).Build();
        figure.EnableLegendToggle = true;

        string svg = figure.ToSvg();

        Assert.Single(Regex.Matches(svg, "data-legend-index="));
        Assert.Contains("signal", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLine_IsOneColourSlot_TheNextSeriesTakesTheNextColour()
    {
        var figure = Plt.Create().WithSize(800, 400).AddSubPlot(1, 1, 1, ax => ax
            .Psd(Signal, Sixteen)
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
        var line = OnlySeriesOf(Of(Signal, Sixteen, series => { series.Color = Colors.Red; series.LineWidth = 4; series.LineStyle = LineStyle.Dashed; }));

        Assert.Equal(Colors.Red, line.Color);
        Assert.Equal(4.0, line.LineWidth);
        Assert.Equal(LineStyle.Dashed, line.LineStyle);
    }

    [Fact]
    public void TheCallback_RunsOnceOnTheSeriesTheVerbAdded()
    {
        var seen = new List<LineSeries>();

        var figure = Of(Signal, Sixteen, seen.Add);

        Assert.Same(OnlySeriesOf(figure), Assert.Single(seen));
    }

    [Fact]
    public void TheVerb_ChainsWithTheRestOfTheBuilder()
    {
        var builder = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            var same = ax.Psd(Signal, Sixteen);
            Assert.Same(ax, same);
        });

        Assert.Single(builder.Build().SubPlots[0].Series);
    }

    [Fact]
    public void TheChart_SurvivesTheJson_AndDrawsTheSamePicture()
    {
        var figure = Of(Signal, Sixteen, line => line.Label = "signal");
        double[] gappy = [.. Signal];
        gappy[13] = double.NaN;
        var gap = Of(gappy, Sixteen);

        Assert.Equal(figure.ToSvg(), ChartSerializerDefault.Current.FromJson(figure.ToJson()).ToSvg());
        Assert.Equal(gap.ToSvg(), ChartSerializerDefault.Current.FromJson(gap.ToJson()).ToSvg());
        Assert.Contains("\"NaN\"", gap.ToJson(), StringComparison.Ordinal);
    }

    // ---- what the verb refuses ----------------------------------------------------------------------------------------

    [Fact]
    public void ANullSignal_IsRefusedByTheNameOfTheSignal()
    {
        Assert.Equal("signal", Assert.Throws<ArgumentNullException>(() => Of(null!, Sixteen)).ParamName);
    }

    [Fact]
    public void AnEmptySignal_IsRefusedByTheNameOfTheSignal()
    {
        var refused = Assert.Throws<ArgumentException>(() => Of([], Sixteen));

        Assert.Equal("signal", refused.ParamName);
        Assert.Contains("no samples", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASegmentLongerThanTheSignal_IsRefusedByTheNameOfTheOptions_AndTheDefaultIs256()
    {
        var refused = Assert.Throws<ArgumentException>(() => Of(Signal));   // 32 samples against the default segment of 256

        Assert.Equal("options", refused.ParamName);
        Assert.Contains("256", refused.Message, StringComparison.Ordinal);
        Assert.Contains("32", refused.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(16, 16, 0)]   // an overlap equal to the segment
    [InlineData(16, 20, 0)]   // an overlap above it
    [InlineData(16, 0, 12)]   // a padded length shorter than the segment
    public void OptionsThatDoNotFitTheSignal_AreRefusedByTheNameOfTheOptions(int segment, int overlap, int padTo)
    {
        var options = new SpectralOptions { SegmentLength = segment, Overlap = overlap, PadTo = padTo };

        Assert.Equal("options", Assert.Throws<ArgumentException>(() => Of(Signal, options)).ParamName);
    }

    [Fact]
    public void ARefusedCall_LeavesTheAxesAsItWas()
    {
        var axes = Plt.Create().AddSubPlot(1, 1, 1, ax =>
        {
            Assert.Throws<ArgumentException>(() => ax.Psd([], Sixteen));
            Assert.Throws<ArgumentException>(() => ax.Psd(Signal));
            Assert.Throws<ArgumentNullException>(() => ax.Psd(null!, Sixteen));
        }).Build().SubPlots[0];

        Assert.Empty(axes.Series);
        Assert.Null(axes.XAxis.Label);
        Assert.Null(axes.YAxis.Label);
    }

    [Fact]
    public void ANullBuilder_IsRefused()
    {
        AxesBuilder axes = null!;

        Assert.Equal("axes", Assert.Throws<ArgumentNullException>(() => axes.Psd(Signal, Sixteen)).ParamName);
    }
}
