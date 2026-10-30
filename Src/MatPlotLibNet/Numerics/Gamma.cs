// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The gamma function: its logarithm and the regularised incomplete gamma function. The pieces the chi-square
/// distribution (a log-rank test's p-value) and the beta function (Student's t) are built from.</summary>
internal static class Gamma
{
    // Lanczos approximation, g = 7 with nine coefficients (Godfrey's): about fifteen correct digits for x >= 0.5.
    private const double LanczosG = 7;
    private static readonly double[] Lanczos =
    [
        0.99999999999980993, 676.5203681218851, -1259.1392167224028, 771.32342877765313, -176.61502916214059,
        12.507343278686905, -0.13857109526572012, 9.9843695780195716e-6, 1.5056327351493116e-7,
    ];

    private const double HalfLogTwoPi = 0.91893853320467274178;
    private const int MaxIterations = 10_000;
    private const double Epsilon = 1e-16;

    /// <summary>The natural logarithm of the gamma function at <paramref name="x"/>, for x above 0 (scipy's <c>gammaln</c>).
    /// Below 0.5 the reflection formula turns it into the same series at 1 - x. Exactly 0 at 1 and at 2, where the
    /// approximation would leave a rounding error of its own.</summary>
    /// <param name="x">A positive number.</param>
    /// <returns>ln Γ(x); NaN for x at or below 0 or NaN.</returns>
    internal static double LogGamma(double x)
    {
        if (double.IsNaN(x) || x <= 0) return double.NaN;
        if (x == 1 || x == 2) return 0;
        if (x < 0.5) return Math.Log(Math.PI / Math.Sin(Math.PI * x)) - LogGamma(1 - x);

        x -= 1;
        double a = Lanczos[0];
        for (int i = 1; i < Lanczos.Length; i++) a += Lanczos[i] / (x + i);
        double t = x + LanczosG + 0.5;
        return HalfLogTwoPi + (x + 0.5) * Math.Log(t) - t + Math.Log(a);
    }

    private const double EulerMascheroni = 0.57721566490153286;
    private const int MaxHarmonic = 4096;

    /// <summary>The digamma function, ψ(x) = d/dx ln Γ(x). At a whole number up to 4096 it is the harmonic sum
    /// <c>-γ + 1 + 1/2 + … + 1/(x-1)</c>, which needs no logarithm and so does not depend on the platform's libm (the Kraskov estimator
    /// reads it at whole numbers only). Elsewhere it is shifted up to 10 by ψ(x) = ψ(x + 1) - 1/x and read off the asymptotic series.</summary>
    /// <param name="x">The argument; above 0.</param>
    /// <returns>ψ(x), or NaN for a <paramref name="x"/> that is not above 0 (or not a number).</returns>
    internal static double Digamma(double x)
    {
        if (double.IsNaN(x) || x <= 0) return double.NaN;
        if (double.IsPositiveInfinity(x)) return x;
        if (x <= MaxHarmonic && x == Math.Floor(x))
        {
            double sum = -EulerMascheroni;
            for (int k = 1; k < (int)x; k++) sum += 1.0 / k;
            return sum;
        }

        double shift = 0;
        while (x < 10)
        {
            shift -= 1 / x;
            x += 1;
        }

        double inverse = 1 / x;
        double square = inverse * inverse;
        double series = square * (1.0 / 12 - square * (1.0 / 120 - square * (1.0 / 252 - square * (1.0 / 240
                      - square * (1.0 / 132 - square * (691.0 / 32760 - square / 12))))));
        return shift + Math.Log(x) - 0.5 * inverse - series;
    }

    /// <summary>The regularised upper incomplete gamma function Q(a, x) = Γ(a, x) / Γ(a): the chance that a gamma variable of
    /// shape <paramref name="a"/> exceeds <paramref name="x"/> (scipy's <c>gammaincc</c>). It runs from 1 at x = 0 down to 0.
    /// Below a + 1 it is one minus the lower function's series; above, a continued fraction (modified Lentz) for it directly, so
    /// the far tail keeps its relative accuracy.</summary>
    /// <param name="a">The shape, above 0.</param>
    /// <param name="x">The point.</param>
    /// <returns>Q(a, x) in [0, 1]; NaN for a at or below 0, or a missing number.</returns>
    internal static double RegularisedUpper(double a, double x)
    {
        if (double.IsNaN(a) || double.IsNaN(x) || a <= 0) return double.NaN;
        if (x <= 0) return 1;
        if (double.IsPositiveInfinity(x)) return 0;

        double logFront = a * Math.Log(x) - x - LogGamma(a);
        return x < a + 1 ? 1 - Math.Exp(logFront) * LowerSeries(a, x) : Math.Exp(logFront) * UpperFraction(a, x);
    }

    /// <summary>The series for the lower function: P(a, x) = x^a e^-x / Γ(a) · Σ x^n / (a (a+1) ... (a+n)); this is the sum,
    /// without the front factor.</summary>
    private static double LowerSeries(double a, double x)
    {
        double term = 1 / a, sum = term;
        for (int n = 1; n < MaxIterations; n++)
        {
            term *= x / (a + n);
            sum += term;
            if (Math.Abs(term) < Math.Abs(sum) * Epsilon) break;
        }

        return sum;
    }

    /// <summary>The continued fraction for the upper function, without the front factor x^a e^-x / Γ(a), by the modified Lentz
    /// method.</summary>
    private static double UpperFraction(double a, double x)
    {
        double b = x + 1 - a, c = 1 / LentzExtensions.Tiny, d = 1 / b, h = d;
        for (int i = 1; i < MaxIterations; i++)
        {
            double an = -i * (i - a);
            b += 2;
            d = (an * d + b).AwayFromZero();
            c = (b + an / c).AwayFromZero();
            d = 1 / d;
            double delta = d * c;
            h *= delta;
            if (Math.Abs(delta - 1) < Epsilon) break;
        }

        return h;
    }
}
