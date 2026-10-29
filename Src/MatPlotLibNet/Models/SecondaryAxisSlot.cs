// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Models;

/// <summary>One stacked Y axis and the series plotted against it, as the lists 1.23.0 published hand them out. Use
/// <see cref="YAxisSlot"/>, the slot the container (<see cref="Axes.YAxes"/>) holds: each stack of the container keeps
/// its slots in stack order, index 0 nearest the plot and each further one stacked outward past the ones before it
/// (matplotlib's own repeated <c>ax.twinx()</c> plus <c>spines['right'].set_position(('outward', N))</c>), and the stack
/// rank is the slot's position in that stack — nothing stores it twice.</summary>
/// <remarks>A value view of a <see cref="YAxisSlot"/>: it carries the slot's own
/// <see cref="Axis"/> and series list, so a setting or a series added through it lands on that slot. Two views of one
/// slot are equal. It stays published so existing code compiles; <see cref="YAxisSlot"/> is the slot itself.</remarks>
[Obsolete("Use YAxisSlot, the slot type of Axes.YAxes: the same axis and series, as a class.")]
public readonly record struct SecondaryAxisSlot
{
    private readonly YAxisSlot _slot;

    /// <summary>The axis this slot draws (its own label, limits, ticks and scale).</summary>
    public Axis Axis => _slot.Axis;

    /// <summary>Which side of the plot this slot stacks outward from.</summary>
    public AxisSide Side => _slot.Side;

    /// <summary>Gets the series plotted against this slot's own axis — not shared with any other slot.</summary>
    public IReadOnlyList<ISeries> Series => _slot.Series;

    /// <summary>The view of <paramref name="slot"/>: the same axis and the same series list.</summary>
    /// <param name="slot">The container slot this value shows.</param>
    internal SecondaryAxisSlot(YAxisSlot slot) => _slot = slot;

    /// <summary>Adds an already-constructed series to this slot's own axis.</summary>
    /// <param name="series">The series to plot against this slot's axis.</param>
    /// <returns>The same series, so the caller can keep appending to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="series"/> is null.</exception>
    public TSeries AddSeries<TSeries>(TSeries series) where TSeries : ISeries => _slot.AddSeries(series);
}
