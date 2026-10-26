// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Models;

/// <summary>One outward-stacked secondary Y axis and the series plotted against it. <see cref="Axes.RightYAxes"/> and
/// <see cref="Axes.LeftYAxes"/> each hold these in stack order: index 0 sits nearest the plot, each further one stacks
/// outward past the ones before it (matplotlib's own repeated <c>ax.twinx()</c> plus
/// <c>spines['right'].set_position(('outward', N))</c>). The stack rank is the slot's position in that list — nothing
/// stores it twice.</summary>
public readonly record struct SecondaryAxisSlot
{
    /// <summary>The axis this slot draws (its own label, limits, ticks and scale).</summary>
    public Axis Axis { get; }

    /// <summary>Which side of the plot this slot stacks outward from.</summary>
    public AxisSide Side { get; }

    /// <summary>Gets the series plotted against this slot's own axis — not shared with any other slot.</summary>
    public IReadOnlyList<ISeries> Series => SeriesList;

    internal List<ISeries> SeriesList { get; }

    internal SecondaryAxisSlot(Axis axis, AxisSide side)
    {
        Axis = axis;
        Side = side;
        SeriesList = [];
    }

    /// <summary>Adds an already-constructed series to this slot's own axis.</summary>
    /// <param name="series">The series to plot against this slot's axis.</param>
    /// <returns>The same series, so the caller can keep appending to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="series"/> is null.</exception>
    public TSeries AddSeries<TSeries>(TSeries series) where TSeries : ISeries
    {
        ArgumentNullException.ThrowIfNull(series);
        SeriesList.Add(series);
        return series;
    }
}
