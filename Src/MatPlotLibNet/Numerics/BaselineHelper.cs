// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Numerics;

/// <summary>Computes per-layer baseline offsets for stacked area series.</summary>
internal static class BaselineHelper
{
    /// <summary>
    /// Computes baseline offsets for each layer at each data point.
    /// </summary>
    /// <param name="ySets">Layer data — <c>ySets[layer][point]</c>.</param>
    /// <param name="baseline">The baseline strategy to apply.</param>
    /// <returns>
    /// <c>baselines[layer][point]</c> — the Y offset at which each layer starts.
    /// Every strategy returns the same shape, preserving the postcondition.
    /// </returns>
    public static double[][] ComputeBaselines(double[][] ySets, StackedBaseline baseline)
        => baseline switch
        {
            StackedBaseline.Symmetric      => ComputeSymmetric(ySets),
            StackedBaseline.Wiggle         => ComputeWiggle(ySets),
            StackedBaseline.WeightedWiggle => ComputeWeightedWiggle(ySets),
            _                              => ComputeZero(ySets),
        };

    // ── Zero: cumulative from zero ───────────────────────────────────────────

    private static double[][] ComputeZero(double[][] ySets)
    {
        int layers = ySets.Length;
        int n = layers > 0 ? ySets[0].Length : 0;
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

    private static double[][] ComputeSymmetric(double[][] ySets)
    {
        int layers = ySets.Length;
        int n = layers > 0 ? ySets[0].Length : 0;

        // Compute total sum at each point
        var total = new double[n];
        for (int layer = 0; layer < layers; layer++)
            for (int j = 0; j < n; j++)
                total[j] += Value(ySets, layer, j);

        // Start zero-baselines, then shift by -total/2
        var baselines = ComputeZero(ySets);
        for (int layer = 0; layer < layers; layer++)
            for (int j = 0; j < n; j++)
                baselines[layer][j] -= total[j] / 2.0;

        return baselines;
    }

    // ── Wiggle: Byron-Wattenberg to minimise visual slope ────────────────────

    private static double[][] ComputeWiggle(double[][] ySets)
    {
        int layers = ySets.Length;
        int n = layers > 0 ? ySets[0].Length : 0;

        // baselines[0][j] = -0.5 * Σ ySets[i][j]
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

    private static double[][] ComputeWeightedWiggle(double[][] ySets)
    {
        int layers = ySets.Length;
        int n = layers > 0 ? ySets[0].Length : 0;

        // Weight each layer by its average value (a common approximation)
        var weights = new double[layers];
        for (int layer = 0; layer < layers; layer++)
        {
            double sum = 0;
            for (int j = 0; j < n; j++) sum += Value(ySets, layer, j);
            weights[layer] = n > 0 ? sum / n : 0;
        }

        double totalWeight = weights.Sum();
        if (totalWeight == 0) return ComputeWiggle(ySets);

        // Compute total per-point sum
        var total = new double[n];
        for (int layer = 0; layer < layers; layer++)
            for (int j = 0; j < n; j++)
                total[j] += Value(ySets, layer, j);

        // baselines[0][j] = -Σ (weight_i / totalWeight * ySets[i][j]) * n / 2
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
