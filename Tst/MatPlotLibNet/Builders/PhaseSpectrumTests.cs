// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Tests.Rendering;
using static MatPlotLibNet.Tests.SpectralSignals;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>PhaseSpectrum</c> verb (matplotlib's <c>phase_spectrum</c>): the phase of each frequency in a whole signal,
/// UNWRAPPED along the frequencies (numpy's <c>unwrap</c>), so a delay reads as a straight line instead of a saw. Drawn as ONE
/// line of (frequency, radians). The numbers marked "matplotlib" were printed by matplotlib 3.11.2.</summary>
public class PhaseSpectrumTests
{
    private const double Tolerance = 1e-12;

    private static Figure Of(double[] signal, SpectrumOptions options = default, Action<LineSeries>? configure = null) =>
        TestFigures.OfOneVerb(ax => ax.PhaseSpectrum(signal, options, configure));

    [Fact]
    public void ThePhase_IsNotWrapped_ItRunsPastMinusPi()
    {
        var line = Of(SpectralSignals.Impulse7).OnlySeries<LineSeries>();

        Assert.True(line.YData[^1] < -Math.PI, $"the phase {line.YData[^1]:R} was wrapped");
        Spectrum.Of(SpectralSignals.Impulse7, default).Phases().AssertCloseTo(line.YData, Tolerance);
    }

    [Fact]
    public void WithMatplotlibsWindowPaddingAndCentre_ThePhaseIsMatplotlibs()
    {
        var options = new SpectrumOptions { SampleRate = 1, Window = SpectralWindow.Hamming, PadTo = 14, CenterFrequency = 5 };

        var line = Of(Eleven, options).OnlySeries<LineSeries>();

        // matplotlib: phase_spectrum(x, window=np.hamming(11), pad_to=14, Fs=1), with Fc = 5 added to the frequencies.
        Enumerable.Range(0, 8).Select(k => 5 + k / 14.0).ToList().AssertCloseTo(line.XData, Tolerance);
        (new double[] { 0, -2.163700435605824, -4.670579701211791, -7.047970734160306, -9.470518961093804, -11.862153197832207, -14.064940172192411, -15.707963267948966 }).AssertCloseTo(line.YData, Tolerance);
    }
}
