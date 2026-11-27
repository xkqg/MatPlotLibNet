// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>The sizes of the slices of a whole (a pie's, a donut's): the one guard that says whether there is a whole to draw.</summary>
internal static class PartsOfAWholeExtensions
{
    extension(double[] sizes)
    {
        /// <summary>The total of the slices that can be drawn (a slice that is missing or infinite counts as 0 and draws nothing of its
        /// own), or <see langword="null"/> when that total is nothing or less: matplotlib refuses a pie of negative sizes and of all
        /// zeros, and a pie or a donut with no whole to share draws no slice.</summary>
        /// <returns>The total, above zero; <see langword="null"/> when there is no whole to draw.</returns>
        internal double? DrawableTotal()
        {
            double total = sizes.DrawableValues().Sum();
            return total > 0 ? total : null;
        }
    }
}
