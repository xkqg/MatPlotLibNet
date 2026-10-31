// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet;

/// <summary>The part a staircase is made of, handed to the callback of <see cref="XYAxesExtensions.Stairs"/> so it can be
/// styled: the outline when there is no baseline, the fill when there is one. Exactly one of the two is set, and it is the very
/// series on the axes, so a change made through it is the chart's.
/// <para>A value of this type can only be made by the library: it has no public constructor, and its properties have no
/// setters.</para></summary>
public readonly record struct StairsParts
{
    /// <summary>Makes the parts of a staircase drawn as an outline.</summary>
    /// <param name="outline">The line along the staircase.</param>
    internal StairsParts(LineSeries outline)
    {
        Outline = outline;
        Fill = null;
    }

    /// <summary>Makes the parts of a staircase drawn as a fill.</summary>
    /// <param name="fill">The area under the staircase, whose top edge is its one stroke.</param>
    internal StairsParts(AreaSeries fill)
    {
        Outline = null;
        Fill = fill;
    }

    /// <summary>The line along the staircase, level along each bin and upright at each step from one value to the next, with no
    /// drop at either end; <see langword="null"/> when the staircase has a baseline, which is drawn as <see cref="Fill"/>
    /// instead.</summary>
    public LineSeries? Outline { get; }

    /// <summary>The area between the staircase and its baseline. Its top edge is the one stroke of the staircase (its
    /// <see cref="AreaSeries.LineWidth"/>, <see cref="AreaSeries.LineStyle"/> and <see cref="AreaSeries.EdgeColor"/>), so no edge
    /// is drawn twice, and it is filled at its <see cref="AreaSeries.Alpha"/>; <see langword="null"/> when the staircase has no
    /// baseline, which is drawn as <see cref="Outline"/> instead.</summary>
    public AreaSeries? Fill { get; }
}
