// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Models;

/// <summary>One Y axis of an <see cref="Axes"/> and the series plotted against it: a slot of the side's
/// <see cref="YAxisStack"/>. Left slot 0 is the primary Y axis (<see cref="Axes.YAxis"/>, with
/// <see cref="Axes.Series"/> as its series); every further slot on either side stacks outward past the ones before
/// it, as matplotlib does with a repeated <c>ax.twinx()</c> plus <c>spines['right'].set_position(('outward', N))</c>.
/// The slot's rank is its position in its stack; nothing stores it twice.</summary>
/// <remarks>A class, not a value: the stack, the builders and the renderer all hold the SAME slot, so a setting made
/// through one reaches the others. What a slot draws (label, limits, ticks, scale, colours) lives on its
/// <see cref="Axis"/>.</remarks>
public sealed class YAxisSlot
{
    internal YAxisSlot(AxisSide side) => Side = side;

    /// <summary>The axis this slot draws: its own label, limits, ticks, scale and colours.</summary>
    public Axis Axis { get; } = new();

    /// <summary>Which side of the plot this slot stands on.</summary>
    public AxisSide Side { get; }

    /// <summary>The series plotted against this slot's own axis, in the order they were added; no other slot shares
    /// them.</summary>
    public IReadOnlyList<ISeries> Series => SeriesList;

    /// <summary>The list behind <see cref="Series"/>; for left slot 0 it is the list behind <see cref="Axes.Series"/>.</summary>
    internal List<ISeries> SeriesList { get; } = [];

    /// <summary>Whether this slot is linked to the primary Y axis (<see cref="Models.Axis.Link"/>, matplotlib's
    /// <c>secondary_yaxis</c>): it shows the primary's range in other units and has no range of its own. Read for a slot past
    /// the primary; the primary has nothing to link to.</summary>
    internal bool IsLinked => Axis.Link is not null;

    /// <summary>Whether this slot, past the primary, is drawn: it has series, or it is linked, when it draws the primary's
    /// range in its own unit without any series. A slot with neither is still placed and charged its band, so the slot past it
    /// stands where the layout made room for it, but draws nothing.</summary>
    internal bool IsDrawn => SeriesList.Count > 0 || IsLinked;

    /// <summary>Adds an already-constructed series to this slot's own axis.</summary>
    /// <typeparam name="TSeries">The series type, so the caller keeps the concrete type.</typeparam>
    /// <param name="series">The series to plot against this slot's axis.</param>
    /// <returns>The same series, so the caller can keep configuring or appending to it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="series"/> is null.</exception>
    public TSeries AddSeries<TSeries>(TSeries series) where TSeries : ISeries
    {
        ArgumentNullException.ThrowIfNull(series);
        SeriesList.Add(series);
        return series;
    }
}
