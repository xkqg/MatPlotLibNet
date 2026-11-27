// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
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
        /// <summary>Paints <paramref name="bands"/>, lowest first (the painter's algorithm): the lowest band over the whole grid,
        /// then, in each higher band's colour, the region above its lower level, as one path of the pieces the cells
        /// hold of it, so the region has no seam and leaves out a valley inside it; what shows of a band is what lies between
        /// its two levels. A value below the first level shows the lowest band's colour and one above the last the highest
        /// band's. A cell that has a value that cannot be drawn is left empty, not painted with a colour nobody measured.</summary>
        /// <param name="x">The grid's X coordinates.</param>
        /// <param name="y">The grid's Y coordinates.</param>
        /// <param name="z">The grid's values, indexed [row, column].</param>
        /// <param name="bands">The bands in ascending order (<see cref="MarchingSquares.ExtractBands(double[], double[], double[,], double[])"/>).</param>
        /// <param name="mapping">The colour map a band's colour is taken from, at the middle of the band, and how a value is placed on it.</param>
        /// <param name="edges">What the theme decides about the edge of the series' bands: a band has none, but its hatch is coloured by the
        /// theme's rule, as every hatch is.</param>
        /// <param name="alpha">The opacity of the whole fill.</param>
        /// <param name="hatches">The hatch of each band, lowest first; none for a band past the end of the list.</param>
        /// <param name="colourSpan">The values the colour map runs over, from its first colour to its last; <see langword="null"/>
        /// for the bands' own, the first level to the last.</param>
        internal void FillContourBands(double[] x, double[] y, double[,] z, MarchingSquares.ContourBand[] bands, ColorMapping mapping,
            ResolvedPatchEdge edges, double alpha = 1.0, HatchPattern[]? hatches = null, MinMaxRange? colourSpan = null)
        {
            if (bands.Length == 0) return;
            double zMin = bands[0].LevelLow, zMax = bands[^1].LevelHigh;
            if (zMax - zMin < 1e-12) return;

            using var opacity = context.Ctx.WithOpacityBelowFull(alpha);

            // Painter's algorithm: the lowest band stands under the whole grid, each band above it over the bands below.
            var scale = mapping.Over(colourSpan ?? new MinMaxRange(zMin, zMax));

            // matplotlib fills a ContourSet with antialiased=False: its bands meet along the iso-lines with no edge to hide a blend.
            bool aliased = context.Theme.Looks.Patch.LikeCollections;
            context.FillGrid(x, y, z, BandStyle(bands, 0, scale, hatches, aliased, edges));

            var segments = new List<PathSegment>();
            for (int b = 1; b < bands.Length; b++)
            {
                if (bands[b].Polygons.Length == 0) continue;
                segments.Clear();
                foreach (var piece in bands[b].Polygons)
                {
                    segments.Add(new MoveToSegment(context.Transform.DataToPixel(piece[0].X, piece[0].Y)));
                    for (int i = 1; i < piece.Length; i++)
                        segments.Add(new LineToSegment(context.Transform.DataToPixel(piece[i].X, piece[i].Y)));
                    segments.Add(new CloseSegment());
                }

                context.Ctx.DrawPath(segments, BandStyle(bands, b, scale, hatches, aliased, edges));
            }
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

    /// <summary>The fill of band <paramref name="b"/>: the colour <paramref name="scale"/> gives the middle of the band, the band's
    /// hatch and whether its edge is drawn with no antialiasing (<paramref name="aliased"/>).</summary>
    private static ShapeStyle BandStyle(MarchingSquares.ContourBand[] bands, int b, ColormapContext scale,
        HatchPattern[]? hatches, bool aliased, ResolvedPatchEdge edges)
    {
        var color = scale.ColorOf(scale.Normalizer.MiddleOf(bands[b].LevelLow, bands[b].LevelHigh));
        return edges.StyleOf(color, PatchEdge.None, new HatchFill(hatches.ElementOr(b, HatchPattern.None), null)) with { Aliased = aliased };
    }
}
