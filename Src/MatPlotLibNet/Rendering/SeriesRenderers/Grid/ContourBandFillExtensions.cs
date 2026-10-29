// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Algorithms;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>The one fill of a contour: the bands between the levels, painted over the grid's points. A filled
/// <see cref="ContourSeries"/> and a <see cref="ContourfSeries"/> fill through it, so the fill of both stands where the grid
/// stands, follows the axes' range and the limits the user set, and is bounded by the same iso-lines.</summary>
internal static class ContourBandFillExtensions
{
    extension(SeriesRenderContext context)
    {
        /// <summary>Paints <paramref name="bands"/>, lowest first: the lowest band over the whole grid (the bands above it
        /// paint over it), then the polygons of every band in its colour. A cell that has a value that cannot be drawn is left
        /// empty, not painted with a colour nobody measured.</summary>
        /// <param name="x">The grid's X coordinates.</param>
        /// <param name="y">The grid's Y coordinates.</param>
        /// <param name="z">The grid's values, indexed [row, column].</param>
        /// <param name="bands">The bands in ascending order (<see cref="MarchingSquares.ExtractBands(double[], double[], double[,], double[])"/>).</param>
        /// <param name="colorMap">The colour map a band's colour is taken from, at the middle of the band.</param>
        /// <param name="normalizer">How a value is placed on the colour map.</param>
        /// <param name="alpha">The opacity of the whole fill.</param>
        /// <param name="hatches">The hatch of each band, lowest first; none for a band past the end of the list.</param>
        internal void FillContourBands(double[] x, double[] y, double[,] z, MarchingSquares.ContourBand[] bands, IColorMap colorMap,
            INormalizer normalizer, double alpha = 1.0, HatchPattern[]? hatches = null)
        {
            if (bands.Length == 0) return;
            double zMin = bands[0].LevelLow, zMax = bands[^1].LevelHigh;
            if (zMax - zMin < 1e-12) return;

            if (alpha < 1.0) context.Ctx.SetOpacity(alpha);

            // Painter's algorithm: the lowest band stands under the whole grid.
            var bottomColor = colorMap.ColorOf(bands[0].LevelLow + (bands[0].LevelHigh - bands[0].LevelLow) * 0.5, normalizer, zMin, zMax);
            context.FillGrid(x, y, z, new ShapeStyle(bottomColor, null, 0));

            for (int b = 0; b < bands.Length; b++)
            {
                double bandMid = bands[b].LevelLow + (bands[b].LevelHigh - bands[b].LevelLow) * 0.5;
                var color = colorMap.ColorOf(bandMid, normalizer, zMin, zMax);
                var hatch = hatches is not null && b < hatches.Length ? hatches[b] : HatchPattern.None;

                foreach (var polygon in bands[b].Polygons)
                {
                    if (polygon.Length < 3) continue;
                    var pixels = new List<Point>(polygon.Length);
                    foreach (var pt in polygon)
                        pixels.Add(context.Transform.DataToPixel(pt.X, pt.Y));
                    context.Ctx.DrawPolygon(pixels, new ShapeStyle(color, null, 0) { Hatch = hatch });
                }
            }

            if (alpha < 1.0) context.Ctx.SetOpacity(1.0);
        }

        /// <summary>Paints the grid itself in one style: the rectangle from its first corner to its last when every cell has
        /// a value that can be drawn, otherwise the cells that have. The first corner stands where the grid begins even when
        /// the axis cannot place it (a log axis clips it to the edge where the axis begins), as a bar's foot does.</summary>
        private void FillGrid(double[] x, double[] y, double[,] z, ShapeStyle style)
        {
            int rows = Math.Min(z.GetLength(0), y.Length), cols = Math.Min(z.GetLength(1), x.Length);
            bool IsDrawable(int r, int c) => z.IsDrawableCell(x, y, r, c);

            bool masked = false;
            for (int r = 0; r < rows - 1 && !masked; r++)
                for (int c = 0; c < cols - 1 && !masked; c++)
                    masked = !IsDrawable(r, c);

            var transform = context.Transform;
            if (!masked)
            {
                var first = new Point(transform.DataToPixelClippedX(x[0], y[0]).X, transform.DataToPixelClippedY(x[0], y[0]).Y);
                var grid = Rect.FromCorners(first, transform.DataToPixel(x[cols - 1], y[rows - 1]));
                if (grid.IsDrawable()) context.Ctx.DrawRectangle(grid, style);
                return;
            }

            for (int r = 0; r < rows - 1; r++)
            {
                for (int c = 0; c < cols - 1; c++)
                {
                    if (!IsDrawable(r, c)) continue;
                    context.Ctx.DrawRectangle(Rect.FromCorners(transform.DataToPixel(x[c], y[r]), transform.DataToPixel(x[c + 1], y[r + 1])), style);
                }
            }
        }
    }
}
