// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The Kraskov-Stögbauer-Grassberger (2004) estimate of mutual information by nearest neighbours, in the conditional form of
/// Frenzel and Pompe (2007): it needs no bins, and with the transfer entropy of one series to another it is the estimate of
/// <c>I(X; Y | Z)</c>. For each sample the distance <c>ε</c> to the k-th nearest other sample is found in the joint space of the three
/// variables (the maximum norm over all their columns); the samples strictly inside <c>ε</c> are counted in the subspace (X, Z), in
/// (Y, Z) and in Z, and the estimate is <c>ψ(k) - ⟨ψ(n_xz + 1) + ψ(n_yz + 1) - ψ(n_z + 1)⟩</c>, averaged over the samples.
/// The counts are strict, so a neighbour exactly on the edge is not inside; with no columns in Z every sample is inside it, and the
/// estimate is the plain mutual information of Kraskov's first algorithm. Brute force, O(n²) in the number of samples.</summary>
internal static class KraskovEstimator
{
    /// <summary>The conditional mutual information of X and Y given Z, in nats. It can be negative for variables that carry no
    /// information about each other; it is not clamped, because clamping would bias an average of estimates upward.</summary>
    /// <param name="x">The columns of the first variable, all of the same length.</param>
    /// <param name="y">The columns of the second variable.</param>
    /// <param name="z">The columns of the variable conditioned on; none for the plain mutual information.</param>
    /// <param name="neighbours">The k of the k-th nearest neighbour; at least 1 and below the number of samples.</param>
    /// <exception cref="ArgumentException">A variable has no column, or the columns differ in length.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="neighbours"/> is below 1, or not below the number of samples.</exception>
    internal static double ConditionalMutualInformation(
        IReadOnlyList<double[]> x, IReadOnlyList<double[]> y, IReadOnlyList<double[]> z, int neighbours)
    {
        if (x.Count == 0) throw new ArgumentException("A variable needs at least one column.", nameof(x));
        if (y.Count == 0) throw new ArgumentException("A variable needs at least one column.", nameof(y));
        int n = x[0].Length;
        foreach (var column in x.Concat(y).Concat(z))
        {
            if (column.Length != n)
                throw new ArgumentException($"All columns must have the same length ({n}); one has {column.Length}.");
        }

        if (neighbours < 1 || neighbours >= n)
            throw new ArgumentOutOfRangeException(nameof(neighbours), neighbours, $"The neighbours are from 1 to {n - 1}, one less than the samples.");

        // ψ at the whole numbers the counts can take: 1 to n.
        var psi = new double[n + 1];
        for (int m = 1; m <= n; m++) psi[m] = Gamma.Digamma(m);

        var dx = new double[n];
        var dy = new double[n];
        var dz = new double[n];
        var joint = new double[n - 1];
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            int m = 0;
            for (int j = 0; j < n; j++)
            {
                if (j == i) continue;
                dx[j] = Distance(x, i, j);
                dy[j] = Distance(y, i, j);
                dz[j] = Distance(z, i, j);
                joint[m++] = Math.Max(dx[j], Math.Max(dy[j], dz[j]));
            }

            Array.Sort(joint);
            double epsilon = joint[neighbours - 1];

            int nxz = 0, nyz = 0, nz = 0;
            for (int j = 0; j < n; j++)
            {
                if (j == i || !(dz[j] < epsilon)) continue;
                nz++;
                if (dx[j] < epsilon) nxz++;
                if (dy[j] < epsilon) nyz++;
            }

            sum += psi[nxz + 1] + psi[nyz + 1] - psi[nz + 1];
        }

        return Gamma.Digamma(neighbours) - sum / n;
    }

    /// <summary>The distance of two samples in a variable: the largest difference over its columns (0 when it has none).</summary>
    private static double Distance(IReadOnlyList<double[]> columns, int i, int j)
    {
        double max = 0;
        for (int c = 0; c < columns.Count; c++)
        {
            double d = Math.Abs(columns[c][i] - columns[c][j]);
            if (d > max) max = d;
        }

        return max;
    }
}
