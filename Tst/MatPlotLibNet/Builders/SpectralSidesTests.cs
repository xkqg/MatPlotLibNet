// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Serialization;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>sides</c> of matplotlib's spectrum verbs, reached from the verbs an application calls: <c>Psd</c>, <c>Csd</c> and
/// <c>Cohere</c> through <see cref="SpectralOptions.Sides"/>, <c>MagnitudeSpectrum</c>, <c>PhaseSpectrum</c> and
/// <c>AngleSpectrum</c> through <see cref="SpectrumOptions.Sides"/>. A two-sided line starts at minus half the sample rate (matplotlib 3.11.2:
/// <c>psd(x, NFFT=64, Fs=2, sides='twosided')</c> has 64 frequencies, -1.0 to 0.96875), and the line is one series, so the JSON
/// of the figure carries it and reads back to the same picture. The vectors themselves are held in <c>WelchTests</c> and
/// <c>SpectrumTests</c>.</summary>
public class SpectralSidesTests
{
    private static readonly double[] Signal = [.. Enumerable.Range(0, 128).Select(i => ((i * 37 + 11) % 23) - 11.0)];

    private static readonly double[] Other = [.. Enumerable.Range(0, 128).Select(i => ((i * 17 + 5) % 19) - 9.0 + 0.25 * (((i * 37 + 11) % 23) - 11.0))];

    private static LineSeries Line(Action<AxesBuilder> verb)
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, verb).Build();
        return Assert.IsType<LineSeries>(Assert.Single(figure.SubPlots[0].Series));
    }

    private static readonly SpectralOptions Welch64 = new() { SegmentLength = 64, Sides = SpectralSides.TwoSided };

    private static readonly SpectrumOptions Whole = new() { Sides = SpectralSides.TwoSided };

    [Fact]
    public void Psd_TwoSided_StartsAtMinusHalfTheSampleRate_AndHoldsEveryFrequencyOfTheTransform()
    {
        var line = Line(ax => ax.Psd(Signal, Welch64));

        Assert.Equal(64, line.XData.Length);
        Assert.Equal(-1.0, line.XData[0], 1e-12);
        Assert.Equal(0.96875, line.XData[^1], 1e-12);
        Assert.Equal(64, line.YData.Length);
    }

    [Fact]
    public void Psd_TwoSided_IsSymmetricAboutZero_AndTheOneSidedLineIsItsUpperHalfRaisedByThreeDecibels()
    {
        var both = Line(ax => ax.Psd(Signal, Welch64));
        var one = Line(ax => ax.Psd(Signal, Welch64 with { Sides = SpectralSides.Default }));

        Assert.Equal(33, one.XData.Length);
        for (int k = 1; k < 32; k++)
        {
            Assert.Equal(both.YData[32 + k], both.YData[32 - k], 1e-9);
            Assert.Equal(10 * Math.Log10(2), one.YData[k] - both.YData[32 + k], 1e-9);
        }
    }

    [Fact]
    public void Csd_TwoSided_HasOneMagnitudeAtAFrequencyAndAtItsNegative()
    {
        var line = Line(ax => ax.Csd(Signal, Other, Welch64));

        Assert.Equal(64, line.XData.Length);
        Assert.Equal(-1.0, line.XData[0], 1e-12);
        for (int k = 1; k < 32; k++) Assert.Equal(line.YData[32 + k], line.YData[32 - k], 1e-9);
    }

    [Fact]
    public void Cohere_TwoSided_IsTheSameOnBothHalves_AsTheOneSidedCoherence()
    {
        var both = Line(ax => ax.Cohere(Signal, Other, Welch64));
        var one = Line(ax => ax.Cohere(Signal, Other, Welch64 with { Sides = SpectralSides.OneSided }));

        Assert.Equal(64, both.XData.Length);
        for (int k = 0; k <= 32; k++)
        {
            int at = k == 32 ? 0 : 32 + k;
            Assert.Equal(one.YData[k], both.YData[at], 1e-9);
        }
    }

    [Fact]
    public void MagnitudeSpectrum_TwoSided_RunsFromMinusHalfTheRate_AndIsEvenAboutZero()
    {
        var line = Line(ax => ax.MagnitudeSpectrum(Signal, Whole));

        Assert.Equal(128, line.XData.Length);
        Assert.Equal(-1.0, line.XData[0], 1e-12);
        Assert.Equal(0.984375, line.XData[^1], 1e-12);
        for (int k = 1; k < 64; k++) Assert.Equal(line.YData[64 + k], line.YData[64 - k], 1e-12);
    }

    [Fact]
    public void AngleSpectrum_TwoSided_IsOddAboutZero()
    {
        var line = Line(ax => ax.AngleSpectrum(Signal, Whole));

        Assert.Equal(128, line.XData.Length);
        for (int k = 1; k < 64; k++) Assert.Equal(line.YData[64 + k], -line.YData[64 - k], 1e-9);
    }

    [Fact]
    public void PhaseSpectrum_TwoSided_HasAPointForEveryFrequencyOfTheTransform()
    {
        var line = Line(ax => ax.PhaseSpectrum(Signal, Whole with { PadTo = 200 }));

        Assert.Equal(200, line.XData.Length);
        Assert.Equal(-1.0, line.XData[0], 1e-12);
        Assert.Equal(0.99, line.XData[^1], 1e-12);
    }

    [Fact]
    public void ATwoSidedLine_IsOneSeries_ThatTheJsonCarriesAndReadsBackTheSame()
    {
        var figure = Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Psd(Signal, Welch64)).Build();

        var read = figure.RoundTripped();

        var line = Assert.IsType<LineSeries>(Assert.Single(read.SubPlots[0].Series));
        Assert.Equal(-1.0, line.XData[0], 1e-12);
        Assert.Equal(64, line.XData.Length);
        Assert.Equal(figure.ToSvg(), read.ToSvg());
    }
}
