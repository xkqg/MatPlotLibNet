// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="WireframeSeries"/> as grid lines projected from 3D to 2D: one mark of the scene at its nearest point
/// (<see cref="WholeDrawing"/>), its lines drawn in grid order.</summary>
internal sealed class WireframeSeriesRenderer : SceneSeriesRenderer<WireframeSeries>
{
    /// <inheritdoc />
    public WireframeSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(WireframeSeries series)
    {
        int rows = series.Z.GetLength(0), cols = series.Z.GetLength(1);
        if (rows < 2 || cols < 2) return;

        var color = ColorOf(series);
        double width = Slot.LineWidthOf(series);

        // Fitted to the values that are there: a missing value (NaN) and an infinity are left out.
        var proj = ProjectionFor(series.X.DrawableRange(), series.Y.DrawableRange(), series.Z.DrawableRange());

        // The grid lines along X (constant Y), then along Y (constant X); a line breaks at a vertex that cannot be placed.
        var lines = new List<List<Point>>(rows + cols);
        double nearest = double.NegativeInfinity;
        for (int r = 0; r < rows; r++)
        {
            var points = new List<Point>(cols);
            for (int c = 0; c < cols; c++)
            {
                var at = proj.Project(series.X[c], series.Y[r], series.Z[r, c]);
                points.Add(at);
                if (at.IsDrawable()) nearest = Math.Max(nearest, proj.Depth(series.X[c], series.Y[r], series.Z[r, c]));
            }

            lines.Add(points);
        }

        for (int c = 0; c < cols; c++)
        {
            var points = new List<Point>(rows);
            for (int r = 0; r < rows; r++)
                points.Add(proj.Project(series.X[c], series.Y[r], series.Z[r, c]));
            lines.Add(points);
        }

        var stroke = StrokeStyle.Solid(color, width);
        DrawAsOneMark(nearest, () =>
        {
            foreach (var line in lines) DrawPolyline(line, stroke);
        });
    }
}
