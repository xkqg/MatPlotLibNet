// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Student's t distribution: the one place in the library that evaluates its cumulative distribution function and
/// inverts it. The band of a regression and the quantiles of a Q-Q plot against a t distribution both go through
/// <see cref="Quantile"/>, so two parts of a chart that ask for the same probability get the same number.</summary>
internal static class StudentT
{
    private const int MaxBisections = 400;

    /// <summary>The cumulative distribution function of Student's t distribution with <paramref name="degreesOfFreedom"/>
    /// degrees of freedom at <paramref name="t"/> (scipy's <c>t.cdf</c>), through the regularised incomplete beta function:
    /// for t below 0 it is half of I_x(ν/2, 1/2) at x = ν / (ν + t²), and one minus that half above.</summary>
    /// <param name="t">The point.</param>
    /// <param name="degreesOfFreedom">The degrees of freedom ν, above 0 (not a whole number necessarily).</param>
    /// <returns>P(T &lt;= t); NaN for a missing number or ν at or below 0.</returns>
    internal static double Cdf(double t, double degreesOfFreedom)
    {
        if (double.IsNaN(t) || double.IsNaN(degreesOfFreedom) || degreesOfFreedom <= 0) return double.NaN;
        if (double.IsNegativeInfinity(t)) return 0;
        if (double.IsPositiveInfinity(t)) return 1;

        double half = 0.5 * Beta.RegularisedIncomplete(degreesOfFreedom / 2, 0.5, degreesOfFreedom / (degreesOfFreedom + t * t));
        return t < 0 ? half : 1 - half;
    }

    /// <summary>The inverse of the cumulative distribution function: the value <c>t</c> below which a fraction
    /// <paramref name="p"/> of a Student's t population lies (scipy's <c>t.ppf</c>, R's <c>qt</c>), found by bisection on
    /// <see cref="Cdf"/> in the lower tail, where the probability keeps its relative accuracy, and mirrored for p above a half.
    /// It saturates outside the open interval (0, 1) as <see cref="StandardNormal.Quantile"/> does: <c>p &lt;= 0</c> gives
    /// <see cref="double.NegativeInfinity"/> and <c>p &gt;= 1</c> <see cref="double.PositiveInfinity"/>.</summary>
    /// <param name="p">A probability, normally in (0, 1).</param>
    /// <param name="degreesOfFreedom">The degrees of freedom ν, above 0.</param>
    /// <returns>The quantile; NaN for a missing number or ν at or below 0. It is odd around the median.</returns>
    internal static double Quantile(double p, double degreesOfFreedom)
    {
        if (double.IsNaN(p) || double.IsNaN(degreesOfFreedom) || degreesOfFreedom <= 0) return double.NaN;
        if (p <= 0) return double.NegativeInfinity;
        if (p >= 1) return double.PositiveInfinity;
        if (p == 0.5) return 0;
        if (p > 0.5) return -Quantile(1 - p, degreesOfFreedom);

        // Bracket the root below 0: double the distance from 0 until the probability there is below p.
        double hi = 0, lo = -1;
        while (Cdf(lo, degreesOfFreedom) > p && !double.IsInfinity(lo)) { hi = lo; lo *= 2; }

        for (int i = 0; i < MaxBisections; i++)
        {
            double mid = lo / 2 + hi / 2;
            if (mid == lo || mid == hi) break;
            if (Cdf(mid, degreesOfFreedom) > p) hi = mid; else lo = mid;
        }

        return lo / 2 + hi / 2;
    }
}
