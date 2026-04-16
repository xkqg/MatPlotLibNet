// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="VoxelSeries"/> as depth-sorted cube faces projected from 3D to 2D.</summary>
internal sealed class VoxelSeriesRenderer : SeriesRenderer<VoxelSeries>
{
    /// <inheritdoc />
    public VoxelSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(VoxelSeries series)
    {
        int xDim = series.Filled.GetLength(0);
        int yDim = series.Filled.GetLength(1);
        int zDim = series.Filled.GetLength(2);
        if (xDim == 0 || yDim == 0 || zDim == 0) return;

        var bounds = Area.PlotBounds;
        var baseColor = ResolveColor(series.Color);
        var strokeColor = Colors.Black.WithAlpha(80);

        var proj = Context.Projection3D
            ?? new Projection3D(30, -60, bounds, 0, xDim, 0, yDim, 0, zDim);

        // Per-face shading for visual depth
        var topColor = baseColor;
        var frontColor = baseColor.WithAlpha((byte)(baseColor.A * 0.85));
        var sideColor = baseColor.WithAlpha((byte)(baseColor.A * 0.70));

        // Build faces for all filled voxels
        var faces = new List<(double Depth, Point[] Vertices, Color Fill)>();

        for (int x = 0; x < xDim; x++)
            for (int y = 0; y < yDim; y++)
                for (int z = 0; z < zDim; z++)
                {
                    if (!series.Filled[x, y, z]) continue;

                    double x0 = x, x1 = x + 1;
                    double y0 = y, y1 = y + 1;
                    double z0 = z, z1 = z + 1;

                    // Top face
                    AddFace(faces, proj, topColor,
                        (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1));
                    // Bottom face
                    AddFace(faces, proj, sideColor,
                        (x0, y0, z0), (x0, y1, z0), (x1, y1, z0), (x1, y0, z0));
                    // Front face (Y = y0)
                    AddFace(faces, proj, frontColor,
                        (x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1));
                    // Back face (Y = y1)
                    AddFace(faces, proj, frontColor,
                        (x1, y1, z0), (x0, y1, z0), (x0, y1, z1), (x1, y1, z1));
                    // Left face (X = x0)
                    AddFace(faces, proj, sideColor,
                        (x0, y1, z0), (x0, y0, z0), (x0, y0, z1), (x0, y1, z1));
                    // Right face (X = x1)
                    AddFace(faces, proj, sideColor,
                        (x1, y0, z0), (x1, y1, z0), (x1, y1, z1), (x1, y0, z1));
                }

        Ctx.SetOpacity(series.Alpha);

        // Use shared depth queue if available for cross-series compositing
        if (Context.DepthQueue is { } queue)
        {
            foreach (var (depth, verts, fill) in faces)
            {
                var vLocal = verts; var fLocal = fill; var sLocal = strokeColor;
                queue.Add(depth, () => Ctx.DrawPolygon(vLocal, fLocal, sLocal, 0.5));
            }
        }
        else
        {
            // Sort back-to-front (painter's algorithm)
            faces.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            foreach (var (_, verts, fill) in faces)
                Ctx.DrawPolygon(verts, fill, strokeColor, 0.5);
        }

        Ctx.SetOpacity(1.0);
    }

    private static void AddFace(
        List<(double Depth, Point[] Vertices, Color Fill)> sink,
        Projection3D proj, Color fill,
        (double x, double y, double z) c0,
        (double x, double y, double z) c1,
        (double x, double y, double z) c2,
        (double x, double y, double z) c3)
    {
        var pts = new[]
        {
            proj.Project(c0.x, c0.y, c0.z),
            proj.Project(c1.x, c1.y, c1.z),
            proj.Project(c2.x, c2.y, c2.z),
            proj.Project(c3.x, c3.y, c3.z),
        };
        double cx = (c0.x + c1.x + c2.x + c3.x) / 4.0;
        double cy = (c0.y + c1.y + c2.y + c3.y) / 4.0;
        double cz = (c0.z + c1.z + c2.z + c3.z) / 4.0;
        sink.Add((proj.Depth(cx, cy, cz), pts, fill));
    }
}
