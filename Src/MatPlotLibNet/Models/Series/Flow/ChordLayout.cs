// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Numerics;
using MatPlotLibNet.Rendering;

namespace MatPlotLibNet.Models.Series;

/// <summary>A stretch of the circle, in radians counted clockwise from twelve o'clock.</summary>
/// <param name="Start">Where it begins.</param>
/// <param name="End">Where it ends, never before <paramref name="Start"/>.</param>
internal readonly record struct ChordSpan(double Start, double End);

/// <summary>One group of a chord diagram: an arc of the circle as long as the flows of the group.</summary>
/// <param name="Group">The group's position in the matrix.</param>
/// <param name="Start">Where the arc begins, in radians clockwise from twelve o'clock.</param>
/// <param name="End">Where the arc ends.</param>
internal readonly record struct ChordArc(int Group, double Start, double End)
{
    /// <summary>The middle of the arc.</summary>
    public double Mid => (Start + End) / 2;
}

/// <summary>One ribbon: the flows between two groups, or the flow of a group to itself. It stands on the arc of each group with
/// an end as wide as the flow that group sends to the other, so a flow that goes only one way has an end of no width at the
/// other.</summary>
/// <param name="Source">The group that sends more to the other (the lower of the two when they send as much), whose colour the ribbon wears.</param>
/// <param name="Target">The other group; the same as <paramref name="Source"/> for a loop.</param>
/// <param name="SourceSpan">The ribbon's end on the arc of the source.</param>
/// <param name="TargetSpan">The ribbon's end on the arc of the target; the same span as the source's for a loop.</param>
/// <param name="Value">The flow from <paramref name="Source"/> to <paramref name="Target"/>.</param>
internal readonly record struct ChordRibbon(int Source, int Target, ChordSpan SourceSpan, ChordSpan TargetSpan, double Value);

/// <summary>The arcs and the ribbons of a chord diagram.</summary>
/// <param name="Arcs">One arc for each group, in the order of the matrix.</param>
/// <param name="Ribbons">One ribbon for each pair of groups with a flow between them, and one for each group with a flow to
/// itself, the largest first.</param>
internal readonly record struct ChordLayoutResult(IReadOnlyList<ChordArc> Arcs, IReadOnlyList<ChordRibbon> Ribbons);

/// <summary>The one owner of the geometry of a chord diagram, in radians, with no drawing and no random choice. The rule is that
/// of d3-chord: a group's arc is as long as its row of the matrix (a flow of a group to itself counted once), the circle less
/// the gaps between the arcs is shared among the groups in proportion, and the angle is counted clockwise from twelve o'clock.
/// Two things are added to it. The ribbons inside an arc run from the group farthest clockwise to the nearest and the group's
/// own loop last, so that the ribbons of a complete graph cross as few times as any layout can (C(n, 4)), where the order of
/// the matrix crosses them far more. And a matrix that cannot be drawn is refused by name, since d3 would draw a blank circle.</summary>
internal static class ChordLayout
{
    /// <summary>The gap between neighbouring arcs, in radians: a score of the circle.</summary>
    public const double DefaultPadding = 0.05;

    /// <summary>Lays out the diagram of a matrix of flows.</summary>
    /// <param name="flows">The flow from the group of each row to the group of each column; square, finite, not negative, and not
    /// all zero.</param>
    /// <param name="padding">The gap between neighbouring arcs, in radians.</param>
    /// <returns>The arcs and the ribbons.</returns>
    /// <exception cref="ArgumentException"><paramref name="flows"/> is not square, or is all zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="flows"/> holds a flow that is negative, infinite or not a
    /// number (the message names its place), or <paramref name="padding"/> is negative, not a number, or so large that the gaps
    /// alone fill the circle.</exception>
    public static ChordLayoutResult Compute(double[,] flows, double padding = DefaultPadding)
    {
        double total = Refuse(flows);
        int n = flows.GetLength(0);
        if (n == 0) return new ChordLayoutResult([], []);

        ThrowIfNotAGap(padding, n, nameof(padding));

        double scale = (2 * Math.PI - padding * n) / total;
        var spans = new ChordSpan[n, n];
        var arcs = new ChordArc[n];
        double at = 0;
        for (int group = 0; group < n; group++)
        {
            double begin = at;
            // The farthest group clockwise first, the nearest last, the group's own loop after them.
            for (int steps = n - 1; steps >= 0; steps--)
            {
                int other = (group + steps) % n;
                double width = flows[group, other] * scale;
                spans[group, other] = new ChordSpan(at, at + width);
                at += width;
            }

            arcs[group] = new ChordArc(group, begin, at);
            at += padding;
        }

        return new ChordLayoutResult(arcs, Ribbons(flows, spans));
    }

    private static ChordRibbon[] Ribbons(double[,] flows, ChordSpan[,] spans)
    {
        int n = flows.GetLength(0);
        var ribbons = new List<ChordRibbon>();
        var sizes = new List<double>();
        for (int low = 0; low < n; low++)
        {
            for (int high = low; high < n; high++)
            {
                double there = flows[low, high], back = flows[high, low];
                if (!(there > 0) && !(back > 0)) continue;

                if (low == high)
                {
                    ribbons.Add(new ChordRibbon(low, low, spans[low, low], spans[low, low], there));
                    sizes.Add(there);
                    continue;
                }

                // The group that sends more owns the ribbon; of two that send the same, the lower.
                bool lowSends = there >= back;
                int source = lowSends ? low : high, target = lowSends ? high : low;
                ribbons.Add(new ChordRibbon(source, target, spans[source, target], spans[target, source], flows[source, target]));
                sizes.Add(there + back);
            }
        }

        // The largest first, so the small ribbons are drawn over it and stay visible; a tie by the pair's place in the matrix, the order
        // the ribbons were made in (the order is stable).
        return sizes.ToArray().DescendingOrder().Pick(ribbons);
    }

    /// <summary>The gap between neighbouring arcs, refused unless it is 0 or more and the gaps of all <paramref name="arcs"/> arcs
    /// leave room for the arcs: the one place that says how wide a gap can be.</summary>
    /// <param name="padding">The gap, in radians.</param>
    /// <param name="arcs">How many arcs, and so gaps, the circle holds.</param>
    /// <param name="name">The parameter that holds the gap, for the error.</param>
    internal static double ThrowIfNotAGap(double padding, int arcs, string name) =>
        padding.ThrowIfNot(padding >= 0 && padding * arcs < 2 * Math.PI, name, "must be 0 or more, and together with the other gaps less than the circle");

    /// <summary>Checks that the matrix can be drawn and returns the sum of its flows.</summary>
    private static double Refuse(double[,] flows)
    {
        static string Cell((int Row, int Column) at) => string.Invariant($"flows[{at.Row},{at.Column}]");
        int n = flows.GetLength(0), columns = flows.GetLength(1);
        if (n != columns)
            throw new ArgumentException(string.Invariant($"flows must be square, with one row and one column for each group; it has {n} rows and {columns} columns."), nameof(flows));

        double total = 0;
        for (int row = 0; row < n; row++)
        {
            for (int column = 0; column < n; column++)
            {
                total += flows[row, column].ThrowIfNotFinite(nameof(flows), (row, column), Cell).ThrowIfBelow(0, nameof(flows), (row, column), Cell);
            }
        }

        // A matrix of no groups is a diagram of nothing, as a series with no data is every other chart; a matrix of groups that
        // send nothing is a blank circle, and is refused.
        if (n > 0 && total == 0)
            throw new ArgumentException("flows are all zero, so there is nothing to draw.", nameof(flows));
        return total;
    }
}
