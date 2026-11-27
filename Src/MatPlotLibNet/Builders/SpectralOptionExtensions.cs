// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>The four settings the options of a spectrum share, <see cref="SpectralOptions"/> (a Welch estimate) and
/// <see cref="SpectrumOptions"/> (the spectrum of a whole signal), and the one rule each is held to. A property refuses a value
/// no spectrum can be computed with by its own name, in the same words in both.</summary>
internal static class SpectralOptionExtensions
{
    /// <summary>The sample rate of a spectrum that is told none: matplotlib's default <c>Fs</c>, so the frequencies run from 0 to 1, the
    /// Nyquist frequency. Both kinds of options read it.</summary>
    internal const double RestingSampleRate = 2;

    extension(double value)
    {
        /// <summary>This value as a sample rate: a finite number above 0.</summary>
        /// <param name="property">The property that was given the value, named in the exception.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is not a finite number above 0.</exception>
        internal double AsSampleRate(string property) =>
            value.ThrowIfNotFinite(property, "The sample rate").ThrowIfNotAbove(0, property, "The sample rate");

        /// <summary>This value as a centre frequency: a finite number.</summary>
        /// <param name="property">The property that was given the value, named in the exception.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is not a finite number.</exception>
        internal double AsCenterFrequency(string property) =>
            value.ThrowIfNotFinite(property, "The centre frequency");
    }

    extension(SpectralSides value)
    {
        /// <summary>This value as the sides of a spectrum: a member of <see cref="SpectralSides"/>.</summary>
        /// <param name="property">The property that was given the value, named in the exception.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is not a member of <see cref="SpectralSides"/>.</exception>
        internal SpectralSides AsSides(string property) =>
            value.ThrowIfNotDefined(property, "The sides are Default, OneSided or TwoSided.");
    }

    extension(int value)
    {
        /// <summary>This value as a padded length: 0 (no padding) or a number of samples.</summary>
        /// <param name="property">The property that was given the value, named in the exception.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is below 0.</exception>
        internal int AsPadTo(string property) =>
            value.ThrowIfBelow(0, property, "The padded length");
    }
}
