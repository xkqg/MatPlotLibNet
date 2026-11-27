// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Styling.ColorMaps;

/// <summary>The one answer to "where does a value stand in a range, when the range may have no width": a range of no width, which has
/// no scale to stand in, is the middle of the colour map, and every other range is the normaliser's own transform of it. The linear,
/// logarithmic, symmetric-logarithmic and power-law normalisers answer <see cref="INormalizer.Normalize"/> through it and supply only
/// their transform (<see cref="IRangeFraction"/>); the centred and two-slope scales read their own halves.</summary>
internal static class RangeFractionExtensions
{
    extension(IRangeFraction normalizer)
    {
        /// <summary>Where <paramref name="value"/> stands in the range <paramref name="min"/> to <paramref name="max"/>, in [0, 1]: 0.5
        /// for a range of no width, the normaliser's transform of the value otherwise.</summary>
        /// <param name="value">The value.</param>
        /// <param name="min">The low end of the range.</param>
        /// <param name="max">The high end of the range.</param>
        /// <returns>The position in the colour map.</returns>
        internal double Normalized(double value, double min, double max)
        {
            double range = max - min;
            if (range == 0) return 0.5;
            return normalizer.Fraction(value, min, max);
        }
    }
}
