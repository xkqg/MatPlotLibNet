// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Numerics;

/// <summary>Extension methods for <see cref="StackedBaseline"/> that compute per-layer baseline offsets.</summary>
internal static class StackedBaselineExtensions
{
    /// <summary>Computes baseline offsets for each layer at each data point.
    /// Returns <c>baselines[layer][point]</c> — the Y offset at which each layer starts.</summary>
    /// <param name="baseline">The baseline strategy to apply.</param>
    /// <param name="ySets">Layer data — <c>ySets[layer][point]</c>.</param>
    /// <param name="n">Number of data points. When &gt; 0, baselines are padded to this length
    /// so jagged YSets are safe; when 0 (default), falls back to max layer length.</param>
    public static double[][] ComputeFor(this StackedBaseline baseline, double[][] ySets, int n = 0)
        => baseline switch
        {
            StackedBaseline.Symmetric      => ComputeSymmetric(ySets, n),
            StackedBaseline.Wiggle         => ComputeWiggle(ySets, n),
            StackedBaseline.WeightedWiggle => ComputeWeightedWiggle(ySets, n),
            _                              => ComputeZero(ySets, n),
        };

    // ── Zero: cumulative from zero ───────────────────────────────────────────

    private static double[][] ComputeZero(double[][] ySets, int n)
    {
        int layers = ySets.Length;
        if (n <= 0) n = layers > 0 ? ySets.Max(s => s.Length) : 0;
        var baselines = new double[layers][];
        for (int layer = 0; layer < layers; layer++)
        {
            baselines[layer] = new double[n];
            for (int j = 0; j < n; j++)
                baselines[layer][j] = layer == 0 ? 0.0 : baselines[layer - 1][j] + Value(ySets, layer - 1, j);
        }
        return baselines;
    }

    // ── Symmetric: shift so midpoint of total stack is at y = 0 ────────────

    private static double[][] ComputeSymmetric(double[][] ySets, int n)
    {
        int layers = ySets.Length;
        if (n <= 0) n = layers > 0 ? ySets.Max(s => s.Length) : 0;

        var total = new double[n];
        for (int layer = 0; layer < layers; layer++)
            for (int j = 0; j < n; j++)
                total[j] += Value(ySets, layer, j);

        var baselines = ComputeZero(ySets, n);
        for (int layer = 0; layer < layers; layer++)
            for (int j = 0; j < n; j++)
                baselines[layer][j] -= total[j] / 2.0;

        return baselines;
    }

    // ── Wiggle: Byron-Wattenberg to minimise visual slope ────────────────────

    private static double[][] ComputeWiggle(double[][] ySets, int n)
    {
        int layers = ySets.Length;
        if (n <= 0) n = layers > 0 ? ySets.Max(s => s.Length) : 0;

        var total = new double[n];
        for (int layer = 0; layer < layers; layer++)
            for (int j = 0; j < n; j++)
                total[j] += Value(ySets, layer, j);

        var baselines = new double[layers][];
        baselines[0] = new double[n];
        for (int j = 0; j < n; j++)
            baselines[0][j] = -0.5 * total[j];

        for (int layer = 1; layer < layers; layer++)
        {
            baselines[layer] = new double[n];
            for (int j = 0; j < n; j++)
                baselines[layer][j] = baselines[layer - 1][j] + Value(ySets, layer - 1, j);
        }

        return baselines;
    }

    // ── WeightedWiggle: wiggle weighted by layer magnitude ────────────────────

    private static double[][] ComputeWeightedWiggle(double[][] ySets, int n)
    {
        int layers = ySets.Length;
        if (n <= 0) n = layers > 0 ? ySets.Max(s => s.Length) : 0;

        var weights = new double[layers];
        for (int layer = 0; layer < layers; layer++)
        {
            double sum = 0;
            for (int j = 0; j < n; j++) sum += Value(ySets, layer, j);
            weights[layer] = n > 0 ? sum / n : 0;
        }

        double totalWeight = weights.Sum();
        if (totalWeight == 0) return ComputeWiggle(ySets, n);

        var total = new double[n];
        for (int layer = 0; layer < layers; layer++)
            for (int j = 0; j < n; j++)
                total[j] += Value(ySets, layer, j);

        var baselines = new double[layers][];
        baselines[0] = new double[n];
        for (int j = 0; j < n; j++)
        {
            double shift = 0;
            for (int layer = 0; layer < layers; layer++)
                shift += (weights[layer] / totalWeight) * Value(ySets, layer, j);
            baselines[0][j] = -0.5 * shift;
        }

        for (int layer = 1; layer < layers; layer++)
        {
            baselines[layer] = new double[n];
            for (int j = 0; j < n; j++)
                baselines[layer][j] = baselines[layer - 1][j] + Value(ySets, layer - 1, j);
        }

        return baselines;
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static double Value(double[][] ySets, int layer, int j)
        => j < ySets[layer].Length ? ySets[layer][j] : 0.0;
}
