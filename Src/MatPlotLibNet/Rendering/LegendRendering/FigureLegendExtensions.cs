// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>The legend of a figure as a whole (<see cref="Figure.Legend"/>): which entries it lists and where it stands. The
/// renderer of each axes says which of its series and patches have rows (<see cref="AxesRenderer.LegendEntries(bool)"/>); here the
/// rows of every subplot are gathered by that very rule and the ones that look the same are listed once.</summary>
internal static class FigureLegendExtensions
{
    extension(Figure figure)
    {
        /// <summary>The entries of the figure's legend: those of each subplot in subplot order, as that subplot's own legend would
        /// list them (the rule that says which series and patches have a row, in which order, with which swatch, asked of the
        /// renderer that draws these axes), the text of the series rows following the figure legend's
        /// <see cref="Legend.LegendValues"/>. A row that is drawn exactly like an earlier one (<see cref="LegendEntry.LookUnder"/>:
        /// the same label with the same handle in the same colour) is left out: the reader needs each name once, however many
        /// subplots show it.</summary>
        /// <param name="legend">The figure's legend, whose <see cref="Legend.LegendValues"/> decides the text of the entries.</param>
        /// <param name="ctx">The surface the subplots are measured on, which decides each renderer.</param>
        /// <returns>The entries in the order they are listed; empty when no subplot has a labelled series or patch.</returns>
        internal IReadOnlyList<LegendEntry> LegendEntries(Legend legend, IRenderContext ctx)
        {
            var entries = new List<LegendEntry>();
            var seen = new HashSet<object>();

            foreach (var axes in figure.SubPlots)
            {
                foreach (var entry in AxesRenderer.Create(axes, default, ctx, figure.Theme).LegendEntries(legend.LegendValues))
                {
                    if (seen.Add(entry.LookUnder(figure.Theme)))
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
}
