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
        if (x.Length == 0) throw new ArgumentException("Data must not be empty.", nameof(x));
        if (degree < 0 || degree > 10)
            throw new ArgumentOutOfRangeException(nameof(degree), "Degree must be between 0 and 10.");

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

        return SolveLinear(ata, aty);
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
    /// Uses the t-distribution with <c>n − (degree+1)</c> degrees of freedom.
    /// </summary>
    /// <param name="x">Original X data.</param>
    /// <param name="y">Original Y data.</param>
    /// <param name="coefficients">Fitted polynomial coefficients from <see cref="PolyFit"/>.</param>
    /// <param name="evalX">X values at which to compute the band.</param>
    /// <param name="level">Confidence level (default 0.95).</param>
    /// <returns>A <see cref="ConfidenceBand"/> containing upper and lower bound arrays.</returns>
    /// <remarks>Confidence intervals assume normally distributed residuals; accuracy degrades
    /// for heavy-tailed or heteroscedastic data.</remarks>
    public static ConfidenceBand ConfidenceBand(
        double[] x, double[] y, double[] coefficients, double[] evalX, double level = 0.95)
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

        double tVal = StudentTQuantile((1.0 + level) / 2.0, dof);

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
        double[,] ataInv = InvertSymmetric(ata);

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

    // --- Internal helpers ---

    /// <summary>Solves Ax = b via Gaussian elimination with partial pivoting.</summary>
    private static double[] SolveLinear(double[,] a, double[] b)
    {
        int n = b.Length;
        // Augmented matrix
        double[,] aug = new double[n, n + 1];
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++) aug[r, c] = a[r, c];
            aug[r, n] = b[r];
        }
        // Forward elimination with partial pivoting
        for (int col = 0; col < n; col++)
        {
            int pivot = col;
            for (int row = col + 1; row < n; row++)
                if (Math.Abs(aug[row, col]) > Math.Abs(aug[pivot, col])) pivot = row;
            for (int k = 0; k <= n; k++) (aug[col, k], aug[pivot, k]) = (aug[pivot, k], aug[col, k]);
            double diag = aug[col, col];
            if (Math.Abs(diag) < 1e-14) continue;
            for (int row = col + 1; row < n; row++)
            {
                double factor = aug[row, col] / diag;
                for (int k = col; k <= n; k++) aug[row, k] -= factor * aug[col, k];
            }
        }
        // Back substitution
        double[] result = new double[n];
        for (int row = n - 1; row >= 0; row--)
        {
            double sum = aug[row, n];
            for (int col = row + 1; col < n; col++) sum -= aug[row, col] * result[col];
            double d = aug[row, row];
            result[row] = Math.Abs(d) > 1e-14 ? sum / d : 0;
        }
        return result;
    }

    /// <summary>Inverts a symmetric matrix using the same Gaussian elimination augmented with identity.</summary>
    private static double[,] InvertSymmetric(double[,] a)
    {
        int n = a.GetLength(0);
        double[,] aug = new double[n, 2 * n];
        for (int r = 0; r < n; r++)
        {
            for (int c = 0; c < n; c++) aug[r, c] = a[r, c];
            aug[r, n + r] = 1.0;
        }
        for (int col = 0; col < n; col++)
        {
            int pivot = col;
            for (int row = col + 1; row < n; row++)
                if (Math.Abs(aug[row, col]) > Math.Abs(aug[pivot, col])) pivot = row;
            for (int k = 0; k < 2 * n; k++) (aug[col, k], aug[pivot, k]) = (aug[pivot, k], aug[col, k]);
            double diag = aug[col, col];
            if (Math.Abs(diag) < 1e-14) continue;
            for (int k = 0; k < 2 * n; k++) aug[col, k] /= diag;
            for (int row = 0; row < n; row++)
            {
                if (row == col) continue;
                double factor = aug[row, col];
                for (int k = 0; k < 2 * n; k++) aug[row, k] -= factor * aug[col, k];
            }
        }
        double[,] inv = new double[n, n];
        for (int r = 0; r < n; r++)
            for (int c = 0; c < n; c++)
                inv[r, c] = aug[r, n + c];
        return inv;
    }

    /// <summary>
    /// Approximates the upper quantile of the t-distribution at a given one-sided probability and degrees of freedom.
    /// Uses a lookup table for small dof and the normal approximation for large dof.
    /// </summary>
    private static double StudentTQuantile(double p, int dof)
    {
        // p is the one-sided probability (e.g., 0.975 for 95% CI, 0.995 for 99% CI)
        if (dof >= 120) return NormalQuantile(p);

        // Lookup for p ≈ 0.995  (99% two-sided CI)
        if (p >= 0.99)
        {
            if (dof >= 60) return 2.660;
            if (dof >= 30) return 2.750;
            if (dof >= 20) return 2.845;
            if (dof >= 15) return 2.947;
            if (dof >= 10) return 3.169;
            if (dof >= 8)  return 3.355;
            if (dof >= 6)  return 3.707;
            if (dof >= 5)  return 4.032;
            if (dof >= 4)  return 4.604;
            if (dof >= 3)  return 5.841;
            if (dof >= 2)  return 9.925;
            return 63.657; // dof == 1
        }

        // Lookup for p ≈ 0.975  (95% two-sided CI)
        if (dof >= 60) return 2.000;
        if (dof >= 30) return 2.042;
        if (dof >= 20) return 2.086;
        if (dof >= 15) return 2.131;
        if (dof >= 10) return 2.228;
        if (dof >= 8)  return 2.306;
        if (dof >= 6)  return 2.447;
        if (dof >= 5)  return 2.571;
        if (dof >= 4)  return 2.776;
        if (dof >= 3)  return 3.182;
        if (dof >= 2)  return 4.303;
        return 12.706; // dof == 1
    }

    /// <summary>Rational approximation of the standard normal quantile (Abramowitz and Stegun 26.2.17).</summary>
    private static double NormalQuantile(double p)
    {
        if (p <= 0) return double.NegativeInfinity;
        if (p >= 1) return double.PositiveInfinity;
        if (p < 0.5) return -NormalQuantile(1 - p);
        double t = Math.Sqrt(-2.0 * Math.Log(1 - p));
        const double c0 = 2.515517, c1 = 0.802853, c2 = 0.010328;
        const double d1 = 1.432788, d2 = 0.189269, d3 = 0.001308;
        return t - (c0 + c1 * t + c2 * t * t) / (1 + d1 * t + d2 * t * t + d3 * t * t * t);
    }
}
