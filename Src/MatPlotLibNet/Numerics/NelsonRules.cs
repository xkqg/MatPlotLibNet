// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Numerics;

/// <summary>Nelson's eight tests for a process out of control (<see cref="ControlRules"/>), as a table of rules: each rule is
/// one flag and one predicate that says whether a pattern is complete at a point, judged from the points up to it. The table is
/// the whole vocabulary: a rule is added by a row, and the chart asks for the rows it wants.</summary>
internal static class NelsonRules
{
    /// <summary>The values of a chart with the limits they are judged by, and the questions the rules ask of them.</summary>
    /// <param name="Values">The plotted values, in order.</param>
    /// <param name="Limits">The control limits the chart is drawn with.</param>
    private readonly record struct Judged(double[] Values, ControlLimits Limits)
    {
        /// <summary>One sigma: a third of the way from the centre line to the upper control limit.</summary>
        internal double Sigma => (Limits.Upper - Limits.Center) / ControlChartConstants.Sigmas;

        /// <summary>Whether the value at <paramref name="i"/> can be placed (not missing, not infinite).</summary>
        internal bool Placed(int i) => double.IsFinite(Values[i]);

        /// <summary>The distance of the value at <paramref name="i"/> from the centre line, signed (above is positive).</summary>
        internal double Offset(int i) => Values[i] - Limits.Center;

        /// <summary>Which side of the centre line the value at <paramref name="i"/> is on: 1 above, -1 below, 0 on it or not placed.</summary>
        internal int Side(int i) => Placed(i) ? Math.Sign(Offset(i)) : 0;

        /// <summary>How many of the last <paramref name="window"/> points up to <paramref name="i"/> (fewer at the start of the
        /// data) are placed and satisfy <paramref name="test"/>.</summary>
        internal int Count(int i, int window, Func<int, bool> test)
        {
            int count = 0;
            for (int j = Math.Max(0, i - window + 1); j <= i; j++)
            {
                if (Placed(j) && test(j)) count++;
            }

            return count;
        }

        /// <summary>How many of the last <paramref name="window"/> points are on <paramref name="side"/> of the centre line,
        /// farther from it than <paramref name="distance"/>.</summary>
        internal int CountBeyond(int i, int window, int side, double distance)
        {
            var judged = this;
            return Count(i, window, j => side * judged.Offset(j) > distance);
        }

        /// <summary>Whether the last <paramref name="points"/> points up to <paramref name="i"/> are all placed, with the
        /// steps between them all going the same way (<paramref name="alternating"/> false) or each going the other way from
        /// the one before (true), none standing still.</summary>
        internal bool Steps(int i, int points, bool alternating)
        {
            if (i < points - 1) return false;
            int first = i - points + 1;
            for (int j = first; j <= i; j++)
            {
                if (!Placed(j)) return false;
            }

            // The first step sets the direction; each later step goes the same way, or (alternating) the other way from the one
            // before it, and none stands still.
            int direction = Math.Sign(Values[first + 1] - Values[first]);
            for (int j = first + 1; j <= i; j++)
            {
                int step = Math.Sign(Values[j] - Values[j - 1]);
                int expected = alternating && (j - first) % 2 == 0 ? -direction : direction;
                if (step == 0 || step != expected) return false;
            }

            return true;
        }
    }

    /// <summary>One test: the flag that asks for it, and whether its pattern is complete at a point.</summary>
    /// <param name="Flag">The rule.</param>
    /// <param name="CompletedAt">Whether the pattern is complete at the point, judged from the points up to it.</param>
    private readonly record struct Rule(ControlRules Flag, Func<Judged, int, bool> CompletedAt);

    private static readonly Rule[] Table =
    [
        // 1: one point beyond a control limit.
        new(ControlRules.Nelson1, (v, i) => v.Placed(i) && (v.Values[i] > v.Limits.Upper || v.Values[i] < v.Limits.Lower)),

        // 2: nine in a row on one side of the centre line.
        new(ControlRules.Nelson2, (v, i) => v.Side(i) != 0 && v.CountBeyond(i, 9, v.Side(i), 0) >= 9),

        // 3: six in a row steadily rising or falling.
        new(ControlRules.Nelson3, (v, i) => v.Steps(i, 6, alternating: false)),

        // 4: fourteen in a row alternating.
        new(ControlRules.Nelson4, (v, i) => v.Steps(i, 14, alternating: true)),

        // 5: two of three beyond two sigma, on one side.
        new(ControlRules.Nelson5, (v, i) => v.Side(i) != 0 && v.Side(i) * v.Offset(i) > 2 * v.Sigma && v.CountBeyond(i, 3, v.Side(i), 2 * v.Sigma) >= 2),

        // 6: four of five beyond one sigma, on one side.
        new(ControlRules.Nelson6, (v, i) => v.Side(i) != 0 && v.Side(i) * v.Offset(i) > v.Sigma && v.CountBeyond(i, 5, v.Side(i), v.Sigma) >= 4),

        // 7: fifteen in a row within one sigma of the centre line.
        new(ControlRules.Nelson7, (v, i) => v.Count(i, 15, j => Math.Abs(v.Offset(j)) < v.Sigma) >= 15),

        // 8: eight in a row more than one sigma from the centre line.
        new(ControlRules.Nelson8, (v, i) => v.Count(i, 8, j => Math.Abs(v.Offset(j)) > v.Sigma) >= 8),
    ];

    /// <summary>The points the <paramref name="rules"/> flag: every point at which a pattern one of them asks for is complete.</summary>
    /// <param name="rules">The rules to judge by.</param>
    /// <param name="values">The plotted values, in order; a missing value (NaN) or an infinity is never flagged and breaks every
    /// pattern through it.</param>
    /// <param name="limits">The control limits the chart is drawn with, which also set the zones.</param>
    /// <returns>The indices of the flagged points, ascending, each once.</returns>
    internal static int[] Flagged(ControlRules rules, double[] values, ControlLimits limits)
    {
        var judged = new Judged(values, limits);
        Rule[] asked = [.. Table.Where(rule => (rules & rule.Flag) != 0)];
        return [.. Enumerable.Range(0, values.Length).Where(i => asked.Any(rule => rule.CompletedAt(judged, i)))];
    }
}
