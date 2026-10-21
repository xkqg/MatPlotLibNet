// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The three series lists of an axes, one per axis pair the series are drawn against.</summary>
internal enum SeriesAxisPair
{
    /// <summary><see cref="Axes.Series"/>: the bottom X axis and the left Y axis.</summary>
    Primary,

    /// <summary><see cref="Axes.SecondarySeries"/>: the bottom X axis and the right-hand Y axis.</summary>
    SecondaryY,

    /// <summary><see cref="Axes.XSecondarySeries"/>: the top X axis and the left Y axis.</summary>
    SecondaryX,
}

/// <summary>What the theme hands out at one colour-cycle slot.</summary>
/// <param name="CycleColor">The colour of the slot: the <see cref="Theme.PropCycler"/>'s colour when the theme has a
/// cycler, otherwise <see cref="Theme.CycleColors"/> at the slot, wrapping around. A series renderer receives it as
/// the fallback for a series that names no colour of its own.</param>
/// <param name="CycledProperties">The cycler's whole set for the slot (colour, dash, marker, width), handed to the series
/// renderer alongside the colour; <see langword="null"/> when the theme has no cycler.</param>
internal readonly record struct SeriesSlot(Color CycleColor, CycledProperties? CycledProperties)
{
    /// <summary>The colour <paramref name="series"/> is drawn in: its own <see cref="IHasColor.Color"/> when it sets
    /// one, otherwise the slot's colour. The legend swatch (and anything else that names a series by its colour)
    /// reads this, so it shows what the series renderer draws.</summary>
    /// <param name="series">The series that holds this slot.</param>
    /// <returns>The series' explicit colour, or <see cref="CycleColor"/>.</returns>
    internal Color ColorOf(ISeries series) => (series as IHasColor)?.Color ?? CycleColor;
}

/// <summary>The one owner of "the colour of series i". The axes decides which slot a series holds; the theme decides
/// what that slot is worth.</summary>
/// <remarks>Every site that draws a series or names it by colour goes through here: the primary series passes in
/// <see cref="AxesRenderer"/>, both secondary-axis parts, and the legend. Before this owner the secondary parts read
/// <see cref="Theme.CycleColors"/> only while the legend read the <see cref="Theme.PropCycler"/> first, so under a
/// cycler theme a secondary line and its swatch wore different colours.</remarks>
internal static class SeriesSlotExtensions
{
    /// <summary>What <paramref name="theme"/> hands out at slot <paramref name="slotIndex"/>.</summary>
    /// <param name="theme">The active theme.</param>
    /// <param name="slotIndex">The slot, as <see cref="SlotIndex"/> computes it.</param>
    /// <returns>The slot's colour and, when the theme has a cycler, its full set of cycled properties.</returns>
    internal static SeriesSlot SeriesSlot(this Theme theme, int slotIndex)
    {
        var cycled = theme.PropCycler?[slotIndex];
        return new SeriesSlot(cycled?.Color ?? theme.CycleColors[slotIndex % theme.CycleColors.Length], cycled);
    }

    /// <summary>The slot a series holds: its position in <see cref="Axes.AllSeries"/>. Primary series hold their own
    /// index, secondary-Y series continue after the primary ones, secondary-X series after both.</summary>
    /// <param name="axes">The axes that owns the series.</param>
    /// <param name="pair">The list the series sits in.</param>
    /// <param name="indexInPair">The series' index within that list.</param>
    /// <returns>The slot index.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pair"/> is not a defined value.</exception>
    internal static int SlotIndex(this Axes axes, SeriesAxisPair pair, int indexInPair) => pair switch
    {
        SeriesAxisPair.Primary => indexInPair,
        SeriesAxisPair.SecondaryY => axes.Series.Count + indexInPair,
        SeriesAxisPair.SecondaryX => axes.Series.Count + axes.SecondarySeries.Count + indexInPair,
        _ => throw new ArgumentOutOfRangeException(nameof(pair), pair, "Not a series list of an axes."),
    };

    /// <summary>The slot of the series at <paramref name="indexInPair"/> in <paramref name="pair"/>, valued by
    /// <paramref name="theme"/>.</summary>
    /// <param name="axes">The axes that owns the series.</param>
    /// <param name="theme">The active theme.</param>
    /// <param name="pair">The list the series sits in.</param>
    /// <param name="indexInPair">The series' index within that list.</param>
    /// <returns>The slot's colour and cycled properties.</returns>
    internal static SeriesSlot SeriesSlot(this Axes axes, Theme theme, SeriesAxisPair pair, int indexInPair) =>
        theme.SeriesSlot(axes.SlotIndex(pair, indexInPair));
}
