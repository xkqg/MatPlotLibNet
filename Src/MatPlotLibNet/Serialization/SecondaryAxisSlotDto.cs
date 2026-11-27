// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Serialization;

/// <summary>Data-transfer object for one <see cref="YAxisSlot"/> past slot 0 of its side: an entry of the frozen lists
/// (<see cref="AxesDto.ExtraRightYAxes"/>, <see cref="AxesDto.LeftYAxes"/>) or of the overflow lists
/// (<see cref="AxesDto.RightYAxesOverflow"/>, <see cref="AxesDto.LeftYAxesOverflow"/>). A linked X axis below the plot is
/// <see cref="XAxisSlotDto"/>.</summary>
internal sealed record SecondaryAxisSlotDto
{
    /// <summary>The slot's axis: its label, limits, ticks, scale and colours.</summary>
    public AxisDto? Axis { get; init; }
    /// <summary>The series plotted against the slot's axis; absent when it has none.</summary>
    public List<SeriesDto>? Series { get; init; }

    /// <summary>The JSON's form of <paramref name="slot"/>: its axis, and its series when it has any.</summary>
    internal static SecondaryAxisSlotDto Of(YAxisSlot slot) => new()
    {
        Axis = AxisDto.Of(slot.Axis),
        Series = slot.Series.Count > 0 ? slot.Series.ToDtos() : null,
    };

    /// <summary>Restores this slot's own axis settings and series onto <paramref name="slot"/>, which
    /// <see cref="YAxisStack.Add"/> already created.</summary>
    /// <param name="axes">The axes being read; its series registry builds the slot's series.</param>
    /// <param name="slot">The slot read into.</param>
    internal void RestoreOnto(Axes axes, YAxisSlot slot)
    {
        Axis?.ApplyTo(slot.Axis);
        foreach (var sDto in Series ?? [])
            axes.CreateOnSecondarySeat(sDto, s => slot.AddSeries(s));
    }
}
