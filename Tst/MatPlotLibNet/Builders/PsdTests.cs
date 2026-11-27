// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;
using static MatPlotLibNet.Tests.SpectralSignals;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Psd</c> verb (matplotlib's <c>psd</c>): the power spectral density of a signal, estimated by Welch's method
/// (<c>Welch</c>, held in <c>WelchTests</c>) and drawn as ONE line of (frequency, 10·log10 of the density), so a reader sees the
/// power in decibels. This holds what the verb does with the estimate: the decibel rule (a bin with no power is a gap, never
/// minus infinity) and the label the scaling chooses; what the spectral verbs share (the options reaching the estimate, a missing
/// sample, the defaults, the titles) is held in <see cref="SignalAxesExtensionsTests"/>, the refusals in the verb's shape of
/// <c>ComposedChartShapes</c>. The numbers marked "matplotlib" were printed by matplotlib 3.11.2 for
/// <see cref="SpectralSignals.X"/> with <c>NFFT=16, Fs=1, noverlap=4</c>.</summary>
public class PsdTests
{
    private const double Tolerance = 1e-9;

    /// <summary>The same with <c>scale_by_freq=False</c>, which matplotlib labels "dB" instead of "dB/Hz".</summary>
    private static readonly double[] SpectrumDecibels =
    [
        -1.7309266314546825, 6.5537151439481445, 5.973569687561493, 9.77242720594556, 16.720934889131936,
        12.619553464052826, -2.9917346692531517, -4.354052248363946, -7.79177083369261,
    ];

    private static Figure Of(double[] signal, SpectralOptions options = default, Action<LineSeries>? configure = null) =>
        TestFigures.OfOneVerb(ax => ax.Psd(signal, options, configure));

    // ---- what is drawn --------------------------------------------------------------------------------------------------

    [Fact]
    public void ADensityIsTenTimesTheLogarithmOfThePower_NotTwenty()
    {
        // Doubling the amplitude of the signal quadruples the power, which is 10·log10(4) = 6.0206 dB more at every frequency.
        var once = Of(X, Sixteen).OnlySeries<LineSeries>();
        var twice = Of([.. X.Select(v => 2 * v)], Sixteen).OnlySeries<LineSeries>();

        for (int i = 0; i < once.YData.Length; i++)
        {
            Assert.Equal(10 * Math.Log10(4), twice.YData[i] - once.YData[i], 1e-9);
        }
    }

    [Fact]
    public void TheScalingOfTheOptions_ChoosesTheUnitOfTheLabel_AndTheLevelOfTheLine()
    {
        var figure = Of(X, Sixteen with { Scaling = SpectralScaling.Spectrum });

        SpectrumDecibels.AssertCloseTo(figure.OnlySeries<LineSeries>().YData, Tolerance);
        Assert.Equal("Power Spectral Density (dB)", figure.SubPlots[0].YAxis.Label);
        Assert.Equal("Power Spectral Density (dB/Hz)", Of(X, Sixteen).SubPlots[0].YAxis.Label);
    }

    // ---- no power is a gap, never minus infinity -----------------------------------------------------------------------

    [Fact]
    public void ABinWithNoPower_IsAGap_AndNeverMinusInfinity()
    {
        // The rectangular window over a constant signal puts all the power in the first bin: the others are exactly zero.
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 8, Window = SpectralWindow.Rectangular };
        var figure = Of([1, 1, 1, 1, 1, 1, 1, 1], options);

        var line = figure.OnlySeries<LineSeries>();

        Assert.Equal(10 * Math.Log10(8), line.YData[0], Tolerance);
        Assert.All(line.YData.Skip(1), value => Assert.True(double.IsNaN(value), $"expected a gap, got {value:R}"));
        Assert.DoesNotContain(double.NegativeInfinity, line.YData);
        figure.ToSvg().AssertNoNonFiniteNumber();
    }

    [Fact]
    public void ASignalOfZeros_HasNoPowerAnywhere_SoNothingIsDrawn()
    {
        var figure = Of(new double[16], new SpectralOptions { SegmentLength = 16 });

        var line = figure.OnlySeries<LineSeries>();

        Assert.Equal(9, line.XData.Length);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        figure.ToSvg().AssertNoNonFiniteNumber();
    }

    // ---- missing samples ----------------------------------------------------------------------------------------------

    [Fact]
    public void AnInfinity_IsAMissingSampleToo()
    {
        double[] infinite = [.. X];
        infinite[13] = double.PositiveInfinity;

        Assert.All(Of(infinite, Sixteen).OnlySeries<LineSeries>().YData, value => Assert.True(double.IsNaN(value)));
    }
}
