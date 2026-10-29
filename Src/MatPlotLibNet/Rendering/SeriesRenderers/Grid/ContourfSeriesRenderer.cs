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
        var norm = series.NormalizerOrLinear();

        double zMin = bands[0].LevelLow;
        double zMax = bands[^1].LevelHigh;
        double zRange = zMax - zMin;
        if (zRange < 1e-12) return;

        // The fill, by the one code a filled contour fills through, at the opacity of the whole layer.
        Context.FillContourBands(series.XData, series.YData, series.ZData, bands, cmap, norm, series.Alpha, series.Hatches);

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
}
