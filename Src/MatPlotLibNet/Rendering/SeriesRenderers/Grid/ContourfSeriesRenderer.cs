// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Algorithms;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>
/// Renders a <see cref="ContourfSeries"/> using the painter's algorithm:
/// fills the entire plot area with the bottom-band color, then paints each
/// ascending band's closed polygon(s) over the previous, and optionally
/// overlays the iso-line boundaries.
/// </summary>
internal sealed class ContourfSeriesRenderer : SeriesRenderer<ContourfSeries>
{
    /// <inheritdoc />
    public ContourfSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ContourfSeries series)
    {
        int rows = series.ZData.GetLength(0);
        int cols = series.ZData.GetLength(1);
        if (rows < 2 || cols < 2) return;

        // The levels the user named bound the bands, or else the count's evenly spaced levels over the grid's range.
        int nLevels = Math.Max(2, series.Levels);
        double[]? named = series.LevelValues.ExplicitLevels();
        var bands = named is null
            ? MarchingSquares.ExtractBands(series.XData, series.YData, series.ZData, nLevels)
            : MarchingSquares.ExtractBands(series.XData, series.YData, series.ZData, named);
        if (bands.Length == 0) return;

        var cmap = series.GetColorMapOrDefault(ColorMaps.Viridis);
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        double zMin = bands[0].LevelLow;
        double zMax = bands[^1].LevelHigh;
        double zRange = zMax - zMin;
        if (zRange < 1e-12) return;

        // Apply alpha for the entire fill layer
        if (series.Alpha < 1.0)
            Ctx.SetOpacity(series.Alpha);

        // Painter's algorithm: bottom band fills the entire plot area
        var bottomColor = cmap.ColorOf(bands[0].LevelLow + (bands[0].LevelHigh - bands[0].LevelLow) * 0.5, norm, zMin, zMax);
        FillBottomBand(series, new ShapeStyle(bottomColor, null, 0));

        // Paint each band's polygons ascending
        for (int b = 0; b < bands.Length; b++)
        {
            double bandMid = bands[b].LevelLow + (bands[b].LevelHigh - bands[b].LevelLow) * 0.5;
            var color = cmap.ColorOf(bandMid, norm, zMin, zMax);

            foreach (var polygon in bands[b].Polygons)
            {
                if (polygon.Length < 3) continue;

                var pixels = new List<Point>(polygon.Length);
                foreach (var pt in polygon)
                    pixels.Add(Transform.DataToPixel(pt.X, pt.Y));

                var bandHatch = series.Hatches is not null && b < series.Hatches.Length
                    ? series.Hatches[b]
                    : HatchPattern.None;
                Ctx.DrawPolygon(pixels, new ShapeStyle(color, null, 0) { Hatch = bandHatch });
            }
        }

        // Restore opacity
        if (series.Alpha < 1.0)
            Ctx.SetOpacity(1.0);

        // Overlay iso-lines if requested
        if (!series.ShowLines) return;

        // The lines stand on the bands' edges: the levels named, or the count's levels over the span the bands cover.
        double[] levels = named ?? ContourLevels.Spanning(zMin, zMax, nLevels);

        var contours = MarchingSquares.Extract(series.XData, series.YData, series.ZData, levels);
        foreach (var contour in contours)
        {
            if (contour.Points.Length < 2) continue;
            var pixels = new List<Point>(contour.Points.Length);
            foreach (var pt in contour.Points)
                pixels.Add(Transform.DataToPixel(pt.X, pt.Y));

            var lineColor = cmap.ColorOf(contour.Level, norm, zMin, zMax);
            Ctx.DrawLines(pixels, new StrokeStyle(lineColor, series.LineWidth, LineStyle.Solid));
        }
    }

    /// <summary>Paints the lowest band under the others: over the whole plot area, or, when the grid has a cell with a
    /// missing corner, over each drawable cell, so a masked cell stays empty as matplotlib leaves it (painting it would
    /// show the lowest band where nothing was measured).</summary>
    private void FillBottomBand(ContourfSeries series, ShapeStyle style)
    {
        double[] x = series.XData, y = series.YData;
        double[,] z = series.ZData;
        int rows = Math.Min(z.GetLength(0), y.Length), cols = Math.Min(z.GetLength(1), x.Length);
        bool IsDrawable(int r, int c) => z.IsDrawableCell(x, y, r, c);

        bool masked = false;
        for (int r = 0; r < rows - 1 && !masked; r++)
            for (int c = 0; c < cols - 1 && !masked; c++)
                masked = !IsDrawable(r, c);

        if (!masked)
        {
            Ctx.DrawRectangle(Area.PlotBounds, style);
            return;
        }

        for (int r = 0; r < rows - 1; r++)
        {
            for (int c = 0; c < cols - 1; c++)
            {
                if (!IsDrawable(r, c)) continue;
                Ctx.DrawRectangle(Rect.FromCorners(Transform.DataToPixel(x[c], y[r]), Transform.DataToPixel(x[c + 1], y[r + 1])), style);
            }
        }
    }
}
