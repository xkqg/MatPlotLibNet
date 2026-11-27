// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for one linked X axis below the plot (<see cref="AxesDto.BottomXAxes"/>, a slot of
/// <see cref="YAxisContainer.Bottom"/>): its axis, which carries the link to the primary X axis, and where it stands. It holds
/// no series (<see cref="YAxisSlot.AddSeries{TSeries}"/> refuses one on a bottom slot), so the record has no field for one and a
/// strict reader refuses a series written there as an unknown field.</summary>
internal sealed record XAxisSlotDto
{
    /// <summary>The axis: its label, link, ticks and colours.</summary>
    public AxisDto? Axis { get; init; }
    /// <summary>Where the axis stands (<see cref="YAxisSlot.Location"/>); absent when it stacks.</summary>
    public double? Location { get; init; }

    /// <summary>The JSON's form of <paramref name="slot"/>: its axis and its location.</summary>
    /// <param name="slot">The slot of an axis below the plot.</param>
    /// <returns>The record of the slot.</returns>
    internal static XAxisSlotDto Of(YAxisSlot slot) => new()
    {
        Axis = AxisDto.Of(slot.Axis),
        Location = slot.Location,
    };

    /// <summary>Restores this axis and its location onto <paramref name="slot"/>, which <see cref="YAxisStack.Add"/> already
    /// created; a location that is not finite is left out, as the builder refuses one.</summary>
    /// <param name="slot">The slot read into.</param>
    internal void RestoreOnto(YAxisSlot slot)
    {
        Axis?.ApplyTo(slot.Axis);
        if (Location is { } location && double.IsFinite(location)) slot.Location = location;
    }
}
