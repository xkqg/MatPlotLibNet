// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>The legend of a figure as a whole (<see cref="Figure.Legend"/>): which entries it lists and where it stands. The
/// axes decides which of its series are entries (<see cref="AxesLegendEntryExtensions"/>); here the entries of every subplot are
/// gathered by that very rule and the ones that look the same are listed once.</summary>
internal static class FigureLegendExtensions
{
    extension(Figure figure)
    {
        /// <summary>The entries of the figure's legend: those of each subplot in subplot order, as that subplot's own legend would
        /// list them (the rule that says which series have an entry, in which order, with which swatch; whether a series that asked
        /// for an end label leaves is asked of the renderer that draws these axes), the text of the entries following the figure
        /// legend's <see cref="Legend.LegendValues"/>. An entry that is drawn exactly like an earlier one, the same label with the
        /// same handle in the same colour, is left out: the reader needs each name once, however many subplots show it.</summary>
        /// <param name="legend">The figure's legend, whose <see cref="Legend.LegendValues"/> decides the text of the entries.</param>
        /// <param name="ctx">The surface the subplots are measured on, which decides each renderer.</param>
        /// <returns>The entries in the order they are listed; empty when no subplot has a labelled series.</returns>
        internal IReadOnlyList<LegendEntry> LegendEntries(Legend legend, IRenderContext ctx)
        {
            var entries = new List<LegendEntry>();
            var seen = new HashSet<EntryLook>();

            foreach (var axes in figure.SubPlots)
            {
                bool directLabelsDrawn = AxesRenderer.Create(axes, default, ctx, figure.Theme).DrawsDirectLabels;
                foreach (var entry in axes.LegendEntries(directLabelsDrawn, legend.LegendValues))
                {
                    if (seen.Add(EntryLook.Of(entry, figure.Theme)))
                        entries.Add(entry);
                }
            }

            return entries;
        }
    }

    extension(Legend legend)
    {
        /// <summary>The legend as it stands in the figure. <see cref="LegendPosition.Best"/> looks for the place that covers the
        /// fewest marks of one axes, and a figure has no such marks, so it is the upper right here, whatever place the theme gives
        /// a legend that names none; every other position is kept.</summary>
        /// <returns>The legend with a position that names a place.</returns>
        internal Legend OnTheFigure() =>
            legend.Position == LegendPosition.Best ? legend with { Position = LegendPosition.UpperRight } : legend;
    }

    /// <summary>What an entry looks like once drawn: its text, its handle and the colour and stroke its colour slot gives the
    /// series. Two entries that are equal here are the same entry to the eye.</summary>
    private readonly record struct EntryLook(string Label, LegendSwatch Swatch, SeriesSlot Slot, Color Color)
    {
        /// <summary>The look of <paramref name="entry"/> under <paramref name="theme"/>.</summary>
        internal static EntryLook Of(LegendEntry entry, Theme theme)
        {
            var slot = theme.SeriesSlot(entry.ColorSlotIndex);
            return new(entry.Label, entry.Swatch, slot, slot.ColorOf(entry.Series));
        }
    }
}
