// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Computes non-overlapping X offsets for beeswarm (swarm) plot points using greedy circle packing.</summary>
/// <remarks>Points are placed along a fixed category center. For each point, the algorithm finds the smallest
/// X offset (left or right) that avoids overlap with all previously placed points.
/// For N &gt; 1000 points, falls back to deterministic random jitter to keep render time bounded.</remarks>
internal static class BeeswarmLayout
{
    /// <summary>Computes X offsets for a set of values centered at a category position.</summary>
    /// <param name="sortedValues">Data values sorted in ascending order (one per point).</param>
    /// <param name="markerRadius">Radius of each dot in data units.</param>
    /// <param name="categoryCenter">The X position of the category axis tick.</param>
    /// <returns>An array of X positions (one per input value), non-overlapping.</returns>
    internal static double[] Compute(double[] sortedValues, double markerRadius, double categoryCenter)
    {
        int n = sortedValues.Length;
        if (n == 0) return [];
        if (n == 1) return [categoryCenter];

        // For large datasets fall back to deterministic random jitter
        if (n > 1000)
        {
            var rng = new Random(n);
            var fallback = new double[n];
            for (int i = 0; i < n; i++)
                fallback[i] = categoryCenter + (rng.NextDouble() * 2 - 1) * markerRadius * 3;
            return fallback;
        }

        double diameter = markerRadius * 2;
        var placed = new List<Point>(n);
        var result = new double[n];

        for (int i = 0; i < n; i++)
        {
            double y = sortedValues[i];
            double bestOffset = 0;

            // Try offsets 0, ±diameter, ±2*diameter, … until no overlap
            bool placed2 = false;
            for (int step = 0; step <= n; step++)
            {
                double[] candidates = step == 0 ? [categoryCenter] :
                    [categoryCenter + step * diameter, categoryCenter - step * diameter];

                foreach (double cx in candidates)
                {
                    bool overlaps = false;
                    foreach (var p in placed)
                    {
                        double dx = cx - p.X, dy = y - p.Y;
                        if (dx * dx + dy * dy < diameter * diameter)
                        {
                            overlaps = true;
                            break;
                        }
                    }
                    if (!overlaps)
                    {
                        bestOffset = cx;
                        placed2 = true;
                        break;
                    }
                }
                if (placed2) break;
            }

            placed.Add(new(bestOffset, y));
            result[i] = bestOffset;
        }

        return result;
    }
}
