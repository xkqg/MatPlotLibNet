// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one owner of how a ternary triangle is gridded and ticked, for every chart that draws one: the ternary axes
/// and the two triangles of a Piper diagram. The caller supplies where a composition stands (<c>place</c>, in pixels), so the
/// same drawing serves a triangle in any position and size.</summary>
internal static class TernaryDrawingExtensions
{
    /// <summary>The fractions the grid is drawn at and the ticks are labelled at: 0.2, 0.4, 0.6 and 0.8. The corners are 0 and 1
    /// and carry the names of the parts.</summary>
    internal static readonly double[] Fifths = [0.2, 0.4, 0.6, 0.8];

    /// <summary>The room, in pixels, between an edge and the tick label beside it.</summary>
    internal const double TickGap = 6;

    extension(IRenderContext ctx)
    {
        /// <summary>The grid: for each part, a line at each fifth, from the edge that part is ticked on across to the next.</summary>
        /// <param name="place">Where a composition stands, in pixels.</param>
        /// <param name="stroke">How the lines are drawn.</param>
        internal void DrawTernaryGrid(Func<TernaryPoint, Point> place, StrokeStyle stroke)
        {
            foreach (double v in Fifths)
            {
                double rest = 1 - v;
                ctx.DrawLine(place(new(v, rest, 0)), place(new(v, 0, rest)), stroke);   // the top part
                ctx.DrawLine(place(new(rest, v, 0)), place(new(0, v, rest)), stroke);   // the left part
                ctx.DrawLine(place(new(0, rest, v)), place(new(rest, 0, v)), stroke);   // the right part
            }
        }

        /// <summary>The tick labels, counter-clockwise: the right part under the base, the top part right of the right edge, the
        /// left part left of the left edge.</summary>
        /// <param name="place">Where a composition stands, in pixels.</param>
        /// <param name="font">The font of the labels.</param>
        /// <param name="label">The text of a tick at a fraction of the whole.</param>
        internal void DrawTernaryTicks(Func<TernaryPoint, Point> place, Font font, Func<double, string> label)
        {
            double lift = font.Size * 0.35;
            foreach (double v in Fifths)
            {
                string text = label(v);

                var onBase = place(new(0, 1 - v, v));
                ctx.DrawText(text, new Point(onBase.X, onBase.Y + font.Size + TickGap - 2), font, TextAlignment.Center);

                var onRight = place(new(v, 0, 1 - v));
                ctx.DrawText(text, new Point(onRight.X + TickGap, onRight.Y + lift), font, TextAlignment.Left);

                var onLeft = place(new(1 - v, v, 0));
                ctx.DrawText(text, new Point(onLeft.X - TickGap, onLeft.Y + lift), font, TextAlignment.Right);
            }
        }
    }
}
