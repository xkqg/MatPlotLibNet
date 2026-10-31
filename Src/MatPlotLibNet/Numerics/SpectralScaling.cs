// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>What the averaged power of a spectrum is divided by: the power in each bandwidth of one hertz, or the power in
/// each bin (matplotlib's <c>scale_by_freq</c> true or false).</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber.</remarks>
public enum SpectralScaling
{
    /// <summary>A power spectral density, in units² per hertz: each bin is divided by the sample rate and by the sum of the
    /// squared window weights, so the density summed over the frequencies times their spacing is the power of the signal
    /// (matplotlib's <c>scale_by_freq=True</c>, the default).</summary>
    Density = 0,

    /// <summary>A power spectrum, in units²: each bin is divided by the square of the sum of the window weights, so a tone that
    /// falls on a bin reads as its own power there (matplotlib's <c>scale_by_freq=False</c>).</summary>
    Spectrum = 1,
}
