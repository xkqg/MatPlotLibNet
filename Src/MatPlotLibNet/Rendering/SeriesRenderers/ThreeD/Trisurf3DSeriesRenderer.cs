// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Trisurf3DSeries"/> as depth-sorted filled triangles projected from 3D to 2D.</summary>
internal sealed class Trisurf3DSeriesRenderer : SeriesRenderer<Trisurf3DSeries>
{
    /// <inheritdoc />
    public Trisurf3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Trisurf3DSeries series)
    {
        if (series.X.Length < 3) return;

        var bounds = Area.PlotBounds;
        var baseColor = ResolveColor(series.Color);

        double xMin = series.X.Min(), xMax = series.X.Max();
        double yMin = series.Y.Min(), yMax = series.Y.Max();
        double zMin = series.Z.Min(), zMax = series.Z.Max();
        double zRange = zMax - zMin;
        if (zRange == 0) zRange = 1;

        var proj = Context.Projection3D
            ?? new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

        var cmap = series.ColorMap;
        var normalizer = series.Normalizer ?? LinearNormalizer.Instance;
        bool useColorMap = cmap is not null;

        // Generate triangles using simple sequential triplets. A full Delaunay triangulation
        // would be more general but this approach produces valid geometry for regularly spaced
        // data and keeps the renderer simple.
        var triangles = new List<DepthTriangle>();

        // If the point count is divisible by 3, use sequential triplets;
        // otherwise, use a fan triangulation from point 0.
        int n = series.X.Length;
        if (n % 3 == 0)
        {
            for (int i = 0; i < n - 2; i += 3)
                AddTriangle(triangles, proj, series, i, i + 1, i + 2);
        }
        else
        {
            for (int i = 1; i < n - 1; i++)
                AddTriangle(triangles, proj, series, 0, i, i + 1);
        }

        // Sort back-to-front (painter's algorithm)
        triangles.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        Ctx.SetOpacity(series.Alpha);

        foreach (var tri in triangles)
        {
            var fill = useColorMap
                ? cmap!.GetColor(normalizer.Normalize(tri.AvgZ, zMin, zMax))
                : baseColor;

            Color? stroke = series.ShowWireframe
                ? (series.EdgeColor ?? Colors.Black.WithAlpha(80))
                : null;
            double strokeWidth = series.ShowWireframe ? 0.5 : 0;

            Ctx.DrawPolygon(tri.Vertices, fill, stroke, strokeWidth);
        }

        Ctx.SetOpacity(1.0);
    }

    private static void AddTriangle(
        List<DepthTriangle> sink,
        Projection3D proj, Trisurf3DSeries series,
        int i0, int i1, int i2)
    {
        double x0 = series.X.Data[i0], y0 = series.Y.Data[i0], z0 = series.Z.Data[i0];
        double x1 = series.X.Data[i1], y1 = series.Y.Data[i1], z1 = series.Z.Data[i1];
        double x2 = series.X.Data[i2], y2 = series.Y.Data[i2], z2 = series.Z.Data[i2];

        double avgZ = (z0 + z1 + z2) / 3.0;
        double cx = (x0 + x1 + x2) / 3.0;
        double cy = (y0 + y1 + y2) / 3.0;
        double cz = avgZ;

        var vertices = new[]
        {
            proj.Project(x0, y0, z0),
            proj.Project(x1, y1, z1),
            proj.Project(x2, y2, z2),
        };

        sink.Add(new(proj.Depth(cx, cy, cz), vertices, avgZ));
    }

    private readonly record struct DepthTriangle(double Depth, Point[] Vertices, double AvgZ);
}
