// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Gaussian kernel density estimation: the one bandwidth rule and the one density formula every density the
/// library draws is estimated with (a density curve, a pair grid's diagonal, a ridgeline's ridges).</summary>
internal static class GaussianKde
{
    private const double Sqrt2Pi = 2.5066282746310002; // Math.Sqrt(2 * Math.PI)

    /// <summary>
    /// Computes Silverman's rule-of-thumb bandwidth: <c>1.06 * σ * n^(-0.2)</c>.
    /// <para>Data without a spread (one value, or every value the same) has no σ; the size of its value stands in for it,
    /// R's <c>bw.nrd0</c> rule, so the bandwidth stays in the data's units (data in units 100 times smaller has a bandwidth
    /// 100 times larger), and 1 stands in when that value is 0. "Without a spread" means the smallest value equals the
    /// largest: equal values whose mean does not round back to them (three times 0.1) deviate from it by a rounding error,
    /// not by a spread.</para>
    /// </summary>
    /// <param name="sortedData">Data values sorted in ascending order.</param>
    /// <returns>The estimated bandwidth; 1.0 for no data at all.</returns>
    internal static double SilvermanBandwidth(double[] sortedData)
    {
        int n = sortedData.Length;
        if (n == 0) return 1.0;
        double sigma = sortedData[0] < sortedData[^1] ? StandardDeviation(sortedData) : 0;
        if (!(sigma > 0))
        {
            double size = Math.Abs(sortedData[0]);
            sigma = size > 0 ? size : 1.0;
        }

        return 1.06 * sigma * Math.Pow(n, -0.2);
    }

    /// <summary>One bandwidth for the densities of several groups, so they are smoothed alike and compare with each other:
    /// the mean of the groups' own <see cref="SilvermanBandwidth"/>s, ggridges' joint bandwidth. The shift between the
    /// groups is not spread: Silverman over the groups pooled would count it as spread and smooth every group by it.
    /// <para>A group without a spread (every value the same) has no width of its own to give and is left out of the mean;
    /// when no group spreads, the groups pooled are the only scale there is, and the bandwidth is
    /// <see cref="SilvermanBandwidth"/> over them.</para></summary>
    /// <param name="sortedGroups">The groups, each sorted ascending and holding at least one value.</param>
    /// <returns>The joint bandwidth, in the units of the values.</returns>
    internal static double JointBandwidth(IReadOnlyList<double[]> sortedGroups)
    {
        double sum = 0;
        int spreading = 0;
        foreach (double[] group in sortedGroups)
        {
            if (group[0] < group[^1])
            {
                sum += SilvermanBandwidth(group);
                spreading++;
            }
        }

        return spreading > 0 ? sum / spreading : SilvermanBandwidth([.. sortedGroups.SelectMany(g => g).Order()]);
    }

    /// <summary>The sample standard deviation of <paramref name="data"/> (divided by n − 1); at least two values.</summary>
    private static double StandardDeviation(double[] data)
    {
        int n = data.Length;
        double sum = 0;
        for (int i = 0; i < n; i++) sum += data[i];
        double mean = sum / n;
        double varSum = 0;
        for (int i = 0; i < n; i++) { double d = data[i] - mean; varSum += d * d; }
        return Math.Sqrt(varSum / (n - 1));
    }

    /// <summary>
    /// Evaluates the KDE at <paramref name="numPoints"/> evenly spaced X values
    /// covering [min − 3h, max + 3h] where h is the bandwidth: <see cref="DensityAt"/> on <see cref="Grid"/>.
    /// </summary>
    /// <param name="sortedData">Data values sorted in ascending order.</param>
    /// <param name="bandwidth">Gaussian kernel bandwidth.</param>
    /// <param name="numPoints">Number of evaluation points (default 100).</param>
    /// <returns>An <see cref="XYCurve"/> with evaluation X coordinates and corresponding density estimates in Y.</returns>
    internal static XYCurve Evaluate(double[] sortedData, double bandwidth, int numPoints = 100)
    {
        if (sortedData.Length == 0) return new([], []);
        double[] xs = Grid(sortedData, bandwidth, numPoints);
        return new(xs, DensityAt(sortedData, bandwidth, xs));
    }

    /// <summary>The grid a density of <paramref name="sortedData"/> is estimated on: <paramref name="numPoints"/> evenly
    /// spaced values from the smallest value minus three bandwidths to the largest plus three, where a Gaussian kernel has
    /// fallen to 1.1 % of its peak. Estimated on the grid of the data of several groups pooled, every group's density stands
    /// on the same x values (a ridgeline).</summary>
    /// <param name="sortedData">Data values sorted in ascending order; at least one.</param>
    /// <param name="bandwidth">Gaussian kernel bandwidth.</param>
    /// <param name="numPoints">Number of grid points; at least two.</param>
    /// <returns>The grid, ascending.</returns>
    internal static double[] Grid(double[] sortedData, double bandwidth, int numPoints)
    {
        double lo = sortedData[0] - 3 * bandwidth;
        double hi = sortedData[^1] + 3 * bandwidth;
        double step = (hi - lo) / (numPoints - 1);
        double[] xs = new double[numPoints];
        for (int j = 0; j < numPoints; j++) xs[j] = lo + step * j;
        return xs;
    }

    /// <summary>The Gaussian kernel density of <paramref name="data"/> at each of <paramref name="xs"/>: one kernel of width
    /// <paramref name="bandwidth"/> on each observation, averaged. The points need not be a grid of this data.</summary>
    /// <param name="data">The observations; at least one. Sorted ascending wherever the result must be reproducible bit for
    /// bit, since the kernels are summed in the order given.</param>
    /// <param name="bandwidth">Gaussian kernel bandwidth.</param>
    /// <param name="xs">The points to estimate at.</param>
    /// <returns>The density at each point, in the order of <paramref name="xs"/>.</returns>
    internal static double[] DensityAt(double[] data, double bandwidth, double[] xs)
    {
        double n = data.Length;
        double[] density = new double[xs.Length];
        for (int j = 0; j < xs.Length; j++)
        {
            double kernelSum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                double u = (xs[j] - data[i]) / bandwidth;
                kernelSum += Math.Exp(-0.5 * u * u);
            }
            density[j] = kernelSum / (n * bandwidth * Sqrt2Pi);
        }
        return density;
    }
}
