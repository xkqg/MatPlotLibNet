// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering;

/// <summary>The coloured cells of an image: the colour range its values are coloured over and the grid of rectangles, one for
/// each cell, that fills a rectangle. The one owner of both, so an image series (<c>ImageSeriesRenderer</c>, scaled to its plot
/// area) and an image placed on the figure (<see cref="Models.FigureImage"/>, one pixel for each cell) colour and draw their
/// cells by the same rule.</summary>
/// <param name="ColorMap">The map a normalised value is looked up in.</param>
/// <param name="Normalizer">Maps a value onto the colour map's 0 to 1 range, over <paramref name="Min"/> to <paramref name="Max"/>.</param>
/// <param name="Min">The value at the low end of the colour range.</param>
/// <param name="Max">The value at the high end of the colour range.</param>
/// <param name="Alpha">The opacity of the whole image; below 1 the cells are drawn under that opacity.</param>
internal readonly record struct ImageCells(IColorMap ColorMap, INormalizer Normalizer, double Min, double Max, double Alpha)
{
    /// <summary>The cells for an image made of <paramref name="data"/>. The colour range is the one the values span, a missing
    /// value (NaN) and an infinity left out of it (a minimum taken with <c>Math.Min</c> used to carry one NaN into the range, and
    /// every pixel with it), unless a limit is given; a range of no width is made one wide, so no normalizer divides by zero,
    /// and with no value that can be placed it is 0 to 1.</summary>
    /// <param name="data">The values the colour range is taken from: the image's own, not a resampled copy of them.</param>
    /// <param name="colorMap">The map the values are coloured through.</param>
    /// <param name="normalizer">How a value is mapped onto the colour map.</param>
    /// <param name="vMin">The value at the low end of the colour range, or <see langword="null"/> for the smallest value.</param>
    /// <param name="vMax">The value at the high end of the colour range, or <see langword="null"/> for the largest value.</param>
    /// <param name="alpha">The opacity of the whole image.</param>
    /// <returns>The cells, ready to be drawn.</returns>
    internal static ImageCells Of(double[,] data, IColorMap colorMap, INormalizer normalizer, double? vMin, double? vMax, double alpha)
    {
        var drawable = data.DrawableRange() ?? new Range1D(0, 1);
        double min = vMin ?? drawable.Lo, max = vMax ?? drawable.Hi;
        if (min == max) max = min + 1;
        return new(colorMap, normalizer, min, max, alpha);
    }

    /// <summary>Draws <paramref name="pixels"/> as a grid of rectangles that fills <paramref name="bounds"/>, row by row from
    /// the top, each in the colour its value has under the colour range. A missing value takes the map's bad colour, transparent
    /// when it names none. A grid with no row or no column draws nothing, not even the opacity.</summary>
    /// <param name="ctx">The surface to draw on.</param>
    /// <param name="pixels">The values to draw, indexed [row, column]; the cells that are drawn, which may be a resampled copy of the
    /// values the range was taken from.</param>
    /// <param name="bounds">The rectangle the whole grid fills; each cell is its share of it.</param>
    /// <param name="skipTransparentCells">Whether a cell that would be painted fully transparent (a missing value under a map
    /// that names no bad colour, or any value that maps to a clear colour) is left out instead of drawn as an invisible rectangle.
    /// An image series draws it, as it always has; an image at its native size, with an element for each pixel, leaves it out.</param>
    internal void Draw(IRenderContext ctx, double[,] pixels, Rect bounds, bool skipTransparentCells = false)
    {
        int rows = pixels.GetLength(0), cols = pixels.GetLength(1);
        if (rows == 0 || cols == 0) return;

        double cellW = bounds.Width / cols;
        double cellH = bounds.Height / rows;

        if (Alpha < 1.0)
            ctx.SetOpacity(Alpha);

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            var color = ColorMap.ColorOf(pixels[r, c], Normalizer, Min, Max);
            if (skipTransparentCells && color.A == 0) continue;

            ctx.DrawRectangle(
                new Rect(bounds.X + c * cellW, bounds.Y + r * cellH, cellW, cellH),
                new ShapeStyle(color, null, 0));
        }

        if (Alpha < 1.0)
            ctx.SetOpacity(1.0);
    }
}
