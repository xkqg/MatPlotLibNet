// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>What <see cref="WholeStepExtensions"/>' <c>DivMod</c> gives: a whole quotient and the remainder it leaves.</summary>
/// <param name="Quotient">The quotient, a whole number.</param>
/// <param name="Remainder">The remainder, with the divisor's sign.</param>
internal readonly record struct WholeStepDivision(double Quotient, double Remainder);

/// <summary>A value counted in whole steps: numpy's <c>divmod</c>, and the whole multiple of a step at or below and at or above a value,
/// a remainder within a tolerance of a whole step counted as one (matplotlib's <c>_Edge_integer.le</c> and <c>ge</c>). A decimal step is
/// not exact in binary (<c>0.3</c> is 2.9999999999999996 steps of 0.1), so a plain floor or ceiling counts one step too few or too many.
/// The one place the ladders of ticks (<c>SteppedTicks</c>, with matplotlib's tolerance) and the bricks, boxes and reversals of a price
/// chart (<see cref="PriceStepExtensions"/>, with its own) count whole steps; the tolerance is the caller's value.</summary>
internal static class WholeStepExtensions
{
    extension(double value)
    {
        /// <summary>numpy's (and Python's) <c>divmod</c> of two doubles: the remainder takes the sign of the divisor, the quotient is
        /// the whole number the remainder leaves, rounded as numpy's <c>npy_divmod</c> rounds it.</summary>
        /// <param name="step">The divisor.</param>
        /// <returns>The quotient and the remainder.</returns>
        internal WholeStepDivision DivMod(double step)
        {
            double mod = value % step;
            double div = (value - mod) / step;
            if (mod != 0)
            {
                if (step < 0 != mod < 0)
                {
                    mod += step;
                    div -= 1.0;
                }
            }
            else
            {
                mod = Math.CopySign(0, step);
            }

            double floor;
            if (div != 0)
            {
                floor = Math.Floor(div);
                if (div - floor > 0.5) floor += 1.0;
            }
            else
            {
                floor = Math.CopySign(0, value / step);
            }

            return new WholeStepDivision(floor, mod);
        }

        /// <summary>The largest whole <c>n</c> with <c>n * step</c> at or below this value, a remainder within
        /// <paramref name="tolerance"/> of a whole step counted as one (<c>_Edge_integer.le</c>).</summary>
        /// <param name="step">The step, positive.</param>
        /// <param name="tolerance">How close to a whole step, in steps, a remainder counts as one.</param>
        /// <returns>The whole number of steps.</returns>
        internal double StepsAtOrBelow(double step, double tolerance)
        {
            var (d, m) = value.DivMod(step);
            return Math.Abs(m / step - 1) < tolerance ? d + 1 : d;
        }

        /// <summary>The smallest whole <c>n</c> with <c>n * step</c> at or above this value, a remainder within
        /// <paramref name="tolerance"/> of no step counted as none (<c>_Edge_integer.ge</c>).</summary>
        /// <param name="step">The step, positive.</param>
        /// <param name="tolerance">How close to a whole step, in steps, a remainder counts as none.</param>
        /// <returns>The whole number of steps.</returns>
        internal double StepsAtOrAbove(double step, double tolerance)
        {
            var (d, m) = value.DivMod(step);
            return Math.Abs(m / step) < tolerance ? d : d + 1;
        }
    }
}
