// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Numerics.Tensors;

namespace MatPlotLibNet.Numerics;

/// <summary>Histogram computation result.</summary>
/// <param name="Counts">Count of data points in each bin.</param>
/// <param name="Edges">Bin edge values — length is <c>bins + 1</c>.</param>
public readonly record struct HistogramResult(double[] Counts, double[] Edges);

/// <summary>Result of <see cref="NpStats.Unique"/>.</summary>
/// <param name="Values">Distinct values in ascending order.</param>
/// <param name="Counts">Occurrence count for each distinct value.</param>
public readonly record struct UniqueResult(double[] Values, int[] Counts);

/// <summary>Statistical utilities analogous to NumPy's statistical functions.</summary>
public static class NpStats
{
    // -------------------------------------------------------------------------
    // Diff
    // -------------------------------------------------------------------------

    /// <summary>Computes the n-th discrete difference: <c>v[i+1] - v[i]</c> applied <paramref name="n"/> times.</summary>
    /// <param name="v">Input vector.</param>
    /// <param name="n">Number of times to apply the difference operator (default 1).</param>
    /// <returns>Vector of length <c>v.Length - n</c>.</returns>
    public static Vec Diff(Vec v, int n = 1)
    {
        if (n == 0) return v;
        double[] src = v.Data;
        for (int pass = 0; pass < n; pass++)
        {
            var dst = new double[src.Length - 1];
            TensorPrimitives.Subtract(
                (ReadOnlySpan<double>)src.AsSpan(1),
                (ReadOnlySpan<double>)src.AsSpan(0, src.Length - 1),
                dst);
            src = dst;
        }
        return new Vec(src);
    }

    // -------------------------------------------------------------------------
    // Median
    // -------------------------------------------------------------------------

    /// <summary>Returns the median value using linear interpolation for even-length inputs.</summary>
    /// <param name="v">Input vector (must be non-empty).</param>
    /// <returns>Median value.</returns>
    public static double Median(Vec v)
    {
        double[] sorted = v.Data.ToArray();
        Array.Sort(sorted);
        int n = sorted.Length;
        return n % 2 == 1
            ? sorted[n / 2]
            : (sorted[n / 2 - 1] + sorted[n / 2]) * 0.5;
    }

    // -------------------------------------------------------------------------
    // Histogram
    // -------------------------------------------------------------------------

    /// <summary>Computes a histogram with uniform bins over the data range.</summary>
    /// <param name="v">Input data vector.</param>
    /// <param name="bins">Number of equal-width bins.</param>
    /// <returns><see cref="HistogramResult"/> with <c>Counts</c> (length <paramref name="bins"/>) and <c>Edges</c> (length <c>bins + 1</c>).</returns>
    public static HistogramResult Histogram(Vec v, int bins)
    {
        double min = VectorMath.Min(v.Data);
        double max = VectorMath.Max(v.Data);
        double width = (max - min) / bins;

        var edges = new double[bins + 1];
        for (int i = 0; i <= bins; i++)
            edges[i] = min + i * width;

        var counts = new double[bins];
        foreach (double x in v.Data)
        {
            int b = width > 0 ? (int)((x - min) / width) : 0;
            b = Math.Clamp(b, 0, bins - 1);   // last edge belongs to last bin
            counts[b]++;
        }
        return new HistogramResult(counts, edges);
    }

    // -------------------------------------------------------------------------
    // Argsort
    // -------------------------------------------------------------------------

    /// <summary>Returns the indices that would sort <paramref name="v"/> in ascending order.</summary>
    /// <param name="v">Input vector to sort.</param>
    /// <returns>Index array such that <c>v[result[i]]</c> is in ascending order.</returns>
    public static int[] Argsort(Vec v)
    {
        int[] idx = Enumerable.Range(0, v.Length).ToArray();
        Array.Sort(idx, (a, b) => v[a].CompareTo(v[b]));
        return idx;
    }

    // -------------------------------------------------------------------------
    // Unique
    // -------------------------------------------------------------------------

    /// <summary>Returns distinct values (ascending) and their occurrence counts.</summary>
    /// <param name="v">Input vector.</param>
    /// <returns><see cref="UniqueResult"/> with sorted distinct values and per-value occurrence counts.</returns>
    public static UniqueResult Unique(Vec v)
    {
        double[] sorted = v.Data.ToArray();
        Array.Sort(sorted);

        var vals = new List<double>();
        var cnts = new List<int>();
        int i = 0;
        while (i < sorted.Length)
        {
            double cur = sorted[i];
            int run = 0;
            while (i < sorted.Length && sorted[i] == cur) { run++; i++; }
            vals.Add(cur);
            cnts.Add(run);
        }
        return new UniqueResult([.. vals], [.. cnts]);
    }

    // -------------------------------------------------------------------------
    // Covariance
    // -------------------------------------------------------------------------

    /// <summary>Sample covariance matrix (divided by n−1) for a list of column arrays.</summary>
    /// <param name="columns">Array of column data arrays; each inner array must have the same length.</param>
    /// <returns>p × p covariance matrix where p = <c>columns.Length</c>.</returns>
    public static Mat Cov(double[][] columns)
    {
        int p = columns.Length;
        int n = columns[0].Length;
        double[] means = columns.Select(c => VectorMath.Sum(c) / n).ToArray();

        var cov = new double[p, p];
        for (int i = 0; i < p; i++)
            for (int j = i; j < p; j++)
            {
                double s = 0;
                for (int k = 0; k < n; k++)
                    s += (columns[i][k] - means[i]) * (columns[j][k] - means[j]);
                cov[i, j] = cov[j, i] = s / (n - 1);
            }
        return new Mat(cov);
    }

    // -------------------------------------------------------------------------
    // Correlation coefficient
    // -------------------------------------------------------------------------

    /// <summary>Correlation coefficient matrix normalised from <see cref="Cov"/>.</summary>
    /// <param name="columns">Array of column data arrays; each inner array must have the same length.</param>
    /// <returns>p × p Pearson correlation matrix; diagonal entries are exactly 1.0.</returns>
    public static Mat Corrcoef(double[][] columns)
    {
        Mat cov = Cov(columns);
        int p = columns.Length;
        double[] std = new double[p];
        for (int i = 0; i < p; i++)
            std[i] = Math.Sqrt(cov[i, i]);

        var corr = new double[p, p];
        for (int i = 0; i < p; i++)
            for (int j = 0; j < p; j++)
                corr[i, j] = (std[i] > 0 && std[j] > 0)
                    ? cov[i, j] / (std[i] * std[j])
                    : (i == j ? 1.0 : 0.0);
        return new Mat(corr);
    }
}
