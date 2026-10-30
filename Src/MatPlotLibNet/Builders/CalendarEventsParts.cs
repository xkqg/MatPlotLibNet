// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The parts a calendar of events is made of, handed to the callback of <see cref="GridAxesExtensions.CalendarEvents"/>
/// so each can be styled on its own: the heatmap that is the calendar's days, and one bar series for each kind of event, in
/// the order the kinds first appear. They are the very series on the axes, so a change made through them is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct CalendarEventsParts
{
    /// <summary>Makes the parts of one calendar of events.</summary>
    /// <param name="days">The heatmap of the days of the span.</param>
    /// <param name="kinds">The bar series of the kinds of event, in the order they first appear.</param>
    internal CalendarEventsParts(HeatmapSeries days, IReadOnlyList<BarSeries> kinds)
    {
        Days = days;
        Kinds = kinds;
    }

    /// <summary>The days of the span, one cell each, in the neutral grey; the days the first and the last week do not hold are
    /// blank.</summary>
    public HeatmapSeries Days { get; }

    /// <summary>One bar series for each kind of event, in the order the kinds first appear. Each event is a bar standing in the
    /// cell of its day (a floating bar, so its table gives the day and where the bar starts and ends).</summary>
    public IReadOnlyList<BarSeries> Kinds { get; }
}
