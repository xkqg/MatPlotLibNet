// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Gaussian kernel density estimation: the one bandwidth rule and the one density formula every density the
/// library draws is estimated with (a density curve, a pair grid's diagonal, a ridgeline's ridges).</summary>
internal static class GaussianKde
{
    private const double Sqrt2Pi = 2.5066282746310002; // Math.Sqrt(2 * Math.PI)

    /// <summary>Silverman's rule of thumb (<c>1.06 * σ * n^(-0.2)</c>): the factor the library's own themes smooth a density with.</summary>
    internal const double SilvermanFactor = 1.06;

    /// <summary>Scott's factor (<c>σ * n^(-0.2)</c>): scipy's <c>gaussian_kde(bw_method='scott')</c>, which seaborn's <c>kdeplot</c> smooths
    /// with, and matplotlib's own <c>mlab.GaussianKDE</c>, which its <c>violinplot</c> smooths with.</summary>
    internal const double ScottFactor = 1.0;

    /// <summary>
    /// The normal-reference bandwidth: <paramref name="factor"/> times the sample's standard deviation (over n - 1) times
    /// <c>n^(-0.2)</c>; Silverman's rule of thumb at <see cref="SilvermanFactor"/>, Scott's at <see cref="ScottFactor"/>.
    /// <para>Data without a spread (one value, or every value the same) has no σ; the size of its value stands in for it,
    /// R's <c>bw.nrd0</c> rule, so the bandwidth stays in the data's units (data in units 100 times smaller has a bandwidth
    /// 100 times larger), and 1 stands in when that value is 0. "Without a spread" means the smallest value equals the
    /// largest: equal values whose mean does not round back to them (three times 0.1) deviate from it by a rounding error,
    /// not by a spread.</para>
    /// </summary>
    /// <param name="sortedData">Data values sorted in ascending order.</param>
    /// <param name="factor">The factor of the rule (<see cref="SilvermanFactor"/>, <see cref="ScottFactor"/>).</param>
    /// <returns>The estimated bandwidth; 1.0 for no data at all.</returns>
    internal static double Bandwidth(double[] sortedData, double factor)
    {
        int n = sortedData.Length;
        if (n == 0) return 1.0;
        double sigma = sortedData[0] < sortedData[^1] ? sortedData.AsSpan().StdDev(ddof: 1) : 0;
        if (!(sigma > 0))
        {
            double size = Math.Abs(sortedData[0]);
            sigma = size > 0 ? size : 1.0;
        }

        return factor * sigma * Math.Pow(n, -0.2);
    }

    /// <summary>One bandwidth for the densities of several groups, so they are smoothed alike and compare with each other:
    /// the mean of the groups' own Silverman bandwidths (<see cref="Bandwidth"/> at <see cref="SilvermanFactor"/>), ggridges' joint
    /// bandwidth. The shift between the groups is not spread: Silverman over the groups pooled would count it as spread and smooth
    /// every group by it.
    /// <para>A group without a spread (every value the same) has no width of its own to give and is left out of the mean;
    /// when no group spreads, the groups pooled are the only scale there is, and the bandwidth is Silverman's over them.</para></summary>
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
                sum += Bandwidth(group, SilvermanFactor);
                spreading++;
            }
        }

        return spreading > 0 ? sum / spreading : Bandwidth([.. sortedGroups.SelectMany(g => g).Order()], SilvermanFactor);
    }

    extension(double[] sample)
    {
        /// <summary>The density curve of a sample: the one rule a density curve and a pair grid's diagonal are both estimated
        /// by. The drawable observations, ascending (<c>SortedDrawableValues</c>: a missing value and an infinity are not
        /// observations), are smoothed with <paramref name="bandwidth"/> when it is a positive finite number and with the
        /// <paramref name="rule"/>'s bandwidth otherwise (<see cref="Bandwidth"/> at <see cref="DensityRule.BandwidthFactor"/>), and
        /// evaluated on the rule's number of points (<see cref="Evaluate"/>).</summary>
        /// <param name="bandwidth">The width of the Gaussian kernel, in data units; <see langword="null"/>, or a value that is not a
        /// positive finite number, takes the rule's.</param>
        /// <param name="rule">How the density is estimated where no bandwidth is given.</param>
        /// <returns>The curve; <see langword="null"/> when no observation can be drawn.</returns>
        internal XYCurve? DensityCurve(double? bandwidth, DensityRule rule)
        {
            var sorted = sample.SortedDrawableValues();
            if (sorted.Length == 0) return null;
            double h = bandwidth is { } given && given.IsPositiveFinite() ? given : Bandwidth(sorted, rule.BandwidthFactor);
            return Evaluate(sorted, h, rule.Points);
        }
    }

    /// <summary>
    /// Evaluates the KDE at <paramref name="numPoints"/> evenly spaced X values
    /// covering [min − 3h, max + 3h] where h is the bandwidth: <see cref="DensityAt"/> on <see cref="Grid"/>.
    /// </summary>
    /// <param name="sortedData">Data values sorted in ascending order.</param>
    /// <param name="bandwidth">Gaussian kernel bandwidth.</param>
    /// <param name="numPoints">Number of evaluation points.</param>
    /// <returns>An <see cref="XYCurve"/> with evaluation X coordinates and corresponding density estimates in Y.</returns>
    internal static XYCurve Evaluate(double[] sortedData, double bandwidth, int numPoints)
    {
        if (sortedData.Length == 0) return new([], []);
        double[] xs = Grid(sortedData[0], sortedData[^1], bandwidth, numPoints);
        return new(xs, DensityAt(sortedData, bandwidth, xs));
    }

    /// <summary>The grid a density is estimated on: <paramref name="numPoints"/> evenly spaced values from the smallest
    /// observation minus three bandwidths to the largest plus three, where a Gaussian kernel has fallen to 1.1 % of its
    /// peak. Given the smallest and the largest observation of several groups, every group's density stands on the same
    /// x values (a ridgeline).</summary>
    /// <param name="min">The smallest observation.</param>
    /// <param name="max">The largest observation.</param>
    /// <param name="bandwidth">Gaussian kernel bandwidth.</param>
    /// <param name="numPoints">Number of grid points; at least two.</param>
    /// <returns>The grid, ascending.</returns>
    internal static double[] Grid(double min, double max, double bandwidth, int numPoints)
    {
        // numpy's linspace over the sample and three bandwidths either side.
        return new EqualWidthBins(min - 3 * bandwidth, max + 3 * bandwidth, numPoints - 1).Edges();
    }

    /// <summary>The density of every group on one grid with one bandwidth, in the order of the groups: what a ridgeline draws, so
    /// every ridge stands on the same x values.</summary>
    /// <param name="groups">The observations of each group, sorted ascending (see <see cref="DensityAt"/>).</param>
    /// <param name="bandwidth">Gaussian kernel bandwidth, the same for all of them.</param>
    /// <param name="grid">The points to estimate at, shared by all of them.</param>
    /// <returns>The density of each group at each grid point.</returns>
    internal static double[][] JointDensities(double[][] groups, double bandwidth, double[] grid)
    {
        double[][] densities = new double[groups.Length][];
        for (int g = 0; g < groups.Length; g++)
        {
            densities[g] = DensityAt(groups[g], bandwidth, grid);
        }

        return densities;
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
