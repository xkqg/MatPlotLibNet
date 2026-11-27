// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;
using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using static MatPlotLibNet.Tests.SpectralSignals;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The rules the spectral verbs of <see cref="SignalAxesExtensions"/> share, held once over a row per verb: the options a
/// caller gives reach the estimate (Welch's method for <c>Psd</c>, <c>Csd</c> and <c>Cohere</c>, the transform of the whole signal for
/// the magnitude, phase and angle spectra), a missing sample leaves out what it falls in and nothing is drawn of it, the defaults are
/// matplotlib's, and the axes are titled with what they measure. What each verb draws of its estimate, against the numbers matplotlib
/// printed, is held in the verb's own tests.</summary>
public class SignalAxesExtensionsTests
{
    private const double Tolerance = 1e-9;

    /// <summary>What a line drawn of an estimate is: its frequencies and its values.</summary>
    private readonly record struct DrawnLine(double[] X, double[] Y);

    /// <summary>A verb that estimates by Welch's method: its call, the line it draws of the estimate, whether it reads a second
    /// signal, and the title of its Y axis.</summary>
    private readonly record struct WelchVerb(Func<AxesBuilder, double[], double[], SpectralOptions, AxesBuilder> Call,
        Func<double[], double[], SpectralOptions, DrawnLine> Estimate, bool ReadsY, string YTitle);

    private static readonly Dictionary<string, WelchVerb> WelchVerbs = new()
    {
        ["psd"] = new((ax, x, _, o) => ax.Psd(x, o),
            (x, _, o) => Welch.Psd(x, o) is var d ? new(d.Frequencies, [.. d.Power.Select(p => 10 * Math.Log10(p))]) : default,
            false, "Power Spectral Density (dB/Hz)"),
        ["csd"] = new((ax, x, y, o) => ax.Csd(x, y, o),
            (x, y, o) => Welch.Csd(x, y, o) is var d ? new(d.Frequencies, [.. d.Power.Select(p => 10 * Math.Log10(Complex.Abs(p)))]) : default,
            true, "Cross Spectrum Magnitude (dB)"),
        ["cohere"] = new((ax, x, y, o) => ax.Cohere(x, y, o),
            (x, y, o) => Welch.Coherence(x, y, o) is var d ? new(d.Frequencies, d.Power) : default,
            true, "Coherence"),
    };

    /// <summary>A spectrum of the whole signal: its call, the values it draws of the transform, and the title of its Y axis.</summary>
    private readonly record struct SpectrumVerb(Func<AxesBuilder, double[], SpectrumOptions, AxesBuilder> Call,
        Func<SignalSpectrum, double[]> Values, string YTitle);

    private static readonly Dictionary<string, SpectrumVerb> SpectrumVerbs = new()
    {
        ["magnitude"] = new((ax, s, o) => ax.MagnitudeSpectrum(s, o), spectrum => spectrum.Magnitudes(), "Magnitude (energy)"),
        ["phase"] = new((ax, s, o) => ax.PhaseSpectrum(s, o), spectrum => spectrum.Phases(), "Phase (radians)"),
        ["angle"] = new((ax, s, o) => ax.AngleSpectrum(s, o), spectrum => spectrum.Angles(), "Angle (radians)"),
    };

    private static LineSeries Drawn(Func<AxesBuilder, AxesBuilder> verb) =>
        Assert.IsType<LineSeries>(Assert.Single(Plt.Create().AddSubPlot(1, 1, 1, ax => verb(ax)).Build().SubPlots[0].Series));

    private static Figure Figure(Func<AxesBuilder, AxesBuilder> verb) => Plt.Create().AddSubPlot(1, 1, 1, ax => verb(ax)).Build();

    // ---- the options reach the estimate ---------------------------------------------------------------------------------

    /// <summary>Options that each change a Welch estimate, by name: xunit carries the name, never the options.</summary>
    private static readonly Dictionary<string, SpectralOptions> WelchOptions = new()
    {
        ["sample rate"] = Sixteen with { SampleRate = 1000 },
        ["window"] = Sixteen with { Window = SpectralWindow.Blackman },
        ["detrend"] = Sixteen with { Detrend = SpectralDetrend.Mean },
        ["overlap"] = Sixteen with { Overlap = 8 },
        ["segment length"] = new SpectralOptions { SampleRate = 1, SegmentLength = 10 },
        ["padding"] = Sixteen with { PadTo = 32 },
        ["centre frequency"] = Sixteen with { CenterFrequency = 100 },
        ["scaling"] = Sixteen with { Scaling = SpectralScaling.Spectrum },
    };

    /// <summary>Every Welch verb with every option that changes what it draws; a coherence is a ratio, so the scaling cancels out
    /// of it.</summary>
    public static TheoryData<string, string> WelchVerbsAndOptions { get; } = new(
        from verb in WelchVerbs.Keys
        from option in WelchOptions.Keys
        where !(verb == "cohere" && option == "scaling")
        select (verb, option));

