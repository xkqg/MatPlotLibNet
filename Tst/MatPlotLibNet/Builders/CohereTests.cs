// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;
using MatPlotLibNet.Tests.Rendering;
using static MatPlotLibNet.Tests.SpectralSignals;

namespace MatPlotLibNet.Tests.Builders;

/// <summary>The <c>Cohere</c> verb (matplotlib's <c>cohere</c>): the magnitude-squared coherence of two signals, |Pxy|² / (Pxx ·
/// Pyy), estimated by Welch's method (<c>Welch</c>, held in <c>WelchTests</c>) and drawn as ONE line of (frequency, coherence)
/// on the LINEAR scale between 0 and 1: no decibels. matplotlib refuses a signal shorter than two segments, because the
/// coherence of a single segment is 1 at every frequency whatever the signals are; so does the verb. The numbers marked
/// "matplotlib" were printed by matplotlib 3.11.2 for the two signals of <see cref="SpectralSignals"/> with <c>NFFT=16, Fs=1,
/// noverlap=4</c>.</summary>
public class CohereTests
{
    private const double Tolerance = 1e-9;

    private static Figure Of(double[] x, double[] y, SpectralOptions options = default, Action<LineSeries>? configure = null) =>
        TestFigures.OfOneVerb(ax => ax.Cohere(x, y, options, configure));

    // ---- what is drawn --------------------------------------------------------------------------------------------------

    [Fact]
    public void TheCoherence_IsLinear_BetweenZeroAndOne_NotInDecibels()
    {
        Assert.All(Of(X, Y, Sixteen).OnlySeries<LineSeries>().YData, value => Assert.InRange(value, 0.0, 1.0));
    }

    [Fact]
    public void ASignalWithItself_HasCoherenceOneAtEveryFrequency()
    {
        Enumerable.Repeat(1.0, 9).ToList().AssertCloseTo(Of(X, X, Sixteen).OnlySeries<LineSeries>().YData, Tolerance);
    }

    [Fact]
    public void ASignalWithAScaledCopyOfItself_HasCoherenceOne_TheScaleIsNoPartOfIt()
    {
        Enumerable.Repeat(1.0, 9).ToList().AssertCloseTo(Of(X, [.. X.Select(v => -3 * v)], Sixteen).OnlySeries<LineSeries>().YData, Tolerance);
    }

    [Fact]
    public void ABinWithNoPowerInEitherSignal_HasNoCoherence_AGapAndNeverZero()
    {
        // A constant under a rectangular window has power in the first bin only; the others are 0 / 0.
        var options = new SpectralOptions { SampleRate = 1, SegmentLength = 8, Window = SpectralWindow.Rectangular };
        double[] ones = [.. Enumerable.Repeat(1.0, 16)];
        var figure = Of(ones, ones, options);

        var line = figure.OnlySeries<LineSeries>();

        Assert.Equal(1.0, line.YData[0], Tolerance);
        Assert.All(line.YData.Skip(1), value => Assert.True(double.IsNaN(value), $"expected a gap, got {value:R}"));
        figure.ToSvg().AssertNoNonFiniteNumber();
    }

    // ---- what the verb refuses ----------------------------------------------------------------------------------------

    [Fact]
    public void ASignalOfExactlyTwoSegments_IsEnough()
    {
        Assert.Equal(9, Of(X, Y, Sixteen with { Overlap = 0 }).OnlySeries<LineSeries>().XData.Length);
    }
}
