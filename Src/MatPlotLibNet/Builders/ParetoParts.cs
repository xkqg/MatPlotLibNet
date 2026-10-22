// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The two series a Pareto chart is made of, handed to the callback of
/// <see cref="CategoricalAxesExtensions.Pareto"/> so each can be styled on its own: the bars of the counts and the line of
/// their running share. They are the very series on the axes, so a change made through them is the chart's, and each Y axis
/// takes its series' colour and name as the callback leaves them.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct ParetoParts
{
    /// <summary>Makes the parts of one Pareto chart.</summary>
    /// <param name="bars">The bars of the counts, largest first, on the left Y axis.</param>
    /// <param name="cumulative">The line of the running share, on the right-hand Y axis.</param>
    internal ParetoParts(BarSeries bars, LineSeries cumulative)
    {
        Bars = bars;
        Cumulative = cumulative;
    }

    /// <summary>The bars: one for each category, largest count first, measured on the left Y axis. The left axis wears the
    /// bars' colour and is titled with their label.</summary>
    public BarSeries Bars { get; }

    /// <summary>The line of the running share: one point on the centre of each bar, the share of the total the bars up to
    /// and including that one hold, in percent, measured on the right-hand Y axis. The right-hand axis wears the line's
    /// colour and is titled with its label.</summary>
    public LineSeries Cumulative { get; }
}
