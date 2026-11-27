// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>What a short-time transform gives besides the magnitude of each bin.</summary>
internal static class StftResultExtensions
{
    extension(StftResult stft)
    {
        /// <summary>The one-sided power density of each window, as matplotlib's <c>specgram</c> computes it (<c>mode='psd'</c>, the
        /// default): the power |X|² of each bin divided as a density divides it (<see cref="SpectralScaling.Density"/>: the sample rate
        /// times the sum of the squared weights of the Hann window <see cref="Fft.Stft"/> weighs each window by) and folded one-sided
        /// (<see cref="SpectralSides.OneSided"/>: every row but 0 Hz and the Nyquist row doubled).</summary>
        /// <param name="windowSize">The length of each window, the length of its transform.</param>
        /// <param name="sampleRate">The sample rate.</param>
        /// <returns>The density, [frequency bin, window], in the shape of the magnitudes.</returns>
        internal double[,] OneSidedDensity(int windowSize, int sampleRate)
        {
            var magnitudes = stft.Magnitudes;
            int bins = magnitudes.GetLength(0), frames = magnitudes.GetLength(1);
            double divisor = SpectralScaling.Density.DivisorOf(SpectralWindow.Hann.Coefficients(windowSize), sampleRate);
            var density = new double[bins, frames];
            for (int b = 0; b < bins; b++)
            {
                double factor = SpectralSides.OneSided.FoldOf(b, windowSize) / divisor;
                for (int f = 0; f < frames; f++) density[b, f] = magnitudes[b, f] * magnitudes[b, f] * factor;
            }

            return density;
        }
    }
}
