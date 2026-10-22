// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The standard normal distribution (mean 0, standard deviation 1): the one place in the library that inverts its
/// cumulative distribution function.</summary>
/// <remarks>Every caller that needs a z value (the confidence band of <see cref="LeastSquares"/>, a Q-Q plot's theoretical
/// quantiles) goes through <see cref="Quantile"/>, so two parts of a chart that ask for the same probability get the same number.</remarks>
internal static class StandardNormal
{
    // The seams of AS241 (PPND16): the centre is |p - 0.5| <= 0.425; Const1 = 0.425 squared.
    private const double CentreLimit = 0.425;
    private const double CentreSquared = 0.180625;

    // The tail is split where r = sqrt(-ln(min(p, 1 - p))) crosses 5; the near tail shifts r by 1.6, the far tail by 5.
    private const double NearTailLimit = 5.0;
    private const double NearTailShift = 1.6;

    // Coefficients of AS241 PPND16, constant term first (index = power of the argument). The denominators lead with the
    // constant term 1, which the algorithm writes out as ONE; evaluating all eight terms by Horner's rule reproduces the
    // published nesting term for term. Checked digit for digit against R's nmath/qnorm.c, which implements the same algorithm.
    private static readonly double[] CentreNumerator =
    [
        3.3871328727963666080, 1.3314166789178437745e2, 1.9715909503065514427e3, 1.3731693765509461125e4,
        4.5921953931549871457e4, 6.7265770927008700853e4, 3.3430575583588128105e4, 2.5090809287301226727e3,
    ];

    private static readonly double[] CentreDenominator =
    [
        1.0, 4.2313330701600911252e1, 6.8718700749205790830e2, 5.3941960214247511077e3,
        2.1213794301586595867e4, 3.9307895800092710610e4, 2.8729085735721942674e4, 5.2264952788528545610e3,
    ];

    private static readonly double[] NearTailNumerator =
    [
        1.42343711074968357734, 4.63033784615654529590, 5.76949722146069140550, 3.64784832476320460504,
        1.27045825245236838258, 2.41780725177450611770e-1, 2.27238449892691845833e-2, 7.74545014278341407640e-4,
    ];

    private static readonly double[] NearTailDenominator =
    [
        1.0, 2.05319162663775882187, 1.67638483018380384940, 6.89767334985100004550e-1,
        1.48103976427480074590e-1, 1.51986665636164571966e-2, 5.47593808499534494600e-4, 1.05075007164441684324e-9,
    ];

    private static readonly double[] FarTailNumerator =
    [
        6.65790464350110377720, 5.46378491116411436990, 1.78482653991729133580, 2.96560571828504891230e-1,
        2.65321895265761230930e-2, 1.24266094738807843860e-3, 2.71155556874348757815e-5, 2.01033439929228813265e-7,
    ];

    private static readonly double[] FarTailDenominator =
    [
        1.0, 5.99832206555887937690e-1, 1.36929880922735805310e-1, 1.48753612908506148525e-2,
        7.86869131145613259100e-4, 1.84631831751005468180e-5, 1.42151175831644588870e-7, 2.04426310338993978564e-15,
    ];

    /// <summary>The inverse of the standard normal cumulative distribution function: the value <c>z</c> below which a fraction
    /// <paramref name="p"/> of a standard normal population lies (the "probit"; scipy's <c>norm.ppf</c>, R's <c>qnorm</c>).</summary>
    /// <remarks>
    /// <para>Wichura's algorithm AS 241, routine PPND16: three rational approximations, one for the centre
    /// (<c>|p - 0.5| &lt;= 0.425</c>) and two for the tails (<c>r = sqrt(-ln(min(p, 1 - p)))</c> up to 5, and beyond), accurate to
    /// about 1 part in 10^16 across the whole range of a double, down to <c>p = 5e-324</c> (z = -38.47).
    /// Wichura, M. J. (1988), "Algorithm AS 241: The Percentage Points of the Normal Distribution",
    /// <i>Applied Statistics</i> 37(3), 477-484, doi:10.2307/2347330.</para>
    /// <para>It replaces the rational approximation of Abramowitz and Stegun, formula 26.2.23, which this library used for the
    /// confidence band of a regression and which is off by up to 4.4e-4 (1.96039 for the 95 % point, whose exact value is 1.95996).</para>
    /// <para>The function saturates outside the open interval (0, 1), because a probability of 0 or 1 is a limit, not an error:
    /// <c>p &lt;= 0</c> gives <see cref="double.NegativeInfinity"/> and <c>p &gt;= 1</c> gives <see cref="double.PositiveInfinity"/>.
    /// The confidence band of <see cref="LeastSquares"/> reads a level outside (-1, 1) as an infinite margin. A missing probability
    /// (NaN) gives NaN. Nothing throws.</para>
    /// </remarks>
    /// <param name="p">A probability, normally in (0, 1).</param>
    /// <returns>The quantile <c>z</c> with <c>P(Z &lt;= z) = p</c>. It is odd around the median: <c>Quantile(p) = -Quantile(1 - p)</c>.</returns>
    internal static double Quantile(double p)
    {
        if (double.IsNaN(p)) return double.NaN;
        if (p <= 0) return double.NegativeInfinity;
        if (p >= 1) return double.PositiveInfinity;

        double q = p - 0.5;
        if (Math.Abs(q) <= CentreLimit)
        {
            double r = CentreSquared - q * q;
            return q * (Horner(CentreNumerator, r) / Horner(CentreDenominator, r));
        }

        // The smaller of p and 1 - p; 1 - p is exact for p in [0.5, 1], so both tails reach the same r.
        double tail = q < 0 ? p : 1.0 - p;
        double s = Math.Sqrt(-Math.Log(tail));
        double z = s <= NearTailLimit
            ? Horner(NearTailNumerator, s - NearTailShift) / Horner(NearTailDenominator, s - NearTailShift)
            : Horner(FarTailNumerator, s - NearTailLimit) / Horner(FarTailDenominator, s - NearTailLimit);
        return q < 0 ? -z : z;
    }

    /// <summary>Evaluates a polynomial given by its coefficients, constant term first, with Horner's rule.</summary>
    private static double Horner(double[] coefficients, double x)
    {
        double value = coefficients[^1];
        for (int i = coefficients.Length - 2; i >= 0; i--) value = value * x + coefficients[i];
        return value;
    }
}
