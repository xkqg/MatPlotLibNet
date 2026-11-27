// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Where a straight mark stands on the pixel grid of a surface that has one, asked of any surface. The raster export of the
/// Skia package has a grid (<see cref="IPixelGridRenderContext"/>) when the theme it was told asks for one
/// (<see cref="RasterLook.StrokesOnPixelGrid"/>, the matplotlib themes: <see cref="IThemedRenderContext"/>); a surface without a grid, a
/// surface of your own, or a theme that does not ask, leaves the mark where it stands. The surface is the one gate: a renderer asks
/// this member, never the type of its surface and never the theme.</summary>
internal static class RenderContextPixelGridExtensions
{
    extension(IRenderContext ctx)
    {
        /// <summary>A horizontal or vertical stroke on the pixel grid of this surface (<see cref="IPixelGridRenderContext.OnPixelGrid(PixelSegment, double, bool)"/>);
        /// the stroke itself on a surface without a grid.</summary>
        /// <param name="line">The stroke, in the figure's units.</param>
        /// <param name="thickness">The width it is stroked with, in the figure's units.</param>
        /// <param name="projectingEnds">Whether its two ends carry on by half the width (a spine); butt ends otherwise.</param>
        internal PixelSegment OnPixelGrid(PixelSegment line, double thickness, bool projectingEnds) =>
            ctx is IPixelGridRenderContext grid ? grid.OnPixelGrid(line, thickness, projectingEnds) : line;

        /// <summary>A line of horizontal and vertical segments (a step line, an ECDF) on the pixel grid of this surface, as matplotlib's
        /// Agg backend snaps a <c>Line2D</c> (<see cref="IPixelGridRenderContext.OnPixelGrid(IReadOnlyList{Point}, double)"/>); the line
        /// itself on a surface without a grid or when a segment of it is slanted.</summary>
        /// <param name="line">The line's points, in the figure's units.</param>
        /// <param name="thickness">The width it is stroked with, in the figure's units.</param>
        internal IReadOnlyList<Point> OnPixelGrid(IReadOnlyList<Point> line, double thickness) =>
            ctx is IPixelGridRenderContext grid ? grid.OnPixelGrid(line, thickness) : line;

        /// <summary>A rectangle drawn in <paramref name="style"/> (a bar, a box, a cell of a matrix) on the pixel grid of this surface
        /// (<see cref="IPixelGridRenderContext.OnPixelGrid(Rect, double)"/>), by the width of its edge when the style strokes one and
        /// as a shape with no edge otherwise; the rectangle itself on a surface without a grid.</summary>
        /// <param name="area">The rectangle, in the figure's units.</param>
        /// <param name="style">The style it is drawn in: whether it has an edge, and how wide.</param>
        internal Rect OnPixelGrid(Rect area, ShapeStyle style) =>
            ctx is IPixelGridRenderContext grid ? grid.OnPixelGrid(area, style.HasVisibleStroke ? style.StrokeThickness : 0) : area;

        /// <summary>A rectangle (a bar, a box, a cell of a matrix) drawn in <paramref name="style"/> with its corners on the pixel grid of
        /// this surface: put where the grid puts it (<see cref="IPixelGridRenderContext.OnPixelGrid(Rect, double)"/>, by the width of its edge), then drawn as <see cref="IRenderContext.DrawRectangle"/>
        /// draws it. The one way a renderer draws a rectangle that stands on the grid, so the two halves of the rule never part; drawn
        /// where it stands on a surface without a grid.</summary>
        /// <param name="area">The rectangle, in the figure's units.</param>
        /// <param name="style">The style it is drawn in: whether it has an edge, and how wide.</param>
        internal void DrawSnappedRectangle(Rect area, ShapeStyle style) =>
            ctx.DrawRectangle(ctx.OnPixelGrid(area, style), style);

        /// <summary>A rectangle with an edge, drawn on the pixel grid of this surface as matplotlib's Agg backend draws a patch
        /// (<see cref="IPixelGridRenderContext.DrawRectangleOnPixelGrid"/>); drawn where it stands, as
        /// <see cref="IRenderContext.DrawRectangle"/> draws it, on a surface without a grid.</summary>
        /// <param name="area">The rectangle, in the figure's units.</param>
        /// <param name="style">The style it is drawn in.</param>
        internal void DrawRectangleOnPixelGrid(Rect area, ShapeStyle style)
        {
            if (ctx is IPixelGridRenderContext grid) grid.DrawRectangleOnPixelGrid(area, style);
            else ctx.DrawRectangle(area, style);
        }

        /// <summary>A tick mark on the pixel grid of this surface, as matplotlib draws a marker
        /// (<see cref="IPixelGridRenderContext.MarkerOnPixelGrid"/>): <paramref name="marker"/>, the stroke as matplotlib's marker has
        /// it, hung from <paramref name="anchor"/>. A surface without a grid, or a stroke that is neither horizontal nor vertical,
        /// draws <paramref name="asDrawn"/>, the stroke as the library draws it everywhere else.</summary>
        /// <param name="anchor">The point the mark hangs from, in the figure's units.</param>
        /// <param name="marker">The stroke as matplotlib's marker draws it from that point.</param>
        /// <param name="asDrawn">The stroke where nothing is snapped.</param>
        /// <param name="thickness">The width it is stroked with, in the figure's units.</param>
        internal PixelSegment MarkerOnPixelGrid(Point anchor, PixelSegment marker, PixelSegment asDrawn, double thickness) =>
            ctx is IPixelGridRenderContext grid && grid.MarkerOnPixelGrid(anchor, marker, thickness) is { } snapped ? snapped : asDrawn;

        /// <summary>Whether this surface fills a grid of cells in one pass (<see cref="IPixelGridRenderContext.FillsCellGrids"/>); a
        /// surface without a pixel grid does not, and its cells are drawn one rectangle each.</summary>
        internal bool FillsCellGrids => ctx is IPixelGridRenderContext { FillsCellGrids: true };

        /// <summary>Fills <paramref name="area"/> with a grid of cells in one pass on a surface that does
        /// (<see cref="IPixelGridRenderContext.FillCellGrid"/>); asked only where <see cref="IPixelGridRenderContext.FillsCellGrids"/>
        /// holds for it.</summary>
        /// <param name="colours">The cells' colours row by row from the top, each row left to right.</param>
        /// <param name="columns">How many cells a row holds.</param>
        /// <param name="area">The rectangle the grid fills, in the figure's units.</param>
        internal void FillCellGrid(ReadOnlySpan<Color> colours, int columns, Rect area) =>
            ((IPixelGridRenderContext)ctx).FillCellGrid(colours, columns, area);
    }
}
