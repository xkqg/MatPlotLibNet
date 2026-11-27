// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Rendering.SeriesRenderers;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>A surface that paints a pixel grid, and what it adds to where a straight mark stands on that grid: the raster export of
/// the Skia package. A renderer reaches it through <see cref="RenderContextPixelGridExtensions"/> on any <see cref="IRenderContext"/>,
/// where a surface with no grid (markup, a vector page) leaves a mark where it stands; it never asks the type of its surface. It
/// lives here and not on <see cref="IRenderContext"/> so that an application can still implement, proxy or mock the public
/// interface.</summary>
internal interface IPixelGridRenderContext : IRenderContext
{
    /// <summary>A horizontal or vertical stroke, put on the pixel grid the way matplotlib's Agg backend puts a stroke of
    /// horizontal and vertical segments there: each end moved to a whole pixel and, when the width rounds to an odd number of
    /// pixels, half a pixel on, so a line fills one column or row exactly and the rest of its width spills evenly onto the two
    /// beside it.</summary>
    /// <param name="line">The stroke, in the figure's units.</param>
    /// <param name="thickness">The width it is stroked with, in the figure's units.</param>
    /// <param name="projectingEnds">Whether its two ends carry on by half the width, as the projecting caps of a spine do; a
    /// mark with butt ends (the ticks of a rug plot) is as long as it was given.</param>
    /// <returns>The stroke to draw; <paramref name="line"/> itself when this surface does not snap, or the stroke is neither
    /// horizontal nor vertical.</returns>
    PixelSegment OnPixelGrid(PixelSegment line, double thickness, bool projectingEnds);

    /// <summary>A rectangle with each corner put on the pixel grid the way matplotlib's Agg backend puts a path of horizontal and
    /// vertical segments there (a bar): each corner moved to a whole pixel and, when its edge is drawn with a width that rounds to an
    /// odd number of pixels, half a pixel on. With no edge, each side stands on a pixel edge, which is also where matplotlib's image
    /// of a matrix puts the side of a cell, so two cells meet on a pixel edge and no pixel blends them.</summary>
    /// <param name="area">The rectangle, in the figure's units.</param>
    /// <param name="edgeThickness">The width its edge is drawn with, in the figure's units; 0 when it has no visible edge.</param>
    /// <returns>The rectangle to draw; <paramref name="area"/> itself when this surface does not snap.</returns>
    Rect OnPixelGrid(Rect area, double edgeThickness);

    /// <summary>A rectangle with an edge drawn as matplotlib's Agg backend draws a patch of horizontal and vertical segments (a cell of a
    /// table): the rectangle put on the pixel grid as <see cref="OnPixelGrid(Rect, double)"/> puts it, its fill laid down, then its edge as
    /// the frame the edge covers, so each pixel takes the share of the edge that falls in it. A stroke path covers a pixel in quarters, so
    /// the pixels beside a rule of 1.39 pixels were a quarter covered where Agg covers 0.19 of them. A dashed edge, or a rectangle with
    /// no edge, is drawn as <see cref="IRenderContext.DrawRectangle"/> draws it, from the rectangle put on the grid.</summary>
    /// <param name="area">The rectangle, in the figure's units.</param>
    /// <param name="style">The style it is drawn in.</param>
    void DrawRectangleOnPixelGrid(Rect area, ShapeStyle style);

    /// <summary>A mark of one horizontal or vertical stroke hung from a point (a tick mark), put on the pixel grid the way
    /// matplotlib's Agg backend draws a marker: the point is rounded to a whole pixel, and each end of the stroke is moved from there
    /// by its offset rounded to a whole pixel and, when the width rounds to an odd number of pixels, half a pixel on.</summary>
    /// <param name="anchor">The point the mark hangs from, in the figure's units.</param>
    /// <param name="mark">The stroke, in the figure's units.</param>
    /// <param name="thickness">The width it is stroked with, in the figure's units.</param>
    /// <returns>The stroke to draw; <see langword="null"/> when this surface does not snap or the stroke is neither horizontal nor
    /// vertical.</returns>
    PixelSegment? MarkerOnPixelGrid(Point anchor, PixelSegment mark, double thickness);

    /// <summary>A line of horizontal and vertical segments (a step line, an ECDF) put on the pixel grid the way matplotlib's Agg backend
    /// snaps a <c>Line2D</c> in its automatic mode: when the line has at most 1024 points and every segment it draws is horizontal or
    /// vertical, each point moved to a whole pixel and, when the width rounds to an odd number of pixels, half a pixel on.</summary>
    /// <param name="line">The line's points, in the figure's units; a point that cannot be drawn breaks it.</param>
    /// <param name="thickness">The width it is stroked with, in the figure's units.</param>
    /// <returns>The points to draw; <paramref name="line"/> itself when this surface does not snap or the line is not one of
    /// horizontal and vertical segments.</returns>
    IReadOnlyList<Point> OnPixelGrid(IReadOnlyList<Point> line, double thickness);

    /// <summary>Whether this surface fills a grid of cells (an image) in one pass (<see cref="FillCellGrid"/>): it paints pixels and the
    /// theme it draws under puts marks on its pixel grid. A surface that does not draws each cell as a rectangle.</summary>
    bool FillsCellGrids { get; }

    /// <summary>Fills <paramref name="area"/> with a grid of cells in one pass, as matplotlib's Agg backend draws an image: the area put on
    /// the pixel grid as <see cref="OnPixelGrid(Rect, double)"/> puts a rectangle with no edge, and each pixel in it the colour of the cell
    /// its centre falls in, so two cells meet on a pixel edge and no pixel is a blend of two cells or of a cell and the ground. Drawn under
    /// the opacity the surface was last given.</summary>
    /// <param name="colours">The cells' colours row by row from the top, each row left to right as it is shown.</param>
    /// <param name="columns">How many cells a row holds; the number of rows is the number of colours over it.</param>
    /// <param name="area">The rectangle the grid fills, in the figure's units, of positive width and height.</param>
    void FillCellGrid(ReadOnlySpan<Color> colours, int columns, Rect area);
}
