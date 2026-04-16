// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Line3DSeries"/> as depth-sorted projected polyline segments.</summary>
internal sealed class Line3DSeriesRenderer : SeriesRenderer<Line3DSeries>
{
    /// <inheritdoc />
    public Line3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Line3DSeries series)
    {
        if (series.X.Length < 2) return;

        var bounds = Area.PlotBounds;
        var color = ResolveColor(series.Color);

        double xMin = series.X.Min(), xMax = series.X.Max();
        double yMin = series.Y.Min(), yMax = series.Y.Max();
        double zMin = series.Z.Min(), zMax = series.Z.Max();

        var proj = Context.Projection3D
            ?? new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

        // Build segments with average depth for painter's algorithm sorting
        int segCount = series.X.Length - 1;
        var segments = new List<(double Depth, Point P1, Point P2)>(segCount);

        for (int i = 0; i < segCount; i++)
        {
            double x0 = series.X.Data[i], y0 = series.Y.Data[i], z0 = series.Z.Data[i];
            double x1 = series.X.Data[i + 1], y1 = series.Y.Data[i + 1], z1 = series.Z.Data[i + 1];

            double avgDepth = (proj.Depth(x0, y0, z0) + proj.Depth(x1, y1, z1)) / 2.0;
            segments.Add((avgDepth, proj.Project(x0, y0, z0), proj.Project(x1, y1, z1)));
        }

        // Sort back-to-front (painter's algorithm)
        segments.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        foreach (var (_, p1, p2) in segments)
            Ctx.DrawLine(p1, p2, color, series.LineWidth, series.LineStyle);
    }
}
