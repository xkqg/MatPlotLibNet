// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;
using static MatPlotLibNet.Tests.SpectralSignals;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>MagnitudeSpectrum</c> verb (matplotlib's <c>magnitude_spectrum</c>): the size of each frequency in a whole
/// signal, one transform of it (<c>Spectrum</c>, held in <c>SpectrumTests</c>) drawn as ONE line of (frequency, |X| / Σw), on the
/// linear scale or in decibels (20·log10). This holds what the verb does with the spectrum: the two scales and their labels and a
/// zero magnitude in decibels as a gap; what the spectra share is held in <see cref="SignalAxesExtensionsTests"/>.
/// The numbers marked "matplotlib" were printed by matplotlib 3.11.2.</summary>
public class MagnitudeSpectrumTests
{
    private const double Tolerance = 1e-12;

    private static readonly double[] Ones8 = [1, 1, 1, 1, 1, 1, 1, 1];

    /// <summary>matplotlib's <c>magnitude_spectrum(ones(8))</c>: Hann by default, divided by the sum of the window.</summary>
    private static readonly double[] OnesMagnitude = [1, 0.5942915774844614, 0.06100063442391807, 0.015469346261788391, 0];

    private static Figure Of(double[] signal, SpectrumOptions options = default, MagnitudeScale scale = MagnitudeScale.Linear, Action<LineSeries>? configure = null) =>
        TestFigures.OfOneVerb(ax => ax.MagnitudeSpectrum(signal, options, scale, configure));

    // ---- what is drawn --------------------------------------------------------------------------------------------------

    [Fact]
    public void TheChart_IsOneLineSeries_OfTheFrequenciesAndTheMagnitudes_AsMatplotlibPlotsThem()
    {
        var line = Of(Ones8).OnlySeries<LineSeries>();

        (new double[] { 0, 0.25, 0.5, 0.75, 1 }).AssertCloseTo(line.XData, Tolerance);
        OnesMagnitude.AssertCloseTo(line.YData, Tolerance);
    }

    [Fact]
    public void InDecibels_EachMagnitudeIsTwentyTimesItsLogarithm()
    {
        double[] linear = Of(Eleven).OnlySeries<LineSeries>().YData;

        var line = Of(Eleven, scale: MagnitudeScale.Decibel).OnlySeries<LineSeries>();

        linear.Select(magnitude => 20 * Math.Log10(magnitude)).ToList().AssertCloseTo(line.YData, Tolerance);
    }

    [Fact]
    public void InDecibels_DoublingTheSignal_AddsTwentyTimesTheLogarithmOfTwo_NotTen()
    {
        var once = Of(Eleven, scale: MagnitudeScale.Decibel).OnlySeries<LineSeries>();
        var twice = Of([.. Eleven.Select(v => 2 * v)], scale: MagnitudeScale.Decibel).OnlySeries<LineSeries>();

        for (int i = 0; i < once.YData.Length; i++)
        {
            Assert.Equal(20 * Math.Log10(2), twice.YData[i] - once.YData[i], 1e-9);
        }
    }

    [Fact]
    public void InDecibels_TheFirstBinsOfOnes_AreMatplotlibs()
    {
        // matplotlib: magnitude_spectrum(ones(8), scale='dB'); its last bin is an exact zero there (-inf), a gap here.
        var line = Of(Ones8, scale: MagnitudeScale.Decibel).OnlySeries<LineSeries>();

        (new double[] { 0, -4.520008493538224, -24.293312963596456, -36.21056078599582 }).AssertCloseTo(line.YData[..4], Tolerance);
    }

    [Fact]
    public void InDecibels_AZeroMagnitude_IsAGap_AndNeverMinusInfinity()
    {
        // Two equal samples under the rectangular window: everything is in the first bin, the Nyquist bin is exactly zero.
        var figure = Of([1, 1], new SpectrumOptions { Window = SpectralWindow.Rectangular }, MagnitudeScale.Decibel);

        var line = figure.OnlySeries<LineSeries>();

        Assert.Equal(0.0, line.YData[0], Tolerance);
        Assert.True(double.IsNaN(line.YData[1]), $"expected a gap, got {line.YData[1]:R}");
        figure.ToSvg().AssertNoNonFiniteNumber();
    }

    [Fact]
    public void OnTheLinearScale_AZeroMagnitude_IsDrawnAsZero()
    {
        var line = Of([1, 1], new SpectrumOptions { Window = SpectralWindow.Rectangular }).OnlySeries<LineSeries>();

        Assert.Equal([1.0, 0.0], line.YData);
    }

    [Fact]
    public void TheScale_ChoosesTheLabelOfTheYAxis()
    {
        Assert.Equal("Magnitude (energy)", Of(Ones8).SubPlots[0].YAxis.Label);
        Assert.Equal("Magnitude (dB)", Of(Ones8, scale: MagnitudeScale.Decibel).SubPlots[0].YAxis.Label);
    }

    // ---- missing samples ----------------------------------------------------------------------------------------------

    /// <summary>A missing sample leaves nothing of the spectrum to draw on the decibel scale too (the linear scale is held with the
    /// other spectra in <see cref="SignalAxesExtensionsTests"/>).</summary>
    [Fact]
    public void AMissingSample_InDecibels_LeavesNothingToDraw_ButTheFrequencies()
    {
        double[] gappy = [.. Eleven];
        gappy[4] = double.NaN;

        var figure = Of(gappy, scale: MagnitudeScale.Decibel);
        var line = figure.OnlySeries<LineSeries>();

        Assert.Equal(6, line.XData.Length);
        Assert.All(line.YData, value => Assert.True(double.IsNaN(value)));
        figure.ToSvg().AssertNoNonFiniteNumber();
    }
}
