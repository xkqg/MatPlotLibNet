// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The exponential distribution with rate 1: the one place in the library that inverts its cumulative distribution
/// function, for a Q-Q plot against an exponential reference.</summary>
internal static class Exponential
{
    /// <summary>The quantile of the exponential distribution (scipy's <c>expon.ppf</c>): <c>-ln(1 - p)</c>, worked out as
    /// <c>-ln1p(-p)</c>. For a small p the logarithm of <c>1 - p</c> keeps none of p's digits (1 - 1e-9 holds seven correct digits of
    /// 1e-9), and <see cref="double.LogP1"/> is the same naive sum, so the accurate form is written here.</summary>
    /// <param name="p">A probability, normally in [0, 1).</param>
    /// <returns>The quantile; 0 at or below 0, <see cref="double.PositiveInfinity"/> at or above 1, NaN for a missing number.</returns>
    internal static double Quantile(double p)
    {
        if (double.IsNaN(p)) return double.NaN;
        if (p <= 0) return 0;
        if (p >= 1) return double.PositiveInfinity;
        return -LogOnePlus(-p);
    }

    /// <summary>ln(1 + x) to full precision for small x: with u = 1 + x as it was rounded, ln(u) · x / (u - 1) corrects for the
    /// digits the sum lost (Goldberg, "What every computer scientist should know about floating-point arithmetic", theorem 4).</summary>
    private static double LogOnePlus(double x)
    {
        double u = 1 + x;
        return u == 1 ? x : Math.Log(u) * x / (u - 1);
    }
}
