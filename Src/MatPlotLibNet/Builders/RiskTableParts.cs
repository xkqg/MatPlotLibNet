// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Numerics;

namespace MatPlotLibNet;

/// <summary>The parts a risk table is made of, handed to the callback of <see cref="DistributionAxesExtensions.RiskTable"/> so
/// each can be styled on its own: the counts, the line that states the log-rank test, and the test itself. The annotations are
/// the very ones on the axes, so a change made through them is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct RiskTableParts
{
    /// <summary>Makes the parts of one risk table.</summary>
    /// <param name="counts">The counts, group by group.</param>
    /// <param name="test">The line stating the test; <see langword="null"/> for a single group.</param>
    /// <param name="logRank">The result of the test.</param>
    internal RiskTableParts(IReadOnlyList<Annotation> counts, Annotation? test, LogRankResult logRank)
    {
        Counts = counts;
        Test = test;
        LogRank = logRank;
    }

    /// <summary>The counts, one annotation for each group at each time: group by group in the order of the groups, and within a
    /// group in the order of the times. Each stands at its time along the X axis (in data units) on the row of its group, with
    /// the number of subjects at risk as its text.</summary>
    public IReadOnlyList<Annotation> Counts { get; }

    /// <summary>The line that states the log-rank test, pinned to the right end of the slot below the last row (as a share of the
    /// axes, not of the data); <see langword="null"/> when there is a single group, which has nothing to be compared with.</summary>
    public Annotation? Test { get; }

    /// <summary>The log-rank test the line states: its statistic, its degrees of freedom and its p-value. The statistic and
    /// the p-value are NaN when there is nothing to compare (no event in any group, or fewer than two groups).</summary>
    public LogRankResult LogRank { get; }
}
