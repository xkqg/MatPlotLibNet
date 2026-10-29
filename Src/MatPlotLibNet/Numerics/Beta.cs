// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The beta function: the regularised incomplete beta function I_x(a, b), which Student's t distribution is built from.</summary>
internal static class Beta
{
    private const int MaxIterations = 10_000;
    private const double Epsilon = 1e-16;

    /// <summary>The regularised incomplete beta function I_x(a, b): the chance that a beta variable of shape (a, b) is at most
    /// <paramref name="x"/> (scipy's <c>betainc</c>). It runs from 0 at x = 0 to 1 at x = 1. It is worked out by a continued
    /// fraction (modified Lentz) in the half of the range where it converges fast, and by the mirror
    /// I_x(a, b) = 1 - I_(1-x)(b, a) in the other.</summary>
    /// <param name="a">The first shape, above 0.</param>
    /// <param name="b">The second shape, above 0.</param>
    /// <param name="x">The point, held to [0, 1].</param>
    /// <returns>I_x(a, b) in [0, 1]; NaN for a shape at or below 0, or a missing number.</returns>
    internal static double RegularisedIncomplete(double a, double b, double x)
    {
        if (double.IsNaN(a) || double.IsNaN(b) || double.IsNaN(x) || a <= 0 || b <= 0) return double.NaN;
        if (x <= 0) return 0;
        if (x >= 1) return 1;

        double front = Math.Exp(Gamma.LogGamma(a + b) - Gamma.LogGamma(a) - Gamma.LogGamma(b) + a * Math.Log(x) + b * Math.Log(1 - x));
        return x < (a + 1) / (a + b + 2)
            ? front * Fraction(a, b, x) / a
            : 1 - front * Fraction(b, a, 1 - x) / b;
    }

    /// <summary>The continued fraction of the incomplete beta function, by the modified Lentz method.</summary>
    private static double Fraction(double a, double b, double x)
    {
        double qab = a + b, qap = a + 1, qam = a - 1;
        double c = 1, d = (1 - qab * x / qap).AwayFromZero();
        d = 1 / d;
        double h = d;
        for (int m = 1; m < MaxIterations; m++)
        {
            int m2 = 2 * m;
            double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
            d = (1 + aa * d).AwayFromZero();
            c = (1 + aa / c).AwayFromZero();
            d = 1 / d;
            h *= d * c;
            aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
            d = (1 + aa * d).AwayFromZero();
            c = (1 + aa / c).AwayFromZero();
            d = 1 / d;
            double delta = d * c;
            h *= delta;
            if (Math.Abs(delta - 1) < Epsilon) break;
        }

        return h;
    }
}
