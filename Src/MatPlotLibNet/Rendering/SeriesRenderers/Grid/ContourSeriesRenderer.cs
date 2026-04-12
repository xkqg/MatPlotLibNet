// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Algorithms;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders <see cref="ContourSeries"/> instances onto an <see cref="IRenderContext"/>.</summary>
internal sealed class ContourSeriesRenderer : SeriesRenderer<ContourSeries>
{
    /// <inheritdoc />
    public ContourSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(ContourSeries series)
    {
        int rows = series.ZData.GetLength(0);
        int cols = series.ZData.GetLength(1);
        if (rows < 2 || cols < 2) return;

        // Filled contour: delegate to heatmap for the background fill
        if (series.Filled)
            new HeatmapSeriesRenderer(Context).Render(new HeatmapSeries(series.ZData) { ColorMap = series.ColorMap });

        // Compute Z range and build evenly spaced levels
        double zMin = double.MaxValue, zMax = double.MinValue;
        foreach (double v in series.ZData) { zMin = Math.Min(zMin, v); zMax = Math.Max(zMax, v); }
        if (zMin >= zMax) return;

        int nLevels = Math.Max(2, series.Levels);
        var levels = new double[nLevels];
        for (int i = 0; i < nLevels; i++)
            levels[i] = zMin + (zMax - zMin) * (i + 1) / (nLevels + 1);

        // Extract iso-lines via MarchingSquares
        var contours = MarchingSquares.Extract(series.XData, series.YData, series.ZData, levels);

        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        var labelFont = new Font { Size = series.LabelFontSize };
        string fmt = series.LabelFormat ?? "G4";

        foreach (var contour in contours)
        {
            if (contour.Points.Length < 2) continue;

            // Map level to color
            double t = (contour.Level - zMin) / (zMax - zMin);
            var color = series.ColorMap is not null ? cmap.GetColor(t) : Colors.Gray;

            // Convert data points to pixel points
            var pixelPoints = new List<Point>(contour.Points.Length);
            foreach (var pt in contour.Points)
                pixelPoints.Add(Transform.DataToPixel(pt.X, pt.Y));

            Ctx.DrawLines(pixelPoints, color, 1.5, LineStyle.Solid);

            // Label at midpoint of the polyline
            if (series.ShowLabels && pixelPoints.Count >= 2)
            {
                int mid = pixelPoints.Count / 2;
                var labelPt = pixelPoints[mid];
                string text = contour.Level.ToString(fmt);

                // White background rectangle for readability
                double approxW = text.Length * labelFont.Size * 0.6;
                double approxH = labelFont.Size * 1.4;
                Ctx.DrawRectangle(new Rect(labelPt.X - approxW / 2, labelPt.Y - approxH / 2, approxW, approxH),
                    Colors.White, null, 0);

                Ctx.DrawText(text, labelPt, labelFont, TextAlignment.Center);
            }
        }
    }
}
