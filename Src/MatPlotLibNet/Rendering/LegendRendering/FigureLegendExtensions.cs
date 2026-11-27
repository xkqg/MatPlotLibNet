// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.Layout;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.LegendRendering;

/// <summary>A legend of a figure as one draw places it: the legend, the rows it lists and where they stand. The layout measures
/// it once and the overlay draws it (<see cref="FigureLegendExtensions.PlacedLegends"/>).</summary>
/// <param name="Legend">The legend as the figure holds it, its position as given (an outside place says the layout keeps room).</param>
/// <param name="Entries">The rows it lists.</param>
/// <param name="Layout">Where the box and each row stand in the figure.</param>
internal readonly record struct PlacedFigureLegend(Legend Legend, IReadOnlyList<LegendEntry> Entries, LegendLayout Layout);

/// <summary>The legends of a figure as a whole (<see cref="Figure.Legends"/>): which rows they list, where they stand and the
/// room they take. The renderer of each axes says which of its series and patches have rows
/// (<see cref="AxesRenderer.LegendEntries(bool)"/>); here the rows of every subplot are gathered by that very rule, and under the
/// library's own themes the ones that look the same are listed once.</summary>
internal static class FigureLegendExtensions
{
    extension(Figure figure)
    {
        /// <summary>The entries of a legend of the figure: those of each subplot in subplot order, as that subplot's own legend
        /// would list them (the rule that says which series and patches have a row, in which order, with which swatch, asked of
        /// the renderer that draws these axes), the text of the series rows following the legend's
        /// <see cref="Legend.LegendValues"/>. Under the matplotlib themes every row is listed, as <c>fig.legend()</c> lists them
        /// (<see cref="Styling.FigureLook.LegendListsEveryEntry"/>); under the library's own a row drawn exactly like an earlier
        /// one (<see cref="LegendEntry.LooksLike"/>: the same label with the same handle in the same colour) is left out, so the
        /// reader reads each name once, however many subplots show it.</summary>
        /// <param name="legend">The legend, whose <see cref="Legend.LegendValues"/> decides the text of the entries.</param>
        /// <param name="ctx">The surface the subplots are measured on, which decides each renderer.</param>
        /// <returns>The entries in the order they are listed; empty when no subplot has a labelled series or patch.</returns>
        internal IReadOnlyList<LegendEntry> LegendEntries(Legend legend, IRenderContext ctx)
        {
            var entries = new List<LegendEntry>();
            bool listsEvery = figure.Theme.Looks.Figure.LegendListsEveryEntry;

            foreach (var axes in figure.SubPlots)
            {
                foreach (var entry in AxesRenderer.Create(axes, default, ctx, figure.Theme).LegendEntries(legend.LegendValues))
                {
                    bool listed = false;
                    if (!listsEvery)
                    {
                        foreach (var earlier in entries)
                        {
                            if (earlier.LooksLike(entry, figure.Theme)) { listed = true; break; }
                        }
                    }

                    if (!listed) entries.Add(entry);
                }
            }

            return entries;
        }

        /// <summary>Every legend of the figure that is drawn, placed in the rectangle that is the figure and measured with
        /// <paramref name="ctx"/>, in the order they were added. A legend that is hidden or lists no row is left out. The one
        /// placement of a draw: the layout reads the boxes from it (<see cref="RoomFor"/>) and the overlay draws it.</summary>
        /// <param name="ctx">The surface the draw measures text with.</param>
        /// <returns>The placed legends; empty, without allocating, when the figure has none.</returns>
        internal IReadOnlyList<PlacedFigureLegend> PlacedLegends(IRenderContext ctx)
        {
            if (figure.Legends.Count == 0) return [];

            var placed = new List<PlacedFigureLegend>(figure.Legends.Count);
            var area = new Rect(0, 0, figure.Width, figure.Height);
            foreach (var given in figure.Legends)
            {
                var legend = given.OnTheFigure(figure.Theme);
                var entries = figure.LegendEntries(legend, ctx);
                if (new LegendRenderer(legend, figure.Theme, ctx).Place(entries, area, () => MarkObstacles.None) is { } layout)
                    placed.Add(new PlacedFigureLegend(given, entries, layout));
            }

            return placed;
        }

        /// <summary>The room the constrained layout keeps on each edge of the figure for the legends that stand outside the axes,
        /// as matplotlib's constrained layout keeps it for a legend placed <c>outside</c>: each edge the box of its widest legend
        /// (its tallest, on the top and the bottom) and <see cref="LegendPositionStrategy.OutsideReserve"/> past it, as the
        /// position's strategy reserves for a box (<see cref="LegendPositionStrategy.ReserveFor"/>). The tight and the default
        /// layout keep none, as matplotlib's do not, and a legend inside the figure takes none under any layout.</summary>
        /// <param name="placed">The legends of this draw (<see cref="PlacedLegends"/>).</param>
        /// <returns>The room on each edge, in pixels; nothing when no legend stands outside.</returns>
        internal Sides<double> RoomFor(IReadOnlyList<PlacedFigureLegend> placed)
        {
            var room = new Sides<double>(0, 0, 0, 0);
            if (!figure.Spacing.ConstrainedLayout) return room;

            foreach (var legend in placed)
            {
                var strategy = LegendPositionStrategyFactory.Create(legend.Legend.PositionUnder(figure.Theme));
                if (!strategy.StandsOutside) continue;
                var needs = strategy.ReserveFor(new Size(legend.Layout.BoxWidth, legend.Layout.BoxHeight), default, default);
                room = new Sides<double>(Math.Max(room.Left, needs.Left), Math.Max(room.Bottom, needs.Bottom),
                    Math.Max(room.Top, needs.Top), Math.Max(room.Right, needs.Right));
            }

            return room;
        }
    }

    extension(Legend legend)
    {
        /// <summary>The legend as it stands in the figure: at the place it names, or, for a legend that names none, the theme's
        /// (<see cref="LegendLookExtensions.PositionUnder"/>, matplotlib's <c>legend.loc</c>). <see cref="LegendPosition.Best"/> looks
        /// for the place that covers the fewest marks of one axes, and a figure has no such marks, so a place that is still Best is
        /// the upper right here, as matplotlib's figure legend takes it. An outside place is the middle of that edge of the figure,
        /// inside the picture, as matplotlib's <c>outside center right</c> and its kin are. Every other position is kept.</summary>
        /// <param name="theme">The theme the figure is drawn under.</param>
        /// <returns>The legend with a position that names a place inside the figure.</returns>
        internal Legend OnTheFigure(Theme theme)
        {
            var place = legend.PositionUnder(theme) switch
            {
                LegendPosition.Best => LegendPosition.UpperRight,
                LegendPosition.OutsideRight => LegendPosition.CenterRight,
                LegendPosition.OutsideLeft => LegendPosition.CenterLeft,
                LegendPosition.OutsideTop => LegendPosition.UpperCenter,
                LegendPosition.OutsideBottom => LegendPosition.LowerCenter,
                var named => named,
            };
            return place == legend.Position ? legend : legend with { Position = place };
        }
    }
}
