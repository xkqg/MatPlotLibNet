// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Globalization;
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

        // Every line first, then every label: a line traced after a label would run across the label's box.
        var drawn = new List<DrawnContour>(contours.Length);
        foreach (var contour in contours)
        {
            if (contour.Points.Length < 2) continue;

            // Map level to color
            var color = series.ColorMap is not null ? cmap.ColorOf(contour.Level, LinearNormalizer.Instance, zMin, zMax) : Context.Theme.Neutral;

            // Convert data points to pixel points
            var pixelPoints = new List<Point>(contour.Points.Length);
            foreach (var pt in contour.Points)
                pixelPoints.Add(Transform.DataToPixel(pt.X, pt.Y));

            Ctx.DrawLines(pixelPoints, new StrokeStyle(color, 1.5, LineStyle.Solid));
            drawn.Add(new DrawnContour(contour.Level, pixelPoints, color));
        }

        if (series.ShowLabels)
        {
            foreach (var line in drawn) DrawLabel(series, line);
        }
    }

    /// <summary>A contour line as drawn: its level, its points in pixels and its colour.</summary>
    private readonly record struct DrawnContour(double Level, List<Point> Pixels, Color Color);

    /// <summary>Draws the level of <paramref name="line"/> on it, as matplotlib's <c>clabel</c> does: at the middle of the
    /// line, turned along it (<see cref="ContourLabelPlacement"/>), in the line's colour, on a box in the plot's background
    /// that covers the line under the text and <see cref="ContourLabelPlacement.InlineSpacing"/> on each side. A line too
    /// short for its label gets none.</summary>
    private void DrawLabel(ContourSeries series, DrawnContour line)
    {
        var font = new Font { Size = series.LabelFontSize, Color = line.Color };
        string text = line.Level.ToString(series.LabelFormat ?? "G4", CultureInfo.InvariantCulture);
        var size = Ctx.MeasureText(text, font);
        if (ContourLabelPlacement.Along(line.Pixels, size.Width) is not { } label) return;

        // The text is centred on the line: its baseline stands below the centre in the text's own frame, turned with it.
        double radians = label.Rotation * Math.PI / 180, drop = TextBox.CentreDrop * font.Size;
        var baseline = new Point(label.Centre.X + drop * Math.Sin(radians), label.Centre.Y + drop * Math.Cos(radians));
        var box = new TextBox(baseline, new Size(size.Width + 2 * ContourLabelPlacement.InlineSpacing, size.Height), font.Size,
            TextAlignment.Center, TextVerticalAlignment.Baseline, label.Rotation).Corners();

        Ctx.DrawPolygon([box.TopLeft, box.TopRight, box.BottomRight, box.BottomLeft], new ShapeStyle(Context.Theme.AxesBackground, null, 0));
        Ctx.DrawText(text, baseline, font, TextAlignment.Center, label.Rotation);
    }
}
