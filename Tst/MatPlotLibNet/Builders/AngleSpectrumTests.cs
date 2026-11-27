// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Tests.Rendering;
using static MatPlotLibNet.Tests.SpectralSignals;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>AngleSpectrum</c> verb (matplotlib's <c>angle_spectrum</c>): the angle of each frequency in a whole signal,
/// WRAPPED into (−π, π] (numpy's <c>angle</c>), as the transform gives it. Drawn as ONE line of (frequency, radians). The numbers
/// marked "matplotlib" were printed by matplotlib 3.11.2.</summary>
public class AngleSpectrumTests
{
    private const double Tolerance = 1e-12;

    private static Figure Of(double[] signal, SpectrumOptions options = default, Action<LineSeries>? configure = null) =>
        TestFigures.OfOneVerb(ax => ax.AngleSpectrum(signal, options, configure));

    [Fact]
    public void TheAngle_StaysWithinMinusPiAndPi_AndANegativeRealBinIsPlusPi()
    {
        // matplotlib: angle_spectrum(x, window=np.hamming(11), pad_to=14, Fs=1) ends at the Nyquist bin, real and negative: +pi.
        var line = Of(Eleven, new SpectrumOptions { SampleRate = 1, Window = SpectralWindow.Hamming, PadTo = 14 }).OnlySeries<LineSeries>();

        (new double[] { 0, -2.163700435605824, 1.6126056059677956, -0.7647854269807199, 3.095851653265369, 0.704217416526966, -1.4985695578332388, Math.PI }).AssertCloseTo(line.YData, Tolerance);
        Assert.All(line.YData, angle => Assert.InRange(angle, -Math.PI, Math.PI));
        Assert.Equal(Math.PI, line.YData[^1]);
    }
}
