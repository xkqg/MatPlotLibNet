// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet.Tests.Numerics;

/// <summary>What the two spectral options made from a function (a window, a detrend) refuse alike: a function that is not there.</summary>
public class SpectralFunctionTests
{
    public static TheoryData<Action> OfAFunction => new()
    {
        () => SpectralDetrend.Of(null!),
        () => SpectralWindow.Of(null!),
    };

    [Theory]
    [MemberData(nameof(OfAFunction))]
    public void Of_NullFunction_Refuses(Action make) => Assert.Throws<ArgumentNullException>(make);

    /// <summary>A centre frequency shifts every frequency of a two-sided spectrum and of a two-sided density alike: the same eight samples,
    /// rectangular window, the centre at 100.</summary>
    public static TheoryData<Func<IEnumerable<double>>, double[], double> CentredFrequencies => new()
    {
        {
            () => Spectrum.Of([1, 0.5, -1, 2, 0.25, 1.5, -0.75, 1.0],
                new SpectrumOptions { SampleRate = 8, Window = SpectralWindow.Rectangular, Sides = SpectralSides.TwoSided, CenterFrequency = 100 }).Frequencies,
            [96, 97, 98, 99, 100, 101, 102, 103], 1e-12
        },
        {
            () => Welch.Psd([1, 0, -1, 0, 1, 0, -1, 0],
                new SpectralOptions { SegmentLength = 8, SampleRate = 1, Window = SpectralWindow.Rectangular, Sides = SpectralSides.TwoSided, CenterFrequency = 100 }).Frequencies,
            [99.5, 99.625, 99.75, 99.875, 100, 100.125, 100.25, 100.375], 1e-9
        },
    };

    [Theory]
    [MemberData(nameof(CentredFrequencies))]
    public void TwoSided_TheCenterFrequencyShiftsEveryFrequency(Func<IEnumerable<double>> frequencies, double[] expected, double tolerance) =>
        expected.AssertCloseTo([.. frequencies()], tolerance);
}