    [Theory]
    [MemberData(nameof(WelchVerbsAndOptions))]
    public void TheOptions_ReachTheWelchEstimate(string verb, string option)
    {
        var row = WelchVerbs[verb];
        var options = WelchOptions[option];
        var expected = row.Estimate(X, Y, options);
        var plain = Drawn(ax => row.Call(ax, X, Y, Sixteen));

        var line = Drawn(ax => row.Call(ax, X, Y, options));

        expected.X.AssertCloseTo(line.XData, Tolerance);
        expected.Y.AssertCloseTo(line.YData, Tolerance);
        Assert.False(line.XData.SequenceEqual(plain.XData) && line.YData.SequenceEqual(plain.YData), $"{verb}, {option}: the options drew nothing the defaults do not");
    }

    /// <summary>Options that each change a spectrum of the whole signal, by name.</summary>
    private static readonly Dictionary<string, SpectrumOptions> SpectrumOptionRows = new()
    {
        ["sample rate"] = new SpectrumOptions { SampleRate = 1000 },
        ["window"] = new SpectrumOptions { Window = SpectralWindow.Hamming },
        ["padding"] = new SpectrumOptions { PadTo = 16 },
        ["centre frequency"] = new SpectrumOptions { CenterFrequency = 100 },
    };

    public static TheoryData<string, string> SpectrumVerbsAndOptions { get; } = new(
        from verb in SpectrumVerbs.Keys
        from option in SpectrumOptionRows.Keys
        select (verb, option));

    [Theory]
    [MemberData(nameof(SpectrumVerbsAndOptions))]
    public void TheOptions_ReachTheTransform(string verb, string option)
    {
        var row = SpectrumVerbs[verb];
        var options = SpectrumOptionRows[option];
        var spectrum = Spectrum.Of(Eleven, options);
        var plain = Drawn(ax => row.Call(ax, Eleven, default));

        var line = Drawn(ax => row.Call(ax, Eleven, options));

        spectrum.Frequencies.AssertCloseTo(line.XData, Tolerance);
        row.Values(spectrum).AssertCloseTo(line.YData, Tolerance);
        Assert.False(line.XData.SequenceEqual(plain.XData) && line.YData.SequenceEqual(plain.YData), $"{verb}, {option}: the options drew nothing the defaults do not");
    }

    // ---- a missing sample ---------------------------------------------------------------------------------------------

    /// <summary>Each Welch verb with a gap in each signal it reads.</summary>
    public static TheoryData<string, bool> WelchVerbsAndSignals { get; } = new(
        from verb in WelchVerbs.Keys
        from inX in new[] { true, false }
        where inX || WelchVerbs[verb].ReadsY
        select (verb, inX));

