// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Bar3DSeries"/> as depth-sorted rectangular prisms projected from 3D to 2D.</summary>
internal sealed class Bar3DSeriesRenderer : SeriesRenderer<Bar3DSeries>
{
    /// <inheritdoc />
    public Bar3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Bar3DSeries series)
    {
        if (series.X.Length == 0) return;

        var bounds = Area.PlotBounds;
        var baseColor = ResolveColor(series.Color);

        double xMin = series.X.Min(), xMax = series.X.Max();
        double yMin = series.Y.Min(), yMax = series.Y.Max();
        double zMin = 0;
        double zMax = series.Z.Max();

        var proj = new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);
        double hw = series.BarWidth / 2.0;

        // Build bars with depth for painter's algorithm
        var bars = new List<(double Depth, Point[] Top, Point[] Front, Point[] Side)>(series.X.Length);

        for (int i = 0; i < series.X.Length; i++)
        {
            double cx = series.X.Data[i];
            double cy = series.Y.Data[i];
            double h = series.Z.Data[i];

            // 8 corners of the prism
            double x0 = cx - hw, x1 = cx + hw;
            double y0 = cy - hw, y1 = cy + hw;

            // Top face (z = h)
            var top = new[]
            {
                proj.Project(x0, y0, h),
                proj.Project(x1, y0, h),
                proj.Project(x1, y1, h),
                proj.Project(x0, y1, h)
            };

            // Front face (y = y0, z from 0 to h)
            var front = new[]
            {
                proj.Project(x0, y0, 0),
                proj.Project(x1, y0, 0),
                proj.Project(x1, y0, h),
                proj.Project(x0, y0, h)
            };

            // Right face (x = x1, z from 0 to h)
            var side = new[]
            {
                proj.Project(x1, y0, 0),
                proj.Project(x1, y1, 0),
                proj.Project(x1, y1, h),
                proj.Project(x1, y0, h)
            };

            double depth = proj.Depth(cx, cy, h / 2.0);
            bars.Add((depth, top, front, side));
        }

        // Sort back-to-front (painter's algorithm)
        bars.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        var strokeColor = baseColor.WithAlpha(80);

        foreach (var (_, top, front, side) in bars)
        {
            // Shade faces slightly
            var topColor = baseColor;
            var frontColor = baseColor.WithAlpha((byte)(baseColor.A * 0.85));
            var sideColor = baseColor.WithAlpha((byte)(baseColor.A * 0.70));

            Ctx.DrawPolygon(top, topColor, strokeColor, 0.5);
            Ctx.DrawPolygon(front, frontColor, strokeColor, 0.5);
            Ctx.DrawPolygon(side, sideColor, strokeColor, 0.5);
        }
    }
}
