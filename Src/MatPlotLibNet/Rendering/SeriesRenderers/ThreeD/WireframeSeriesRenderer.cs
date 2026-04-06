// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="WireframeSeries"/> as grid lines projected from 3D to 2D.</summary>
internal sealed class WireframeSeriesRenderer : SeriesRenderer<WireframeSeries>
{
    public WireframeSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(WireframeSeries series)
    {
        int rows = series.Z.GetLength(0), cols = series.Z.GetLength(1);
        if (rows < 2 || cols < 2) return;

        var bounds = Area.PlotBounds;
        var color = ResolveColor(series.Color);

        double xMin = series.X.Min(), xMax = series.X.Max();
        double yMin = series.Y.Min(), yMax = series.Y.Max();
        double zMin = double.MaxValue, zMax = double.MinValue;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (series.Z[r, c] < zMin) zMin = series.Z[r, c];
                if (series.Z[r, c] > zMax) zMax = series.Z[r, c];
            }

        var proj = new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

        // Draw grid lines along X direction (constant Y)
        for (int r = 0; r < rows; r++)
        {
            var points = new List<Point>(cols);
            for (int c = 0; c < cols; c++)
                points.Add(proj.Project(series.X[c], series.Y[r], series.Z[r, c]));
            Ctx.DrawLines(points, color, series.LineWidth, LineStyle.Solid);
        }

        // Draw grid lines along Y direction (constant X)
        for (int c = 0; c < cols; c++)
        {
            var points = new List<Point>(rows);
            for (int r = 0; r < rows; r++)
                points.Add(proj.Project(series.X[c], series.Y[r], series.Z[r, c]));
            Ctx.DrawLines(points, color, series.LineWidth, LineStyle.Solid);
        }
    }
}
