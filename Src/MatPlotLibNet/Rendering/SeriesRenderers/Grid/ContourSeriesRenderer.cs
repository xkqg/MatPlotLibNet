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

        // Compute the Z range, over the values that are there: a missing value (NaN) and an infinity are left out, and
        // the cells they are a corner of are masked when the lines are traced.
        if (series.ZData.DrawableRange() is not { } z || z.Lo >= z.Hi) return;
        double zMin = z.Lo, zMax = z.Hi;

        // The levels the user named, or else evenly spaced ones; a named level outside the range crosses no cell.
        double[] levels = series.LevelValues.ExplicitLevels()
            ?? ContourLevels.Interior(zMin, zMax, Math.Max(2, series.Levels));

        // Extract iso-lines via MarchingSquares
        var contours = MarchingSquares.Extract(series.XData, series.YData, series.ZData, levels);

        var cmap = series.GetColorMapOrDefault(ColorMaps.Viridis);

        // A filled contour fills the bands between the levels the lines are traced at, from the lowest value to the highest,
        // through the one fill a Contourf uses (so the fill stands where the grid stands and its edges are the lines).
        if (series.Filled)
        {
            double[] thresholds = [zMin, .. levels.Where(level => level > zMin && level < zMax), zMax];
            Context.FillContourBands(series.XData, series.YData, series.ZData,
                MarchingSquares.ExtractBands(series.XData, series.YData, series.ZData, thresholds), cmap, LinearNormalizer.Instance);
        }

        var labelFont = new Font { Size = series.LabelFontSize };
        string fmt = series.LabelFormat ?? "G4";

        foreach (var contour in contours)
        {
            if (contour.Points.Length < 2) continue;

            // Map level to color
            var color = series.ColorMap is not null ? cmap.ColorOf(contour.Level, LinearNormalizer.Instance, zMin, zMax) : Colors.Gray;

            // Convert data points to pixel points
            var pixelPoints = new List<Point>(contour.Points.Length);
            foreach (var pt in contour.Points)
                pixelPoints.Add(Transform.DataToPixel(pt.X, pt.Y));

            Ctx.DrawLines(pixelPoints, new StrokeStyle(color, 1.5, LineStyle.Solid));

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
                    new ShapeStyle(Colors.White, null, 0));

                Ctx.DrawText(text, labelPt, labelFont, TextAlignment.Center);
            }
        }
    }
}
