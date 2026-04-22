// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Bar3DSeries"/> as depth-sorted rectangular prisms projected from 3D to 2D.</summary>
internal sealed class Bar3DSeriesRenderer : SeriesRenderer<Bar3DSeries>
{
    private enum FaceKind { Top, Bottom, FrontY, BackY, LeftX, RightX }

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

        var proj = Context.Projection3D
            ?? new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);
        double hw = series.BarWidth / 2.0;

        // matplotlib draws 3-D bar edges in solid black so prism geometry pops against the
        // fill colour. Match that.
        var strokeColor = Colors.Black;

        // Per-face fill colours. We want to match matplotlib's bar3d shading exactly
        // (art3d._shade_colors), which is NOT Lambertian — it maps the raw signed dot
        // product to [0.3, 1.0]. Two colours are enough because opposite faces share a
        // shade value (matplotlib draws all 6, same pattern).
        Color topColor, bottomColor;
        Color frontColor, backColor;
        Color leftColor, rightColor;
        if (Context.LightSource is DirectionalLight dl)
        {
            double lx = dl.Dx, ly = dl.Dy, lz = dl.Dz;
            topColor    = baseColor.Shade( 0,  0,  1, lx, ly, lz);
            bottomColor = baseColor.Shade( 0,  0, -1, lx, ly, lz);
            frontColor  = baseColor.Shade( 0, -1,  0, lx, ly, lz);
            backColor   = baseColor.Shade( 0,  1,  0, lx, ly, lz);
            leftColor   = baseColor.Shade(-1,  0,  0, lx, ly, lz);
            rightColor  = baseColor.Shade( 1,  0,  0, lx, ly, lz);
        }
        else
        {
            topColor    = baseColor;
            bottomColor = baseColor.WithAlpha((byte)(baseColor.A * 0.60));
            frontColor  = baseColor.WithAlpha((byte)(baseColor.A * 0.85));
            backColor   = frontColor;
            leftColor   = baseColor.WithAlpha((byte)(baseColor.A * 0.70));
            rightColor  = leftColor;
        }

        // Build every face of every bar and sort them individually by centroid depth.
        // Drawing all 6 faces per prism and letting per-face depth sorting handle occlusion
        // is robust to any camera angle — we don't have to hard-code which 3 faces are
        // front-facing for the current rotation.
        var faces = new List<DepthFace>(series.X.Length * 6);

        for (int i = 0; i < series.X.Length; i++)
        {
            // matplotlib bar3d convention: X[i] / Y[i] are the LEFT-FRONT corner of the
            // bar, not the centre. Bar spans [X, X+BarWidth] × [Y, Y+BarWidth] × [0, Z].
            // This makes X/Y tick labels line up with the bar edges instead of centres.
            double cx = series.X.Data[i];
            double cy = series.Y.Data[i];
            double h  = series.Z.Data[i];

            double x0 = cx, x1 = cx + series.BarWidth;
            double y0 = cy, y1 = cy + series.BarWidth;

            AddFace(faces, proj, topColor,
                new(x0, y0, h), new(x1, y0, h), new(x1, y1, h), new(x0, y1, h));
            AddFace(faces, proj, bottomColor,
                new(x0, y0, 0d), new(x0, y1, 0d), new(x1, y1, 0d), new(x1, y0, 0d));
            AddFace(faces, proj, frontColor,
                new(x0, y0, 0d), new(x1, y0, 0d), new(x1, y0, h), new(x0, y0, h));
            AddFace(faces, proj, backColor,
                new(x1, y1, 0d), new(x0, y1, 0d), new(x0, y1, h), new(x1, y1, h));
            AddFace(faces, proj, leftColor,
                new(x0, y1, 0d), new(x0, y0, 0d), new(x0, y0, h), new(x0, y1, h));
            AddFace(faces, proj, rightColor,
                new(x1, y0, 0d), new(x1, y1, 0d), new(x1, y1, h), new(x1, y0, h));
        }

        // If a shared cross-series depth queue is available on the context, push each
        // face as a deferred draw so the axes renderer can sort across ALL 3-D series
        // (including other Bar3D rows) before painting. Otherwise fall back to the
        // local back-to-front sort — correct for a single-series render.
        if (Context.DepthQueue is { } queue)
        {
            foreach (var face in faces)
            {
                // Capture-by-value — avoid foreach-variable aliasing in the closure.
                var vLocal = face.Vertices; var fLocal = face.Fill; var sLocal = strokeColor;
                queue.Add(face.Depth, () => Ctx.DrawPolygon(vLocal, fLocal, sLocal, 0.5));
            }
        }
        else
        {
            faces.Sort((a, b) => a.Depth.CompareTo(b.Depth));
            foreach (var face in faces)
                Ctx.DrawPolygon(face.Vertices, face.Fill, strokeColor, 0.5);
        }
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
