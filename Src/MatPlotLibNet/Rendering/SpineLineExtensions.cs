// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one owner of where a spine is: its position across the plot (the edge, a data value or a fraction of the
/// axes) and the stretch of the plot it spans (all of it, or the bounds a range frame sets). The four spines are drawn from it
/// (<c>RenderSpines</c>) and the tick marks hang from the position it gives, so a moved spine takes its ticks with it.</summary>
internal static class SpineLineExtensions
{
    /// <summary>The edges in the order their spines are drawn: bottom, top, left, right.</summary>
    private static readonly EdgeTerms[] DrawOrder = [EdgeTerms.Bottom, EdgeTerms.Top, EdgeTerms.Left, EdgeTerms.Right];

    extension(SpineConfig spine)
    {
        /// <summary>Where a spine that runs along X (a horizontal one, on the bottom or the top edge) stands across it, its row, or
        /// where one that runs along Y stands, its column: <paramref name="edge"/> at the edge, the pixel of a data value or of a
        /// fraction of the axes (0 at the bottom, 0 at the left: <c>FractionAlong</c>) when the spine has been moved.</summary>
        /// <param name="alongX">Whether the spine runs along X.</param>
        /// <param name="plot">The plot area.</param>
        /// <param name="edge">The coordinate of the plot's own edge on this side, across the spine.</param>
        /// <param name="transform">The transform that places a data value.</param>
        /// <returns>The coordinate across the spine, in pixels.</returns>
        internal double PlacedAcross(bool alongX, Rect plot, double edge, DataTransform transform) => spine.Position switch
        {
            SpinePosition.Data => transform.PixelAlong(spine.PositionValue, !alongX),
            SpinePosition.Axes => plot.FractionAlong(!alongX, spine.PositionValue),
            _ => edge,
        };

        /// <summary>The line of a spine that runs along X (horizontal) or along Y (vertical): across at its <see cref="PlacedAcross"/>,
        /// from its <see cref="SpineConfig.BoundsFrom"/> to its <see cref="SpineConfig.BoundsTo"/> (data values of the axis it runs
        /// along), the plot's own edges where it has none. It runs the way the screen runs: a horizontal one left to right, a vertical
        /// one from the top down, as it always has (its bounds laid along the screen by <c>AtScreenStart</c>).</summary>
        /// <param name="alongX">Whether the spine runs along X.</param>
        /// <param name="plot">The plot area.</param>
        /// <param name="edge">The coordinate of the plot's own edge on this side, across the spine.</param>
        /// <param name="transform">The transform that places a data value.</param>
        /// <returns>The line.</returns>
        internal PixelSegment LineAlong(bool alongX, Rect plot, double edge, DataTransform transform)
        {
            double across = spine.PlacedAcross(alongX, plot, edge, transform);
            double start = alongX.AtScreenStart(spine.BoundsFrom, spine.BoundsTo) is { } first ? transform.PixelAlong(first, alongX) : plot.StartAlong(alongX);
            double end = alongX.AtScreenEnd(spine.BoundsFrom, spine.BoundsTo) is { } last ? transform.PixelAlong(last, alongX) : plot.EndAlong(alongX);
            return new PixelSegment(Point.Along(alongX, start, across), Point.Along(alongX, end, across));
        }
    }

    extension(IRenderContext ctx)
    {
        /// <summary>Draws the visible spines of <paramref name="spines"/>, bottom, top, left, right, each on the line the one rule
        /// above places it at, the line its tick marks hang from, in its own group. Along a broken axis drawn in parts
        /// (<see cref="DataTransform.RoomsAlong"/>) each part has a spine of its own that stops where the part stops, as each subplot of
        /// matplotlib's broken-axis recipe has, so no spine crosses the room between two parts.</summary>
        /// <param name="spines">The four spines: visibility, position, bounds, colour, width and style.</param>
        /// <param name="transform">The transform that places a data value, over the plot the spines frame.</param>
        /// <param name="theme">The theme the spines are drawn under: the colour of a spine that sets none (its foreground) and
        /// the width of one that was given none.</param>
        internal void RenderSpines(SpinesConfig spines, DataTransform transform, Theme theme)
        {
            var plot = transform.PlotBounds;
            var allRooms = new AxisPair<PixelRoom[]>(transform.RoomsAlong(true), transform.RoomsAlong(false));
            foreach (var edge in DrawOrder)
            {
                var spine = edge.SpineOf(spines);
                if (!spine.Visible) continue;
                var line = spine.LineAlong(edge.AlongX, plot, edge.EdgeAt(plot, 0), transform);
                var rooms = allRooms.Along(edge.AlongX);
                if (rooms.Length == 0)
                {
                    ctx.DrawSpine(spine, line, theme);
                    continue;
                }

                bool alongX = edge.AlongX;
                double start = Math.Min(line.From.CoordinateAlong(alongX), line.To.CoordinateAlong(alongX));
                double end = Math.Max(line.From.CoordinateAlong(alongX), line.To.CoordinateAlong(alongX));
                foreach (var room in rooms)
                {
                    if (room.Lo > start) ctx.DrawSpine(spine, Piece(line, alongX, start, Math.Min(room.Lo, end)), theme);
                    start = Math.Max(start, room.Hi);
                }

                if (end > start) ctx.DrawSpine(spine, Piece(line, alongX, start, end), theme);
            }

            // The piece of a spine's line from one place along it to another, in screen order.
            static PixelSegment Piece(PixelSegment line, bool alongX, double from, double to) =>
                new(line.From.WithCoordinateAlong(alongX, from), line.To.WithCoordinateAlong(alongX, to));
        }

        private void DrawSpine(SpineConfig spine, PixelSegment line, Theme theme)
        {
            var stroke = spine.StrokeIn(theme);
            line = ctx.OnPixelGrid(line, stroke.Thickness, projectingEnds: true);
            ctx.BeginGroup("spine");
            ctx.DrawLine(line.From, line.To, stroke);
            ctx.EndGroup();
        }
    }
}
