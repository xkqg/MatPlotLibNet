// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet;

/// <summary>The scale a magnitude spectrum is drawn on (matplotlib's <c>scale</c> of <c>magnitude_spectrum</c>):
/// <see cref="SignalAxesExtensions.MagnitudeSpectrum"/> takes it.</summary>
/// <remarks><b>Append-only ordinal contract:</b> never reorder, remove, or renumber.</remarks>
public enum MagnitudeScale
{
    /// <summary>The magnitude as it is, |X| divided by the sum of the window (matplotlib's <c>'linear'</c>, the default; its Y axis
    /// is titled "Magnitude (energy)").</summary>
    Linear = 0,

    /// <summary>The magnitude in decibels, twenty times its logarithm to base ten (matplotlib's <c>'dB'</c>); a magnitude of
    /// exactly zero has no decibels and is a gap in the line, never minus infinity.</summary>
    Decibel = 1,
}
