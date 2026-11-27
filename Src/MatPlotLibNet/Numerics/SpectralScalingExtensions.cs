// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The one rule for what the power of a weighted segment is divided by (matplotlib's <c>scale_by_freq</c>), shared by the
/// averaged spectrum (<see cref="Welch"/>) and the spectrum of each window of a spectrogram, so the two cannot differ.</summary>
internal static class SpectralScalingExtensions
{
    extension(SpectralScaling scaling)
    {
        /// <summary>What the power is divided by: the sample rate times the sum of the squared window weights for a density, the square
        /// of the sum of the weights for a spectrum.</summary>
        /// <param name="window">The weights of the window.</param>
        /// <param name="sampleRate">The sample rate.</param>
        /// <returns>The divisor.</returns>
        internal double DivisorOf(double[] window, double sampleRate)
        {
            if (scaling == SpectralScaling.Spectrum)
            {
                double sum = window.Sum();
                return sum * sum;
            }

            return sampleRate * window.Sum(w => w * w);
        }
    }
}
