// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="WireframeSeries"/> as grid lines projected from 3D to 2D.</summary>
internal sealed class WireframeSeriesRenderer : SeriesRenderer<WireframeSeries>
{
    /// <inheritdoc />
    public WireframeSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(WireframeSeries series)
    {
        int rows = series.Z.GetLength(0), cols = series.Z.GetLength(1);
        if (rows < 2 || cols < 2) return;

        var color = ResolveColor(series.Color);

        // Fitted to the values that are there: a missing value (NaN) and an infinity are left out.
        var proj = ProjectionFor(series.X.DrawableRange(), series.Y.DrawableRange(), series.Z.DrawableRange());

        // Draw grid lines along X direction (constant Y); a line breaks at a vertex that cannot be placed.
        for (int r = 0; r < rows; r++)
        {
            var points = new List<Point>(cols);
            for (int c = 0; c < cols; c++)
                points.Add(proj.Project(series.X[c], series.Y[r], series.Z[r, c]));
            DrawPolyline(points, new StrokeStyle(color, series.LineWidth, LineStyle.Solid));
        }

        // Draw grid lines along Y direction (constant X)
        for (int c = 0; c < cols; c++)
        {
            var points = new List<Point>(rows);
            for (int r = 0; r < rows; r++)
                points.Add(proj.Project(series.X[c], series.Y[r], series.Z[r, c]));
            DrawPolyline(points, new StrokeStyle(color, series.LineWidth, LineStyle.Solid));
        }
    }
}
