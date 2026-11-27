// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering.TickFormatters;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The labels of a set of ticks: one number format for all of them, as matplotlib's <c>ScalarFormatter</c> writes
/// an axis.</summary>
internal static class TickLabelExtensions
{
    /// <summary>The fixed-point formats with 0 to 10 decimals, written once rather than built for every axis on every
    /// draw.</summary>
    internal static readonly string[] FixedFormats = ["F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10"];

    /// <summary>The text written once at the end of an axis whose labels were divided by <c>10^orderOfMagnitude</c> and had
    /// <paramref name="offset"/> taken off: the power as <c>1e6</c>, then the offset with its sign (<c>ScalarFormatter.get_offset</c>).</summary>
    /// <param name="orderOfMagnitude">The power of ten the labels were divided by, 0 for none.</param>
    /// <param name="offset">The offset taken off, 0 for none.</param>
    /// <returns>The text; empty when there is neither.</returns>
    internal static string ScaleText(int orderOfMagnitude, double offset) =>
        (orderOfMagnitude != 0 ? "1e" + orderOfMagnitude.InvariantText() : "") + (offset != 0 ? offset.OffsetText() : "");


    extension(double offset)
    {
        /// <summary>The offset written at the end of its axis, as matplotlib's <c>ScalarFormatter.format_data</c> writes it with a
        /// '+' before a positive one: its significand and its power of ten, "+1e2", "+1.2e3", "-1e3", or the significand alone
        /// for a power of 0, "+5".</summary>
        /// <returns>The text.</returns>
        internal string OffsetText()
        {
            int e = (int)Math.Abs(offset).DecadeExponentAtOrBelow();
            double s = Math.Round(offset / Math.Pow(10, e), 10);
            string significand = s % 1 == 0
                ? s.InvariantText("F0")
                : s.InvariantText("G10");
            string text = e == 0 ? significand : $"{significand}e{e.InvariantText()}";
            return offset > 0 ? "+" + text : text;
        }
    }

    extension(double[] ticks)
    {

        /// <summary>The format every tick of an axis is written in: the decimals the step between the first two ticks needs,
        /// so a step of 0.5 writes "0.0", "0.5", "1.0" rather than "0", "0.5", "1"; a value a rounding error from 0 is written
        /// as 0, never "-0.0". Ticks with no step to measure (fewer than two) are written in the plain number format
        /// (<see cref="NumericTickFormatter.FormatValue"/>).</summary>
        /// <returns>The format, made once before the ticks are drawn and called for each.</returns>
        internal Func<double, string> UniformFormat()
        {
            if (ticks is null || ticks.Length < 2) return NumericTickFormatter.FormatValue;
            string format = FixedFormats[ticks.StepDecimals()];
            return v =>
            {
                if (Math.Abs(v) < 1e-10) v = 0;  // avoid "-0.0"
                return v.InvariantText(format);
            };
        }

        /// <summary>The offset matplotlib's <c>ScalarFormatter</c> takes off every label when the ticks in
        /// <paramref name="view"/> share enough leading digits (<c>_compute_offset</c>): 0 for fewer than two of them, for
        /// ticks of both signs or on zero, and when the offset would save fewer than <paramref name="threshold"/> digits
        /// (<c>axes.formatter.offset_threshold</c>); otherwise the round number the ticks share, signed as they are.</summary>
        /// <param name="view">The ends of the axis' view; a tick outside them does not count.</param>
        /// <param name="threshold">How many digits the offset must save.</param>
        /// <returns>The offset, 0 for none.</returns>
        internal double OffsetIn(Range1D view, int threshold)
        {
            var (lo, hi) = view.Interval;
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            foreach (double tick in ticks)
            {
                if (tick < lo || tick > hi) continue;
                min = Math.Min(min, tick);
                max = Math.Max(max, tick);
            }
            if (!(min < max) || (min <= 0 && 0 <= max)) return 0;

            double absMin = Math.Min(Math.Abs(min), Math.Abs(max)), absMax = Math.Max(Math.Abs(min), Math.Abs(max));
            double sign = Math.CopySign(1, min);
            // The smallest power of ten at which the two ends agree, and, where they straddle a multiple of a large power of
            // ten, the smallest at which they are no more than one apart.
            int oomMax = (int)absMax.DecadeExponentAtOrAbove();
            int oom = oomMax;
            while (Math.Floor(absMin / Math.Pow(10, oom)) == Math.Floor(absMax / Math.Pow(10, oom))) oom--;
            oom++;
            if ((absMax - absMin) / Math.Pow(10, oom) <= 1e-2)
            {
                oom = oomMax;
                while (!(Math.Floor(absMax / Math.Pow(10, oom)) - Math.Floor(absMin / Math.Pow(10, oom)) > 1)) oom--;
                oom++;
            }
            double shared = Math.Floor(absMax / Math.Pow(10, oom));
            return shared >= Math.Pow(10, threshold - 1) ? sign * shared * Math.Pow(10, oom) : 0;
        }

        /// <summary>The power of ten the labels of the ticks in <paramref name="view"/> are written divided by, as matplotlib's
        /// <c>ScalarFormatter._set_order_of_magnitude</c> chooses it: 0 for none; otherwise the order of magnitude of the largest tick in
        /// view (of the width of the view when an offset is taken off, <paramref name="offset"/> not 0) when it is at or below
        /// <paramref name="limits"/>' lower power or at or above its upper one.</summary>
        /// <param name="view">The ends of the axis' view; a tick outside them does not count.</param>
        /// <param name="offset">The offset taken off every label (<see cref="OffsetIn"/>), 0 for none.</param>
        /// <param name="limits">The powers of ten beyond which the labels are scaled.</param>
        /// <returns>The power, 0 for none.</returns>
        internal int OrderOfMagnitudeIn(Range1D view, double offset, PowerLimits limits)
        {
            var (lo, hi) = view.Interval;
            double largest = double.NegativeInfinity;
            foreach (double tick in ticks)
                if (tick >= lo && tick <= hi) largest = Math.Max(largest, Math.Abs(tick));
            if (double.IsNegativeInfinity(largest)) return 0;

            int order = offset != 0 ? (int)(hi - lo).DecadeExponentAtOrBelow() : largest == 0 ? 0 : (int)largest.DecadeExponentAtOrBelow();
            return order <= limits.Lower || order >= limits.Upper ? order : 0;
        }

        /// <summary>The fewest decimals that write the step between the first two ticks exactly: the smallest power of ten up
        /// to 10^10 that makes it whole within a billionth. 0 for a whole step (1, 2, 10), 1 for 0.5, 0.1 or 2.5, 2 for 0.25;
        /// 0 for a step of 0, a step that is no number, or one no such power makes whole.</summary>
        private int StepDecimals()
        {
            double step = Math.Abs(ticks[1] - ticks[0]);
            if (!step.IsPositiveFinite()) return 0;
            for (int d = 0; d < FixedFormats.Length; d++)
            {
                double scaled = step * Math.Pow(10, d);
                if (Math.Abs(scaled - Math.Round(scaled)) < 1e-9) return d;
            }
            return 0;
        }
    }
}
