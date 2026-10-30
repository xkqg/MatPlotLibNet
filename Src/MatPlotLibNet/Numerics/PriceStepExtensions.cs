// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>The one owner of price arithmetic in whole steps, for the charts that cut a price series into bricks, boxes and
/// reversals of a fixed size (Renko, Point and Figure, Kagi). Such a chart counts whole steps from a fixed origin, never adds
/// a step to a running level, and compares with a relative tolerance of 1e-9 of a step: a decimal step is not exact in binary
/// (<c>0.3 / 0.1</c> is <c>2.9999999999999996</c>, <c>0.3 - 0.2</c> is <c>0.09999999999999998</c>), and a plain floor or
/// comparison would then draw one step too few on ordinary prices.</summary>
internal static class PriceStepExtensions
{
    /// <summary>The share of a step within which a price counts as on the step boundary.</summary>
    internal const double Tolerance = 1e-9;

    extension(double price)
    {
        /// <summary>The number of whole steps of <paramref name="step"/> from <paramref name="origin"/> up to this price,
        /// counted toward minus infinity: the index of the step boundary at or below the price. A price within the tolerance
        /// of the boundary above it counts as on that boundary.</summary>
        /// <param name="origin">The price at step index 0.</param>
        /// <param name="step">The size of one step; positive.</param>
        /// <returns>The index of the boundary at or below the price.</returns>
        internal int FloorStep(double origin, double step) => (int)Math.Floor((price - origin) / step + Tolerance);

        /// <summary>The index of the step boundary at or above this price, counted from <paramref name="origin"/>. A price
        /// within the tolerance of the boundary below it counts as on that boundary.</summary>
        /// <param name="origin">The price at step index 0.</param>
        /// <param name="step">The size of one step; positive.</param>
        /// <returns>The index of the boundary at or above the price.</returns>
        internal int CeilingStep(double origin, double step) => (int)Math.Ceiling((price - origin) / step - Tolerance);
    }

    extension(double distance)
    {
        /// <summary>Whether this distance is at least <paramref name="amount"/>, a distance within the tolerance of the
        /// amount counting as the amount (a reversal of one step is a move of one step, however the subtraction rounds).</summary>
        /// <param name="amount">The size the distance has to reach; positive.</param>
        /// <returns><see langword="true"/> when the distance reaches the amount.</returns>
        internal bool AtLeast(double amount) => distance >= amount * (1 - Tolerance);
    }

    extension(int index)
    {
        /// <summary>The price of the step boundary with this index: the origin plus the index times the step, counted from the
        /// origin each time and never accumulated.</summary>
        /// <param name="origin">The price at step index 0.</param>
        /// <param name="step">The size of one step.</param>
        /// <returns>The price of the boundary.</returns>
        internal double LevelFrom(double origin, double step) => origin + index * step;
    }
}
