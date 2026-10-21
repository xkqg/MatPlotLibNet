// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace MatPlotLibNet.Rendering.Layout;

/// <summary>Layout operations over a collection of <see cref="Interval1D"/> spans.</summary>
internal static class Interval1DExtensions
{
    /// <summary>
    /// Pushes overlapping spans apart along one axis without ever reordering them. The spans are visited in order
    /// of their centre (equal centres keep their list order); a forward pass moves each one up just far enough to
    /// clear its predecessor by <paramref name="padding"/> and to start at or after <paramref name="lower"/>, then a
    /// backward pass pulls the stack back so its last span ends at <paramref name="upper"/> when the forward pass
    /// overshot it. Every span keeps its position in <paramref name="items"/>; only its <see cref="Interval1D.Start"/>
    /// changes. When the bounds cannot hold all spans the padding between them is kept and the stack spills below
    /// <paramref name="lower"/>.
    /// </summary>
    /// <param name="items">The spans to spread, updated in place.</param>
    /// <param name="padding">Minimum gap between neighbouring spans.</param>
    /// <param name="lower">Lowest allowed start of the first span.</param>
    /// <param name="upper">Highest allowed end of the last span.</param>
    public static void SpreadApart(this IList<Interval1D> items, double padding, double lower, double upper)
    {
        int count = items.Count;
        if (count == 0) return;

        var order = new int[count];
        for (int i = 0; i < count; i++) order[i] = i;
        Array.Sort(order, (a, b) =>
        {
            int byCentre = items[a].Centre.CompareTo(items[b].Centre);
            return byCentre != 0 ? byCentre : a.CompareTo(b);
        });

        double cursor = lower;
        foreach (int idx in order)
        {
            var item = items[idx];
            double start = Math.Max(item.Start, cursor);
            items[idx] = item with { Start = start };
            cursor = start + item.Size + padding;
        }

        double overshoot = cursor - padding - upper;
        if (overshoot <= 0) return;

        for (int k = count - 1; k >= 0; k--)
        {
            int idx = order[k];
            double start = items[idx].Start - overshoot;
            items[idx] = items[idx] with { Start = start };
            if (k > 0)
            {
                double maxAllowedPrevEnd = start - padding;
                double prevEnd = items[order[k - 1]].End;
                overshoot = prevEnd > maxAllowedPrevEnd ? prevEnd - maxAllowedPrevEnd : 0;
            }
        }
    }
}
