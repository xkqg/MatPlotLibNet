// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>A sorted sample fitted to a reference distribution, as a probability plot draws it (scipy's <c>probplot</c>): the
/// plotting position of each observation, the reference's quantile at it, and the least-squares line through the points.</summary>
/// <param name="Positions">Filliben's plotting position of each rank, in ascending order.</param>
/// <param name="Theoretical">The reference's quantile at each position: the X of each point.</param>
/// <param name="StartY">The height of the least-squares line at the first quantile.</param>
/// <param name="EndY">The height of the least-squares line at the last quantile.</param>
internal readonly record struct ProbabilityFit(double[] Positions, double[] Theoretical, double StartY, double EndY)
{
    /// <summary>Filliben's plotting positions for <paramref name="count"/> observations (Filliben 1975, scipy's
    /// <c>_calc_uniform_order_statistic_medians</c>): the median of the k-th smallest of <paramref name="count"/> uniform
    /// observations on (0, 1), in ascending order. The last is 0.5^(1/n), the first one minus that, and the ones between
    /// (k - 0.3175) / (n + 0.365) for rank k; they are symmetric around 0.5.</summary>
    /// <param name="count">The number of observations, two or more.</param>
    /// <returns>The positions, ascending.</returns>
    internal static double[] FillibenPositions(int count)
    {
        var positions = new double[count];
        positions[^1] = Math.Pow(0.5, 1.0 / count);
        positions[0] = 1 - positions[^1];
        for (int rank = 2; rank < count; rank++)
        {
            positions[rank - 1] = (rank - 0.3175) / (count + 0.365);
        }

        return positions;
    }
}

/// <summary>The one owner of how a sample is fitted to a reference distribution for a probability plot.</summary>
internal static class ProbabilityFitExtensions
{
    extension(QQReference reference)
    {
        /// <summary>The k-th smallest observation against the reference's quantile at the plotting position of its rank, and the
        /// least-squares line through them (scipy's <c>linregress</c> of the sample on the quantiles), evaluated at the first and
        /// the last quantile.</summary>
        /// <param name="ordered">The observations, ascending; at least two.</param>
        /// <returns>The positions, the quantiles and the line.</returns>
        /// <exception cref="InvalidOperationException">The reference has no finite quantile at some position: its quantile
        /// function broke its contract.</exception>
        internal ProbabilityFit FitTo(double[] ordered)
        {
            double[] positions = ProbabilityFit.FillibenPositions(ordered.Length);
            double[] theoretical = ((double[])[.. positions.Select(reference.Quantile)])
                .ThrowIfNotGivenFor(positions.Length, "The quantile function of the reference", reference.Name);
            double[] fit = LeastSquares.PolyFit(theoretical, ordered, 1);
            double[] ends = LeastSquares.PolyEval(fit, [theoretical[0], theoretical[^1]]);
            return new ProbabilityFit(positions, theoretical, ends[0], ends[1]);
        }
    }
}