    [Theory]
    [MemberData(nameof(WelchVerbsAndSignals))]
    public void AMissingSample_InEverySegment_LeavesNothingToDraw_ButTheFrequencies(string verb, bool inX)
    {
        double[] x = [.. X], y = [.. Y];
        (inX ? x : y)[13] = double.NaN;   // samples 12..15 are in both segments

        var figure = Figure(ax => WelchVerbs[verb].Call(ax, x, y, Sixteen));
        var line = Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));

        Frequencies.AssertCloseTo(line.XData, Tolerance);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        figure.ToSvg().AssertNoNonFiniteNumber();
    }

    public static TheoryData<string> WelchVerbNames { get; } = new(WelchVerbs.Keys);

    /// <summary>What matplotlib printed for each Welch verb over the two signals of <see cref="SpectralSignals"/> with <c>NFFT=16, Fs=1,
    /// noverlap=4</c>: the verb's values at the frequencies of <see cref="SpectralSignals.Frequencies"/>. <c>psd</c> is
    /// <c>10 * np.log10(psd(X, ...)[0])</c>, <c>csd</c> is <c>10 * np.log10(np.abs(csd(X, Y, ...)[0]))</c> and <c>cohere</c> is
    /// <c>cohere(X, Y, ...)[0]</c>, on the linear scale between 0 and 1.</summary>
    private static readonly Dictionary<string, double[]> MatplotlibPrints = new()
    {
        ["psd"] =
        [
            8.269073368545317, 16.553715143948146, 15.973569687561493, 19.77242720594556, 26.72093488913194,
            22.619553464052828, 7.008265330746849, 5.645947751636054, 2.2082291663073907,
        ],
        ["csd"] =
        [
            7.9074972286111045, 16.734114992703265, 13.418136274606553, 17.607414571221337, 24.778349562021084,
            20.080097917968416, 0.12803239165599697, 0.317085429804546, -2.6883582979439433,
        ],
        ["cohere"] =
        [
            0.7398273667532683, 0.7641232013324645, 0.8144301759255399, 0.9298316916693179, 0.9906896053191183,
            0.9891154864114439, 0.5066652647894018, 0.5965877425693659, 0.6127959243651422,
        ],
    };

    /// <summary>The chart of a Welch verb is ONE line of the frequencies and the values matplotlib prints for them, as it plots them.</summary>
    [Theory]
    [MemberData(nameof(WelchVerbNames))]
    public void TheChartOfAWelchVerb_IsOneLineOfTheFrequenciesAndTheValuesMatplotlibPrints(string verb) =>
        Drawn(ax => WelchVerbs[verb].Call(ax, X, Y, Sixteen)).AssertDrawnAs(Frequencies, MatplotlibPrints[verb], Tolerance);

    /// <summary>Four segments of 16 over 64 samples: a gap in the first segment of X (and, for a verb that reads it, in the third of Y)
    /// leaves exactly those out, so the line is the estimate of the others.</summary>
    [Theory]
    [MemberData(nameof(WelchVerbNames))]
    public void AMissingSample_InOneSegment_LeavesThatSegmentOut_AndTheRestIsStillDrawn(string verb)
    {
        var row = WelchVerbs[verb];
        double[] x = [.. X, .. X], y = [.. Y, .. Y], gappyX = [.. x], gappyY = [.. y];
        gappyX[5] = double.NaN;
        gappyY[40] = double.NaN;
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 16 };
        double[] Kept(double[] signal) => row.ReadsY ? [.. signal[16..32], .. signal[48..]] : signal[16..];

        var drawn = Drawn(ax => row.Call(ax, gappyX, gappyY, options));
        var rest = Drawn(ax => row.Call(ax, Kept(x), Kept(y), options));

        Assert.Equal(9, drawn.YData.Count(double.IsFinite));
        rest.YData.AssertCloseTo(drawn.YData, Tolerance);
    }

    public static TheoryData<string> SpectrumVerbNames { get; } = new(SpectrumVerbs.Keys);

    [Theory]
    [MemberData(nameof(SpectrumVerbNames))]
    public void AMissingSample_LeavesNothingOfTheSpectrumToDraw_ButTheFrequencies(string verb)
    {
        double[] gappy = [.. Eleven];
        gappy[4] = double.PositiveInfinity;

        var figure = Figure(ax => SpectrumVerbs[verb].Call(ax, gappy, default));
        var line = Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));

        Assert.Equal(6, line.XData.Length);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        figure.ToSvg().AssertNoNonFiniteNumber();
    }

    // ---- the defaults and the axes ------------------------------------------------------------------------------------

    /// <summary>Without options a Welch verb takes matplotlib's segments of 256 at a sample rate of 2: 129 frequencies up to 1.</summary>
    [Theory]
    [MemberData(nameof(WelchVerbNames))]
    public void WithoutOptions_AWelchEstimateIsMatplotlibs_ASegmentOf256AndASampleRateOf2(string verb)
    {
        double[] x = [.. Enumerable.Range(0, 512).Select(i => (double)(i * 7919 % 41 - 20))];
        double[] y = [.. Enumerable.Range(0, 512).Select(i => (double)(i * 104729 % 37 - 18))];

        var line = Drawn(ax => WelchVerbs[verb].Call(ax, x, y, default));

        Assert.Equal(129, line.XData.Length);
        Assert.Equal(0.0, line.XData[0]);
        Assert.Equal(1.0, line.XData[^1]);
    }

    /// <summary>Without options a spectrum takes matplotlib's sample rate of 2: its last frequency, the Nyquist one, is 1.</summary>
    [Theory]
    [MemberData(nameof(SpectrumVerbNames))]
    public void WithoutOptions_ASpectrumsSampleRateIsMatplotlibs2(string verb)
    {
        Assert.Equal(1.0, Drawn(ax => SpectrumVerbs[verb].Call(ax, [1, 1, 1, 1, 1, 1, 1, 1], default)).XData[^1]);
    }

    public static TheoryData<string> SpectralVerbNames { get; } = new(WelchVerbs.Keys.Concat(SpectrumVerbs.Keys));

    [Theory]
    [MemberData(nameof(SpectralVerbNames))]
    public void TheAxes_AreTitledWithWhatTheyMeasure(string verb)
    {
        var axes = WelchVerbs.TryGetValue(verb, out var welch)
            ? Figure(ax => welch.Call(ax, X, Y, Sixteen)).SubPlots[0]
            : Figure(ax => SpectrumVerbs[verb].Call(ax, Eleven, default)).SubPlots[0];

        Assert.Equal("Frequency", axes.XAxis.Label);
        Assert.Equal(welch.YTitle ?? SpectrumVerbs[verb].YTitle, axes.YAxis.Label);
    }
}
