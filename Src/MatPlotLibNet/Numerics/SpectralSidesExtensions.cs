// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace MatPlotLibNet.Numerics;

/// <summary>The one rule that says which places a spectrum has and in what order (matplotlib's <c>sides</c>), shared by the averaged
/// spectrum (<see cref="Welch"/>) and the spectrum of a whole signal (<see cref="Spectrum"/>), so the two cannot differ. A real
/// signal's transform is conjugate-symmetric, so only the bins 0 to M / 2 are ever computed: one-sided they are the result as
/// they stand (no copy is made), two-sided the result reads them twice, the negative frequencies as the conjugates of the positive
/// ones, in ascending frequency from the most negative as matplotlib rolls the transform to the centre.</summary>
internal static class SpectralSidesExtensions
{
    extension(SpectralSides sides)
    {
        /// <summary>The frequency of each place of a transform of <paramref name="m"/> samples, k · sampleRate / m +
        /// centerFrequency: k = 0 .. M / 2 one-sided, k = -M / 2 .. M / 2 - 1 (an even M) or -(M - 1) / 2 .. (M - 1) / 2 (an odd one)
        /// two-sided.</summary>
        /// <param name="m">The length of the transform.</param>
        /// <param name="sampleRate">The sample rate.</param>
        /// <param name="centerFrequency">The frequency added to every frequency.</param>
        /// <returns>The frequencies, in the order of the result.</returns>
        internal double[] FrequenciesOf(int m, double sampleRate, double centerFrequency)
        {
            if (sides != SpectralSides.TwoSided)
            {
                var upper = new double[m / 2 + 1];
                for (int k = 0; k < upper.Length; k++) upper[k] = k * sampleRate / m + centerFrequency;
                return upper;
            }

            var all = new double[m];
            for (int place = 0; place < m; place++) all[place] = sides.HarmonicAt(place, m) * sampleRate / m + centerFrequency;
            return all;
        }

        /// <summary>What the power of bin <paramref name="k"/> of a transform of <paramref name="m"/> samples is multiplied by in the result:
        /// one-sided every bin carries the power of its negative frequency as well (twice), but the first (0 Hz) and, for an even M, the
        /// Nyquist bin, which are their own mirrors (once); two-sided every bin carries its own (once). matplotlib's
        /// <c>_spectral_helper</c>: <c>result[slc] *= scaling_factor</c> over <c>slice(1, -1)</c> (an even M) or <c>slice(1, None)</c>.</summary>
        /// <param name="k">The bin, 0 to M / 2.</param>
        /// <param name="m">The length of the transform.</param>
        /// <returns>2 or 1.</returns>
        internal double FoldOf(int k, int m) => sides != SpectralSides.TwoSided && k != 0 && !(m % 2 == 0 && 2 * k == m) ? 2 : 1;

        /// <summary>The values of the result, given the values of the bins 0 to M / 2 of the transform. One-sided, those values
        /// themselves; two-sided, one for each of the M places, a negative frequency's a copy of its positive's.</summary>
        /// <param name="upper">The bins 0 to M / 2.</param>
        /// <param name="m">The length of the transform.</param>
        /// <returns>The values, in the order of the result.</returns>
        internal double[] Arranged(double[] upper, int m)
        {
            if (sides != SpectralSides.TwoSided) return upper;

            var all = new double[m];
            for (int place = 0; place < m; place++) all[place] = upper[Math.Abs(sides.HarmonicAt(place, m))];
            return all;
        }

        /// <summary>The values of the result, given the bins 0 to M / 2 of the transform. One-sided, those bins themselves;
        /// two-sided, one for each of the M places, a negative frequency's the conjugate of its positive's, except the Nyquist bin
        /// of an even M, which is its own mirror and real.</summary>
        /// <param name="upper">The bins 0 to M / 2.</param>
        /// <param name="m">The length of the transform.</param>
        /// <returns>The values, in the order of the result.</returns>
        internal Complex[] Arranged(Complex[] upper, int m)
        {
            if (sides != SpectralSides.TwoSided) return upper;

            var all = new Complex[m];
            for (int place = 0; place < m; place++)
            {
                int harmonic = sides.HarmonicAt(place, m);
                int index = Math.Abs(harmonic);
                all[place] = harmonic < 0 && 2 * index != m ? Complex.Conjugate(upper[index]) : upper[index];
            }

            return all;
        }

        /// <summary>The frequency, in bin spacings, of a place of a two-sided result: the transform rolled so that the most negative
        /// frequency comes first, which numpy's <c>fftfreq</c> puts the Nyquist bin of an even M at.</summary>
        private int HarmonicAt(int place, int m)
        {
            int source = (place + (m + 1) / 2) % m;
            return source <= (m - 1) / 2 ? source : source - m;
        }
    }
}
