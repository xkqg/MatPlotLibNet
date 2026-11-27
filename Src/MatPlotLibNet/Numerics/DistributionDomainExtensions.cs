// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The two questions every distribution function asks of its input before it computes anything: whether a shape (a degrees of
/// freedom, the shape of a gamma or a beta) can exist, and where a probability that reaches either end of its range lands. The beta, gamma,
/// chi-square, Student's t, exponential and normal functions read them here, so the six agree on what is outside their domain.</summary>
internal static class DistributionDomainExtensions
{
    extension(double shape)
    {
        /// <summary>Whether this number cannot be what a distribution function needs above zero (a shape, a degrees of freedom, the argument of the gamma
        /// function): a missing number, zero and every negative number cannot.</summary>
        /// <returns><see langword="true"/> for NaN, zero and every negative number; the function that asked answers NaN.</returns>
        internal bool IsUndefinedShape() => double.IsNaN(shape) || shape <= 0;
    }

    extension(double probability)
    {
        /// <summary>Where this probability lands when it reaches an end of its range: a probability is a limit, not an error, at 0 and at 1.</summary>
        /// <param name="atZero">What a quantile function answers at a probability of 0 or below (the low end of its support).</param>
        /// <param name="atOne">What it answers at a probability of 1 or above (the high end of its support).</param>
        /// <returns>NaN for a missing probability, <paramref name="atZero"/> or <paramref name="atOne"/> at an end, and <see langword="null"/>
        /// for a probability inside the open interval, which the function computes.</returns>
        internal double? Saturated(double atZero, double atOne)
        {
            if (double.IsNaN(probability)) return double.NaN;
            if (probability <= 0) return atZero;
            if (probability >= 1) return atOne;
            return null;
        }
    }
}
