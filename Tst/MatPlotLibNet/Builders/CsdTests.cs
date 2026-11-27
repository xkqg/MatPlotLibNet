// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;
using static MatPlotLibNet.Tests.SpectralSignals;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Csd</c> verb (matplotlib's <c>csd</c>): the cross spectral density of two signals, estimated by Welch's method
/// (<c>Welch</c>, held in <c>WelchTests</c>) and drawn as ONE line of (frequency, 10·log10 of the MAGNITUDE of the cross
/// density), as matplotlib draws it: the magnitude, not its square. The numbers marked "matplotlib" were printed by matplotlib
/// 3.11.2 for the two signals of <see cref="SpectralSignals"/> with <c>NFFT=16, Fs=1, noverlap=4</c>.</summary>
public class CsdTests
{
    private const double Tolerance = 1e-9;

    /// <summary>The same for <c>(X, 3 * X)</c>.</summary>
    private static readonly double[] TripledDecibels =
    [
        13.04028591574194, 21.324927691144772, 20.744782234758116, 24.54363975314218, 31.49214743632856,
        27.390766011249447, 11.77947787794347, 10.41716029883268, 6.979441713504012,
    ];

    private static Figure Of(double[] x, double[] y, SpectralOptions options = default, Action<LineSeries>? configure = null) =>
        TestFigures.OfOneVerb(ax => ax.Csd(x, y, options, configure));

    // ---- what is drawn --------------------------------------------------------------------------------------------------

    [Fact]
    public void ThreeTimesTheSignal_IsTenLogarithmsOfThreeMore_BecauseItIsTheMagnitudeNotItsSquare()
    {
        // |Pxy| of (X, 3X) is three times |Pxx|: 10·log10(3) = 4.77 dB above the power spectrum of X. Drawing the squared
        // magnitude would put it 9.54 dB above.
        var tripled = Of(X, [.. X.Select(v => 3 * v)], Sixteen).OnlySeries<LineSeries>();
        var power = (Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Psd(X, Sixteen)).Build()).OnlySeries<LineSeries>();

        TripledDecibels.AssertCloseTo(tripled.YData, Tolerance);
        for (int i = 0; i < power.YData.Length; i++)
        {
            Assert.Equal(10 * Math.Log10(3), tripled.YData[i] - power.YData[i], 1e-9);
        }
    }

    [Fact]
    public void TheCrossDensityOfASignalWithItself_IsItsPowerSpectrum()
    {
        var cross = Of(X, X, Sixteen).OnlySeries<LineSeries>();
        var power = (Plt.Create().AddSubPlot(1, 1, 1, ax => ax.Psd(X, Sixteen)).Build()).OnlySeries<LineSeries>();

        power.YData.AssertCloseTo(cross.YData, Tolerance);
    }

    [Fact]
    public void SwappingTheTwoSignals_DrawsTheSameMagnitude()
    {
        // conj(Csd(x, y)) = Csd(y, x): the same magnitude, so the same line.
        Of(X, Y, Sixteen).OnlySeries<LineSeries>().YData.AssertCloseTo(Of(Y, X, Sixteen).OnlySeries<LineSeries>().YData, Tolerance);
    }

    // ---- no cross power is a gap, never minus infinity -----------------------------------------------------------------

    [Fact]
    public void ABinWithNoCrossPower_IsAGap_AndNeverMinusInfinity()
    {
        // A constant against a constant under a rectangular window: all the cross power is in the first bin, the others are exactly zero.
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 8, Window = SpectralWindow.Rectangular };
        double[] ones = [1, 1, 1, 1, 1, 1, 1, 1];
        var figure = Of(ones, [.. ones.Select(v => 2 * v)], options);

        var line = figure.OnlySeries<LineSeries>();

        Assert.Equal(10 * Math.Log10(16), line.YData[0], Tolerance);
        Assert.All(line.YData.Skip(1), value => Assert.True(double.IsNaN(value), $"expected a gap, got {value:R}"));
        Assert.DoesNotContain(double.NegativeInfinity, line.YData);
        figure.ToSvg().AssertNoNonFiniteNumber();
    }

    [Fact]
    public void ASignalOfZeros_HasNoCrossPowerAnywhere_SoNothingIsDrawn()
    {
        var figure = Of(new double[16], X[..16], new SpectralOptions { SegmentLength = 16 });

        Assert.All(figure.OnlySeries<LineSeries>().YData, value => Assert.True(double.IsNaN(value)));
        figure.ToSvg().AssertNoNonFiniteNumber();
    }
}
