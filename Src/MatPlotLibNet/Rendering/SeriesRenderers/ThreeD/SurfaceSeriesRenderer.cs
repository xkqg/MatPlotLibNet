// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SurfaceSeries"/> as colored quadrilaterals projected from 3D to 2D.</summary>
internal sealed class SurfaceSeriesRenderer : SeriesRenderer<SurfaceSeries>
{
    public SurfaceSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(SurfaceSeries series)
    {
        int rows = series.Z.GetLength(0), cols = series.Z.GetLength(1);
        if (rows < 2 || cols < 2) return;

        var bounds = Area.PlotBounds;

        // Compute Z range for color mapping
        double zMin = double.MaxValue, zMax = double.MinValue;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (series.Z[r, c] < zMin) zMin = series.Z[r, c];
                if (series.Z[r, c] > zMax) zMax = series.Z[r, c];
            }
        double zRange = zMax - zMin;
        if (zRange == 0) zRange = 1;

        double xMin = series.X.Min(), xMax = series.X.Max();
        double yMin = series.Y.Min(), yMax = series.Y.Max();

        var proj = new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);
        var cmap = series.ColorMap ?? ColorMaps.Viridis;

        // Build quads with average depth for painter's algorithm sorting
        var quads = new List<(double Depth, Point[] Vertices, double AvgZ)>((rows - 1) * (cols - 1));
        for (int r = 0; r < rows - 1; r++)
            for (int c = 0; c < cols - 1; c++)
            {
                double x0 = series.X[c], x1 = series.X[c + 1];
                double y0 = series.Y[r], y1 = series.Y[r + 1];
                double z00 = series.Z[r, c], z10 = series.Z[r + 1, c];
                double z01 = series.Z[r, c + 1], z11 = series.Z[r + 1, c + 1];

                double avgZ = (z00 + z10 + z01 + z11) / 4.0;
                double avgDepth = (proj.Depth(x0, y0, z00) + proj.Depth(x1, y0, z01)
                    + proj.Depth(x0, y1, z10) + proj.Depth(x1, y1, z11)) / 4.0;

                var vertices = new[]
                {
                    proj.Project(x0, y0, z00),
                    proj.Project(x1, y0, z01),
                    proj.Project(x1, y1, z11),
                    proj.Project(x0, y1, z10)
                };

                quads.Add((avgDepth, vertices, avgZ));
            }

        // Sort back-to-front (painter's algorithm)
        quads.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        Ctx.SetOpacity(series.Alpha);

        foreach (var (_, vertices, avgZ) in quads)
        {
            var color = cmap.GetColor((avgZ - zMin) / zRange);
            Color? stroke = series.ShowWireframe ? Colors.Black.WithAlpha(80) : null;
            double strokeWidth = series.ShowWireframe ? 0.5 : 0;
            Ctx.DrawPolygon(vertices, color, stroke, strokeWidth);
        }

        Ctx.SetOpacity(1.0);
    }
}
