// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Polynomial least-squares regression utilities.</summary>
public static class LeastSquares
{
    /// <summary>
    /// Fits a polynomial of the specified <paramref name="degree"/> to the data using the normal equations.
    /// Coefficients are returned as [a₀, a₁, …, aₙ] so that y = a₀ + a₁x + a₂x² + … + aₙxⁿ.
    /// </summary>
    /// <remarks>Numerical stability degrades above degree ~10 due to Vandermonde matrix conditioning;
    /// prefer degree ≤ 6 for well-behaved results.</remarks>
    /// <param name="x">X data values.</param>
    /// <param name="y">Y data values (same length as <paramref name="x"/>).</param>
    /// <param name="degree">Polynomial degree (0–10).</param>
    /// <returns>Coefficient array of length <c>degree + 1</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="x"/> or <paramref name="y"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="degree"/> is outside [0, 10].</exception>
    public static double[] PolyFit(double[] x, double[] y, int degree)
    {
        x.ThrowIfEmpty("Data must not be empty.");
        degree.ThrowIfNotIn(0, 10, nameof(degree));

        int n = x.Length;
        int m = degree + 1;

        // Build normal equations: (X^T X) coeff = X^T y  using Vandermonde rows
        double[,] ata = new double[m, m]; // X^T X
        double[] aty = new double[m];     // X^T y

        for (int i = 0; i < n; i++)
        {
            // Precompute powers of x[i] up to 2*degree
            double[] xpow = new double[2 * m - 1];
            xpow[0] = 1.0;
            for (int p = 1; p < xpow.Length; p++) xpow[p] = xpow[p - 1] * x[i];

            for (int r = 0; r < m; r++)
            {
                aty[r] += xpow[r] * y[i];
                for (int c = 0; c < m; c++) ata[r, c] += xpow[r + c];
            }
        }

        return Linalg.Solve(new Mat(ata), new Vec(aty)).Data;
    }

    /// <summary>Evaluates a polynomial at an array of X values.</summary>
    /// <param name="coefficients">Coefficients [a₀, a₁, …, aₙ] (intercept first).</param>
    /// <param name="x">X values to evaluate at.</param>
    /// <returns>Evaluated Y values.</returns>
    public static double[] PolyEval(double[] coefficients, double[] x)
    {
        double[] result = new double[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            // Horner's method
            double val = 0;
            for (int j = coefficients.Length - 1; j >= 0; j--)
                val = val * x[i] + coefficients[j];
            result[i] = val;
        }
        return result;
    }

    /// <summary>
    /// Computes confidence bands around the fitted polynomial at the given evaluation X points.
    /// Uses the t-distribution with <c>n − (degree+1)</c> degrees of freedom, at its exact quantile
    /// for any level.
    /// </summary>
    /// <param name="x">Original X data.</param>
    /// <param name="y">Original Y data.</param>
    /// <param name="coefficients">Fitted polynomial coefficients from <see cref="PolyFit"/>.</param>
    /// <param name="evalX">X values at which to compute the band.</param>
    /// <param name="level">Confidence level (default <see cref="Numerics.ConfidenceBand.DefaultLevel"/>).</param>
    /// <returns>A <see cref="ConfidenceBand"/> containing upper and lower bound arrays.</returns>
    /// <remarks>Confidence intervals assume normally distributed residuals; accuracy degrades
    /// for heavy-tailed or heteroscedastic data.</remarks>
    public static ConfidenceBand ConfidenceBand(
        double[] x, double[] y, double[] coefficients, double[] evalX, double level = Numerics.ConfidenceBand.DefaultLevel)
    {
        int n = x.Length;
        int m = coefficients.Length; // degree + 1
        int dof = Math.Max(n - m, 1);

        // Residuals and residual variance s²
        double[] yHat = PolyEval(coefficients, x);
        double ss = 0;
        for (int i = 0; i < n; i++) { double r = y[i] - yHat[i]; ss += r * r; }
        double s2 = ss / dof;
        double s = Math.Sqrt(s2);

        double tVal = StudentT.Quantile((1.0 + level) / 2.0, dof);

        // Build X^T X inverse for leverage computation
        int p = m;
        double[,] ata = new double[p, p];
        for (int i = 0; i < n; i++)
        {
            double[] xi = new double[p];
            xi[0] = 1.0;
            for (int j = 1; j < p; j++) xi[j] = xi[j - 1] * x[i];
            for (int r = 0; r < p; r++)
                for (int c = 0; c < p; c++)
                    ata[r, c] += xi[r] * xi[c];
        }
        double[,] ataInv = Linalg.Inv(new Mat(ata)).Data;

        double[] upper = new double[evalX.Length];
        double[] lower = new double[evalX.Length];
        double[] yEval = PolyEval(coefficients, evalX);

        for (int j = 0; j < evalX.Length; j++)
        {
            // Leverage h_j = x0^T (X^T X)^{-1} x0
            double[] x0 = new double[p];
            x0[0] = 1.0;
            for (int k = 1; k < p; k++) x0[k] = x0[k - 1] * evalX[j];
            double h = 0;
            for (int r = 0; r < p; r++)
                for (int c = 0; c < p; c++)
                    h += x0[r] * ataInv[r, c] * x0[c];
            double margin = tVal * s * Math.Sqrt(Math.Max(h, 0));
            upper[j] = yEval[j] + margin;
            lower[j] = yEval[j] - margin;
        }
        return new(upper, lower);
    }
}
