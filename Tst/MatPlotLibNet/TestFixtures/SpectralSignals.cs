// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Tests;

/// <summary>The signals the spectral verbs are checked against, written once: the numbers marked "matplotlib" in their tests were
/// printed by matplotlib 3.11.2 for exactly these.</summary>
public static class SpectralSignals
{
    /// <summary>Thirty-two samples made by integer arithmetic: a tone of period four (10·wave) and a pseudo-random part.</summary>
    public static readonly double[] X =
    [
        -5, 15, 4, -7, 2, 11, 0, -11, -2, 7, -4, -15, 5, 14, 3, -8,
        1, 10, -1, -12, -3, 6, -5, -5, 4, 13, 2, -9, 0, 9, -2, -13,
    ];

    /// <summary>A second signal of thirty-two samples: the same tone a quarter of a period later, with a pseudo-random part of its own.</summary>
    public static readonly double[] Y =
    [
        0, -5, -10, -3, 4, -1, -6, 1, 8, 3, -2, 5, 12, -6, -11, -4,
        3, -2, -7, 0, 7, 2, -3, 4, 11, 6, -12, -5, 2, -3, -8, -1,
    ];

    /// <summary>Segments of 16 samples that start 12 apart (an overlap of 4), so each signal holds two of them, the fewest a
    /// coherence needs; samples 12 to 15 belong to both.</summary>
    public static readonly SpectralOptions Sixteen = new() { SampleRate = 1, SegmentLength = 16, Overlap = 4 };

    /// <summary>matplotlib's frequencies for <see cref="Sixteen"/>: k / 16 for k = 0 to 8.</summary>
    public static readonly double[] Frequencies = [0, 0.0625, 0.125, 0.1875, 0.25, 0.3125, 0.375, 0.4375, 0.5];

    /// <summary>A unit impulse at sample 3 of 7: the signal whose spectrum is a pure delay, its phase and its angle falling by 6π/7 per bin.</summary>
    public static readonly double[] Impulse7 = [0, 0, 0, 1, 0, 0, 0];

    /// <summary>Eleven samples, the signal of the single-signal spectra (magnitude, phase, angle).</summary>
    public static readonly double[] Eleven = [3, -1, 4, 1, -5, 9, 2, -6, 5, 3, -5];
}
