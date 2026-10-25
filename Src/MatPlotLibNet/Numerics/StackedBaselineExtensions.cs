// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Numerics;

/// <summary>Extension methods for <see cref="StackedBaseline"/> that compute per-layer baseline offsets.</summary>
/// <remarks>Every strategy is a rule for the first line, the bottom edge of the lowest layer; each layer then stands on the
/// one below it. <c>Wiggle</c> and <c>WeightedWiggle</c> are matplotlib's <c>stackplot</c> baselines of those names
/// (lib/matplotlib/stackplot.py), after Byron and Wattenberg, "Stacked Graphs - Geometry and Aesthetics".</remarks>
internal static class StackedBaselineExtensions
{
    extension(StackedBaseline baseline)
    {
        /// <summary>Computes baseline offsets for each layer at each data point.
        /// Returns <c>baselines[layer][point]</c> — the Y offset at which each layer starts.</summary>
        /// <param name="ySets">Layer data — <c>ySets[layer][point]</c>.</param>
        /// <param name="n">Number of data points. When &gt; 0, baselines are padded to this length
        /// so jagged YSets are safe; when 0 (default), falls back to max layer length.</param>
        public double[][] ComputeFor(double[][] ySets, int n = 0)
        {
            if (ySets.Length == 0) return [];
            if (n <= 0) n = ySets.Max(s => s.Length);

            return baseline switch
            {
                StackedBaseline.Symmetric      => ComputeSymmetric(ySets, n),
                StackedBaseline.Wiggle         => StackOn(WiggleFirstLine(ySets, n), ySets),
                StackedBaseline.WeightedWiggle => StackOn(WeightedWiggleFirstLine(ySets, n), ySets),
                _                              => StackOn(new double[n], ySets),
            };
        }
    }

    extension(double[][] baselines)
    {
        /// <summary>The top edge of every layer: its baseline plus its own value, 0 past the end of a layer shorter than
        /// <paramref name="n"/>. Whoever fills a layer between its baseline and this edge (the stacked-area series, a stream
        /// graph) reads the stack by the one rule.</summary>
        /// <param name="ySets">Layer data — <c>ySets[layer][point]</c>, the layers this array was computed for.</param>
        /// <param name="n">The number of data points; the edges are as long as this.</param>
        /// <returns>One array of <paramref name="n"/> points per layer.</returns>
        public double[][] TopEdgesFor(double[][] ySets, int n)
        {
            var tops = new double[ySets.Length][];
            for (int layer = 0; layer < ySets.Length; layer++)
            {
                tops[layer] = new double[n];
                for (int j = 0; j < n; j++)
                    tops[layer][j] = baselines[layer][j] + Value(ySets, layer, j);
            }

            return tops;
        }
    }

    // ── Zero is StackOn(zeros) above; Symmetric: shift so midpoint of total stack is at y = 0 ────────────

    private static double[][] ComputeSymmetric(double[][] ySets, int n)
    {
        var total = Totals(ySets, n);

        var baselines = StackOn(new double[n], ySets);
        for (int layer = 0; layer < ySets.Length; layer++)
            for (int j = 0; j < n; j++)
                baselines[layer][j] -= total[j] / 2.0;

        return baselines;
    }

    // ── Wiggle: matplotlib's baseline='wiggle' ───────────────────────────────

    /// <summary>The first line of matplotlib's <c>'wiggle'</c>: <c>-(sum over layers i of y[i] * (m - 0.5 - i)) / m</c>
    /// for <c>m</c> layers counted from the bottom. It puts the middles of the layers as close to the x axis as the stack
    /// allows, so the lower layers weigh more than the upper ones; with equal layers it is the symmetric baseline.</summary>
    private static double[] WiggleFirstLine(double[][] ySets, int n)
    {
        int m = ySets.Length;
        var firstLine = new double[n];
        for (int layer = 0; layer < m; layer++)
        {
            double weight = m - 0.5 - layer;
            for (int j = 0; j < n; j++)
                firstLine[j] += Value(ySets, layer, j) * weight;
        }

        for (int j = 0; j < n; j++)
            firstLine[j] /= -m;
        return firstLine;
    }

    // ── WeightedWiggle: matplotlib's baseline='weighted_wiggle' (the streamgraph layout) ───

    /// <summary>The first line of matplotlib's <c>'weighted_wiggle'</c>: half the total below the x axis, moved by a slope
    /// term that follows how each layer changes from the previous point, weighted by where the layer lies in the stack.
    /// <c>moveUp</c> is the share of the stack at or above the middle of a layer; a layer that grows while it lies mostly
    /// above the middle of the stack pulls the first line up. Point 0 has no previous point, so its slope term is 0.</summary>
    /// <remarks>matplotlib sums the slope terms with <c>cumsum</c>, so one missing value would blank every later point of
    /// the baseline. A point whose slope cannot be measured (a missing value there or at the point before) adds nothing
    /// here, so a gap stays at its own point, as it is for every other baseline and for the layers themselves.</remarks>
    private static double[] WeightedWiggleFirstLine(double[][] ySets, int n)
    {
        var total = Totals(ySets, n);
        var firstLine = new double[n];
        double slopeTerm = 0;
        for (int j = 0; j < n; j++)
        {
            if (j > 0)
            {
                // matplotlib: inv_total is 1/total where total > 0 and 0 elsewhere, to avoid dividing by zero.
                double inverseTotal = total[j] > 0 ? 1.0 / total[j] : 0.0;
                double stacked = 0;
                double centre = 0;
                for (int layer = 0; layer < ySets.Length; layer++)
                {
                    double y = Value(ySets, layer, j);
                    stacked += y;
                    double increase = y - Value(ySets, layer, j - 1);
                    double moveUp = (total[j] - stacked + 0.5 * y) * inverseTotal;
                    centre += (moveUp - 0.5) * increase;
                }

                if (double.IsFinite(centre)) slopeTerm += centre;
            }

            firstLine[j] = slopeTerm - 0.5 * total[j];
        }

        return firstLine;
    }

    // ── Shared: the stack, the totals, a padded read ────────────────────────────

    /// <summary>Stands every layer on the one below it, the lowest on <paramref name="firstLine"/>.</summary>
    private static double[][] StackOn(double[] firstLine, double[][] ySets)
    {
        var baselines = new double[ySets.Length][];
        for (int layer = 0; layer < ySets.Length; layer++)
        {
            baselines[layer] = new double[firstLine.Length];
            for (int j = 0; j < firstLine.Length; j++)
                baselines[layer][j] = layer == 0 ? firstLine[j] : baselines[layer - 1][j] + Value(ySets, layer - 1, j);
        }

        return baselines;
    }

    private static double[] Totals(double[][] ySets, int n)
    {
        var total = new double[n];
        for (int layer = 0; layer < ySets.Length; layer++)
            for (int j = 0; j < n; j++)
                total[j] += Value(ySets, layer, j);
        return total;
    }

    private static double Value(double[][] ySets, int layer, int j)
        => j < ySets[layer].Length ? ySets[layer][j] : 0.0;
}
