// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The parts a connected scatter plot is made of, handed to the callback of
/// <see cref="XYAxesExtensions.ConnectedScatter"/> so each can be styled on its own: the markers on the points and the arrows that
/// run from each point to the next. They are the very series on the axes, so a change made through them is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct ConnectedScatterParts
{
    /// <summary>Makes the parts of one connected scatter plot.</summary>
    /// <param name="points">The markers on the points.</param>
    /// <param name="arrows">The arrows from each point to the next.</param>
    internal ConnectedScatterParts(ScatterSeries points, QuiverSeries arrows)
    {
        Points = points;
        Arrows = arrows;
    }

    /// <summary>The markers, one on each point, drawn over the arrows.</summary>
    public ScatterSeries Points { get; }

    /// <summary>The arrows: arrow <c>i</c> starts on point <c>i</c> and its head is on point <c>i + 1</c>, so there is one fewer
    /// arrow than there are points. Its table gives each arrow's tail and the step it takes.</summary>
    public QuiverSeries Arrows { get; }
}
