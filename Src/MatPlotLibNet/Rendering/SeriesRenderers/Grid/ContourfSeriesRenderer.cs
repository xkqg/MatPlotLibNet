// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

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
    public ContourfSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(ContourfSeries series)
    {
        int rows = series.ZData.GetLength(0);
        int cols = series.ZData.GetLength(1);
        if (rows < 2 || cols < 2) return;

        int nLevels = Math.Max(2, series.Levels);
        var bands = MarchingSquares.ExtractBands(series.XData, series.YData, series.ZData, nLevels);
        if (bands.Length == 0) return;

        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        double zMin = bands[0].LevelLow;
        double zMax = bands[^1].LevelHigh;
        double zRange = zMax - zMin;
        if (zRange < 1e-12) return;

        // Apply alpha for the entire fill layer
        if (series.Alpha < 1.0)
            Ctx.SetOpacity(series.Alpha);

        // Painter's algorithm: bottom band fills the entire plot area
        double t0 = norm.Normalize(bands[0].LevelLow + (bands[0].LevelHigh - bands[0].LevelLow) * 0.5, zMin, zMax);
        var bottomColor = cmap.GetColor(t0);
        Ctx.DrawRectangle(Area.PlotBounds, bottomColor, null, 0);

        // Paint each band's polygons ascending
        for (int b = 0; b < bands.Length; b++)
        {
            double bandMid = bands[b].LevelLow + (bands[b].LevelHigh - bands[b].LevelLow) * 0.5;
            double t = norm.Normalize(bandMid, zMin, zMax);
            var color = cmap.GetColor(t);

            foreach (var polygon in bands[b].Polygons)
            {
                if (polygon.Length < 3) continue;

                var pixels = new List<Point>(polygon.Length);
                foreach (var pt in polygon)
                    pixels.Add(Transform.DataToPixel(pt.X, pt.Y));

                Ctx.DrawPolygon(pixels, color, null, 0);
            }
        }

        // Restore opacity
        if (series.Alpha < 1.0)
            Ctx.SetOpacity(1.0);

        // Overlay iso-lines if requested
        if (!series.ShowLines) return;

        double[] levels = new double[nLevels];
        for (int i = 0; i < nLevels; i++)
            levels[i] = zMin + i * zRange / (nLevels - 1);

        var contours = MarchingSquares.Extract(series.XData, series.YData, series.ZData, levels);
        foreach (var contour in contours)
        {
            if (contour.Points.Length < 2) continue;
            var pixels = new List<Point>(contour.Points.Length);
            foreach (var pt in contour.Points)
                pixels.Add(Transform.DataToPixel(pt.X, pt.Y));

            double tLine = norm.Normalize(contour.Level, zMin, zMax);
            var lineColor = cmap.GetColor(tLine);
            Ctx.DrawLines(pixels, lineColor, series.LineWidth, LineStyle.Solid);
        }
    }
}
