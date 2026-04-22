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
        var faces = new List<DepthFace>();

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
                        new(x0, y0, z1), new(x1, y0, z1), new(x1, y1, z1), new(x0, y1, z1));
                    // Bottom face
                    AddFace(faces, proj, sideColor,
                        new(x0, y0, z0), new(x0, y1, z0), new(x1, y1, z0), new(x1, y0, z0));
                    // Front face (Y = y0)
                    AddFace(faces, proj, frontColor,
                        new(x0, y0, z0), new(x1, y0, z0), new(x1, y0, z1), new(x0, y0, z1));
                    // Back face (Y = y1)
                    AddFace(faces, proj, frontColor,
                        new(x1, y1, z0), new(x0, y1, z0), new(x0, y1, z1), new(x1, y1, z1));
                    // Left face (X = x0)
                    AddFace(faces, proj, sideColor,
                        new(x0, y1, z0), new(x0, y0, z0), new(x0, y0, z1), new(x0, y1, z1));
                    // Right face (X = x1)
                    AddFace(faces, proj, sideColor,
                        new(x1, y0, z0), new(x1, y1, z0), new(x1, y1, z1), new(x1, y0, z1));
                }

        Ctx.SetOpacity(series.Alpha);

        // Use shared depth queue if available for cross-series compositing
        if (Context.DepthQueue is { } queue)
        {
            foreach (var face in faces)
            {
                var vLocal = face.Vertices; var fLocal = face.Fill; var sLocal = strokeColor;
                queue.Add(face.Depth, () => Ctx.DrawPolygon(vLocal, fLocal, sLocal, 0.5));
            }
        }
        else
        {
            // Sort back-to-front (painter's algorithm)
            faces.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            foreach (var face in faces)
                Ctx.DrawPolygon(face.Vertices, face.Fill, strokeColor, 0.5);
        }

        Ctx.SetOpacity(1.0);
    }

    private static void AddFace(
        List<DepthFace> sink,
        Projection3D proj, Color fill,
        Lighting.Vec3 c0, Lighting.Vec3 c1, Lighting.Vec3 c2, Lighting.Vec3 c3)
    {
        var pts = new[]
        {
            proj.Project(c0.X, c0.Y, c0.Z),
            proj.Project(c1.X, c1.Y, c1.Z),
            proj.Project(c2.X, c2.Y, c2.Z),
            proj.Project(c3.X, c3.Y, c3.Z),
        };
        double cx = (c0.X + c1.X + c2.X + c3.X) / 4.0;
        double cy = (c0.Y + c1.Y + c2.Y + c3.Y) / 4.0;
        double cz = (c0.Z + c1.Z + c2.Z + c3.Z) / 4.0;
        sink.Add(new(proj.Depth(cx, cy, cz), pts, fill));
    }

    private readonly record struct DepthFace(double Depth, Point[] Vertices, Color Fill);
}
