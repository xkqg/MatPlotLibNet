// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.TickLocators;

/// <summary>
/// Produces nice-number ticks like <see cref="AutoLocator"/> but caps the result to at most
/// <c>maxN</c> positions. The target count is increased progressively until the output fits
/// the cap — guaranteeing at most <c>maxN</c> ticks while still choosing aligned values.
/// </summary>
public sealed class MaxNLocator : ITickLocator
{
    private readonly int _maxN;

    /// <summary>Initialises with the maximum number of ticks to return.</summary>
    /// <param name="maxN">The upper bound on the number of tick positions returned by <see cref="Locate"/>.</param>
    public MaxNLocator(int maxN) => _maxN = maxN;

    /// <inheritdoc />
    /// <remarks>
    /// Iteratively decreases the target count from <c>maxN</c> down to 1 until the nice-number
    /// algorithm produces no more than <c>maxN</c> positions. This guarantees the cap is respected
    /// while still aligning ticks to round values — it does not skip or thin an existing set.
    /// </remarks>
    public double[] Locate(double min, double max)
    {
        // Start from maxN and reduce the target until ticks fit.
        for (int target = _maxN; target >= 1; target--)
        {
            double[] ticks = new AutoLocator(target).Locate(min, max);
            if (ticks.Length <= _maxN)
                return ticks;
        }
        return [min];
    }
}
