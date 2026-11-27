// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The one owner of how a spectrum is written in decibels, beside <see cref="Spectrum"/> and <see cref="Welch"/>, which
/// make the spectrum.</summary>
internal static class DecibelExtensions
{
    /// <summary>The decibels a factor of ten in a power adds: ten times its logarithm to base ten.</summary>
    private const double PerDecadeOfPower = 10;

    /// <summary>The decibels a factor of ten in a magnitude (an amplitude, the square root of a power) adds: twenty times its logarithm.</summary>
    private const double PerDecadeOfMagnitude = 20;

    /// <summary>The decibels of one value: <paramref name="perDecade"/> times its logarithm to base ten, NaN where it is not above zero.</summary>
    private static double DecibelsOf(double value, double perDecade) => value > 0 ? perDecade * Math.Log10(value) : double.NaN;

    extension(double[] values)
    {
        /// <summary>This power, in decibels: ten times its logarithm to base ten (<see cref="ToDecibels"/>).</summary>
        /// <returns>The decibels, one for each value.</returns>
        internal double[] PowerInDecibels() => values.ToDecibels(PerDecadeOfPower);

        /// <summary>This magnitude, in decibels: twenty times its logarithm to base ten (<see cref="ToDecibels"/>).</summary>
        /// <returns>The decibels, one for each value.</returns>
        internal double[] MagnitudeInDecibels() => values.ToDecibels(PerDecadeOfMagnitude);

        /// <summary>The decibels of each value: <paramref name="decibelsPerDecade"/> times its logarithm to base ten (10 for a
        /// power, 20 for a magnitude). A value that is not above zero has no logarithm: it is NaN, a gap in the line, and not
        /// minus infinity (zero) or an error (a missing value is NaN already).</summary>
        /// <param name="decibelsPerDecade">The decibels a factor of ten in the value adds.</param>
        /// <returns>The decibels, one for each value.</returns>
        internal double[] ToDecibels(double decibelsPerDecade)
        {
            var decibels = new double[values.Length];
            for (int i = 0; i < decibels.Length; i++) decibels[i] = DecibelsOf(values[i], decibelsPerDecade);

            return decibels;
        }
    }

    extension(double[,] values)
    {
        /// <summary>This grid of powers turned into decibels where it stands, value by value as a row of powers is written: ten times the
        /// logarithm to base ten, and NaN for a power that is not above zero. For a grid made to be turned (a spectrogram's density), so no
        /// second grid is made.</summary>
        /// <returns>This grid, now in decibels.</returns>
        internal double[,] PowerToDecibelsInPlace()
        {
            int rows = values.GetLength(0), columns = values.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                    values[r, c] = DecibelsOf(values[r, c], PerDecadeOfPower);

            return values;
        }
    }
}
