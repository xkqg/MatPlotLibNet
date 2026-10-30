// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The one rule that turns shares into whole cells: the largest remainder method (Hamilton's). Each value gets the
/// whole number of cells its share of the total comes to, and the cells left over go one each to the values whose shares
/// left the largest remainders, so the cells add up to exactly the number there are, and every value is at most one cell
/// from its exact share.</summary>
internal static class LargestRemainderExtensions
{
    extension(double[] values)
    {
        /// <summary>Divides <paramref name="seats"/> whole cells among the values in proportion to them. A value that is not
        /// a finite number above zero counts as nothing and takes no cell, and nothing is handed out at all when no value
        /// counts. Values with equal remainders are served in the order they stand, so the first of two equal shares gets
        /// the extra cell and the same input divides the same way on every machine.</summary>
        /// <param name="seats">The number of cells to hand out; 0 or more.</param>
        /// <returns>One whole number of cells for each value, adding up to <paramref name="seats"/> (all zero when no value counts).</returns>
        internal int[] Apportion(int seats)
        {
            var cells = new int[values.Length];
            double[] weights = [.. values.Select(value => double.IsFinite(value) && value > 0 ? value : 0)];
            double total = weights.Sum();
            if (seats <= 0 || !(total > 0) || !double.IsFinite(total)) return cells;

            var remainders = new double[values.Length];
            int handedOut = 0;
            for (int i = 0; i < values.Length; i++)
            {
                double quota = weights[i] / total * seats;
                cells[i] = (int)Math.Floor(quota);
                remainders[i] = quota - cells[i];
                handedOut += cells[i];
            }

            // The cells left over go one each to the largest remainders; equal remainders are served in the order they stand.
            foreach (int i in Enumerable.Range(0, values.Length).OrderByDescending(i => remainders[i]).ThenBy(i => i).Take(seats - handedOut))
            {
                cells[i]++;
            }

            return cells;
        }
    }
}
