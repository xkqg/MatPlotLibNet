// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Gaussian kernel density estimation utilities.</summary>
internal static class GaussianKde
{
    private const double Sqrt2Pi = 2.5066282746310002; // Math.Sqrt(2 * Math.PI)

    /// <summary>
    /// Computes Silverman's rule-of-thumb bandwidth: <c>1.06 * σ * n^(-0.2)</c>.
    /// </summary>
    /// <param name="sortedData">Data values sorted in ascending order.</param>
    /// <returns>The estimated bandwidth. Returns 1.0 for fewer than 2 points or zero variance.</returns>
    internal static double SilvermanBandwidth(double[] sortedData)
    {
        int n = sortedData.Length;
        if (n < 2) return 1.0;
        double sum = 0;
        for (int i = 0; i < n; i++) sum += sortedData[i];
        double mean = sum / n;
        double varSum = 0;
        for (int i = 0; i < n; i++) { double d = sortedData[i] - mean; varSum += d * d; }
        double sigma = Math.Sqrt(varSum / (n - 1));
        if (sigma <= 0) return 1.0;
        return 1.06 * sigma * Math.Pow(n, -0.2);
    }

    /// <summary>
    /// Evaluates the KDE at <paramref name="numPoints"/> evenly spaced X values
    /// covering [min − 3h, max + 3h] where h is the bandwidth.
    /// </summary>
    /// <param name="sortedData">Data values sorted in ascending order.</param>
    /// <param name="bandwidth">Gaussian kernel bandwidth.</param>
    /// <param name="numPoints">Number of evaluation points (default 100).</param>
    /// <returns>Tuple of evaluation X coordinates and corresponding density estimates.</returns>
    internal static (double[] X, double[] Density) Evaluate(double[] sortedData, double bandwidth, int numPoints = 100)
    {
        if (sortedData.Length == 0) return ([], []);
        double lo = sortedData[0] - 3 * bandwidth;
        double hi = sortedData[^1] + 3 * bandwidth;
        double step = (hi - lo) / (numPoints - 1);
        double n = sortedData.Length;
        double[] xs = new double[numPoints];
        double[] density = new double[numPoints];
        for (int j = 0; j < numPoints; j++)
        {
            xs[j] = lo + step * j;
            double kernelSum = 0;
            for (int i = 0; i < sortedData.Length; i++)
            {
                double u = (xs[j] - sortedData[i]) / bandwidth;
                kernelSum += Math.Exp(-0.5 * u * u);
            }
            density[j] = kernelSum / (n * bandwidth * Sqrt2Pi);
        }
        return (xs, density);
    }
}
