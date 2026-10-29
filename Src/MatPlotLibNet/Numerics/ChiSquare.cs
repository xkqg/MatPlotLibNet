// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The chi-square distribution: the one place in the library that turns a test statistic into a p-value.</summary>
internal static class ChiSquare
{
    /// <summary>The survival function of the chi-square distribution with <paramref name="degreesOfFreedom"/> degrees of
    /// freedom at <paramref name="x"/> (scipy's <c>chi2.sf</c>): the chance that a chi-square variable exceeds
    /// <paramref name="x"/>, which is the p-value of a statistic of that size. It is the regularised upper incomplete gamma
    /// function Q(ν/2, x/2); with 2 degrees of freedom exp(-x / 2).</summary>
    /// <param name="x">The statistic.</param>
    /// <param name="degreesOfFreedom">The degrees of freedom, above 0.</param>
    /// <returns>P(X &gt; x) in [0, 1]; 1 for a statistic at or below 0; NaN for a missing number or degrees of freedom at or below 0.</returns>
    internal static double Survival(double x, double degreesOfFreedom) =>
        double.IsNaN(x) || double.IsNaN(degreesOfFreedom) || degreesOfFreedom <= 0
            ? double.NaN
            : Gamma.RegularisedUpper(degreesOfFreedom / 2, x / 2);
}
