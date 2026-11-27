// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>The one drawing of a 2-D grid: a line across the plot at each major tick the plot holds, and on the minor ticks of
/// the axis for a minor grid, by the rule the tick marks stand by (<see cref="TickRun.PlaceOnPlot"/>), so a grid line and its
/// tick mark always stand together.</summary>
internal static class GridLineExtensions
{
    extension(IRenderContext ctx)
    {
        /// <summary>Draws the major and/or minor grid lines of <paramref name="grid"/> behind the plot, at the ticks of the axes
        /// it asks for. A minor line wears the major line's stroke under a theme that draws the minor grid like the major, as
        /// matplotlib does, and half its width and half its colour's alpha otherwise (<see cref="AxisTickLook.MinorGridStroke"/>). Under
        /// the matplotlib themes an axis draws its minor lines only where its minor locator places ticks
        /// (<see cref="AxisTickLook.CarriesMinorGrid"/>).</summary>
        /// <param name="grid">The grid: which axes, which ticks, its colour, width and line style.</param>
        /// <param name="axes">The axes whose minor tick settings place the minor lines.</param>
        /// <param name="xTicks">The major X ticks.</param>
        /// <param name="yTicks">The major Y ticks.</param>
        /// <param name="transform">The transform the ticks are placed with, over the plot the lines run across.</param>
        /// <param name="theme">The theme the grid is drawn under, which says how a minor line differs from a major one.</param>
        internal void RenderGrid(GridStyle grid, Axes axes, double[] xTicks, double[] yTicks, DataTransform transform, Theme theme)
        {
            bool drawX = grid.Axis is GridAxis.X or GridAxis.Both;
            bool drawY = grid.Axis is GridAxis.Y or GridAxis.Both;

            if (grid.Which is GridWhich.Major or GridWhich.Both)
            {
                var major = grid.Stroke;
                if (drawX) ctx.RenderGridLines(EdgeTerms.Bottom, xTicks, transform, major, theme);
                if (drawY) ctx.RenderGridLines(EdgeTerms.Left, yTicks, transform, major, theme);
            }

            if (grid.Which is GridWhich.Minor or GridWhich.Both)
            {
                var ticks = theme.Looks.Ticks;
                var minor = ticks.MinorGridStroke(grid.Stroke);
                if (drawX && ticks.CarriesMinorGrid(axes.XAxis.MinorTicks, axes.XAxis.Scale.Rule))
                    ctx.RenderGridLines(EdgeTerms.Bottom, xTicks, transform, minor, theme, axes.XAxis);
                if (drawY && ticks.CarriesMinorGrid(axes.YAxis.MinorTicks, axes.YAxis.Scale.Rule))
                    ctx.RenderGridLines(EdgeTerms.Left, yTicks, transform, minor, theme, axes.YAxis);
            }
        }

        /// <summary>Draws the major or minor grid lines of the axis whose ticks stand on <paramref name="edge"/>: vertical lines at the X
        /// ticks of the bottom edge, horizontal lines at the Y ticks of the left edge, each across the whole plot. A major line stands at each major tick, a
        /// minor line at each minor tick of <paramref name="minorOf"/> (its own minor spacing when it set one, else the minor ticks
        /// of its scale, <see cref="TickRun.ScaleMinors"/>, else the split of the major step,
        /// <see cref="AxisTickLook.MinorValuesBetween"/>), as matplotlib draws its minor grid at
        /// the minor tick positions; either only where the plot holds its tick (<see cref="TickRun.PlaceOnPlot"/>),
        /// so no line runs over the margins or stands where the scale has no place.</summary>
        /// <param name="edge">The edge the axis' ticks stand on, which says which way it runs: the bottom edge for the X axis, the left
        /// edge for the Y axis.</param>
        /// <param name="ticks">The major ticks of that axis.</param>
        /// <param name="transform">The transform the ticks are placed with, over the plot the lines run across.</param>
        /// <param name="stroke">The line's colour, width and style.</param>
        /// <param name="theme">The theme the grid is drawn under, which says where the minor lines stand.</param>
        /// <param name="minorOf">The axis whose minor lines are drawn; <see langword="null"/> for its major lines.</param>
        internal void RenderGridLines(EdgeTerms edge, double[] ticks, DataTransform transform, StrokeStyle stroke, Theme theme,
            Axis? minorOf = null)
        {
            bool alongX = edge.AlongX;
            var run = new TickRun(ticks, transform, edge);
            var plot = transform.PlotBounds;
            if (minorOf is null)
            {
                foreach (double tick in ticks)
                    if (run.PlaceOnPlot(tick) is { } pos) ctx.DrawGridLine(pos, alongX, plot, stroke);
                return;
            }

            var minor = minorOf.MinorTicks;
            var scaleMinors = minor.Spacing is > 0
                ? null
                : run.ScaleMinors(minorOf.TickSpaceAlong(plot.LengthAlong(alongX), theme, alongX), theme);
            foreach (double value in theme.Looks.Ticks.MinorValuesBetween(minor, ticks, scaleMinors))
                if (run.PlaceOnPlot(value) is { } pos) ctx.DrawGridLine(pos, alongX, plot, stroke);
        }

        /// <summary>One grid line across the whole plot: a vertical one at <paramref name="pos"/> along X
        /// (<paramref name="alongX"/>), a horizontal one at it along Y otherwise. A grid line is matplotlib's <c>Line2D</c> of one
        /// straight segment, which its Agg backend puts on the pixel grid by the line's width; where the theme asks, so is this one
        /// on a surface that has a grid. Its ends stand on the plot's edges, under the spines, where matplotlib clips its line.</summary>
        private void DrawGridLine(double pos, bool alongX, Rect plot, StrokeStyle stroke)
        {
            var line = new SeriesRenderers.PixelSegment(Point.Along(alongX, pos, plot.StartAlong(!alongX)), Point.Along(alongX, pos, plot.EndAlong(!alongX)));
            line = ctx.OnPixelGrid(line, stroke.Thickness, projectingEnds: false);
            ctx.DrawLine(line.From, line.To, stroke);
        }
    }
}
