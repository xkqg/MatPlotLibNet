// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>
/// Renders a <see cref="PlanarBar3DSeries"/> — flat, translucent rectangles placed in the
/// XZ plane at <c>y = Y[i]</c>. Unlike <see cref="Bar3DSeriesRenderer"/> which builds six
/// cuboid faces per bar and shades them with matplotlib's <c>_shade_colors</c> formula,
/// this renderer produces a single polygon per bar and leaves shading to the caller's
/// chosen <see cref="PlanarBar3DSeries.Alpha"/> and <see cref="PlanarBar3DSeries.Color"/>.
/// When a shared <see cref="DepthQueue3D"/> is present on the context, face draws are
/// queued so rectangles from multiple <c>PlanarBar3D</c> series composite correctly
/// across planes regardless of insertion order.
/// </summary>
internal sealed class PlanarBar3DSeriesRenderer : SeriesRenderer<PlanarBar3DSeries>
{
    /// <inheritdoc />
    public PlanarBar3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(PlanarBar3DSeries series)
    {
        if (series.X.Length == 0) return;

        var bounds = Area.PlotBounds;

        double xMin = series.X.Min(), xMax = series.X.Max() + series.BarWidth;
        double yMin = series.Y.Min(), yMax = series.Y.Max();
        double zMin = 0;
        double zMax = series.Z.Max();

        var proj = Context.Projection3D
            ?? new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

        var baseColor = ResolveColor(series.Color);
        var edgeColor = series.EdgeColor ?? Colors.Black;
        byte alpha    = (byte)Math.Clamp(255 * series.Alpha, 0, 255);
        var queue     = Context.DepthQueue;

        for (int i = 0; i < series.X.Length; i++)
        {
            double x0 = series.X.Data[i];
            double x1 = x0 + series.BarWidth;
            double y  = series.Y.Data[i];
            double h  = series.Z.Data[i];

            // Per-bar fill colour resolution: Colors[i] > Color > baseColor, then apply Alpha.
            Color pickedColor = (series.Colors is { } cs && i < cs.Length) ? cs[i] : baseColor;
            Color fill = pickedColor.WithAlpha(alpha);

            // Four corners of the rectangle in the XZ plane at y = const, CCW from viewer.
            var pts = new[]
            {
                proj.Project(x0, y, 0d),
                proj.Project(x1, y, 0d),
                proj.Project(x1, y, h),
                proj.Project(x0, y, h),
            };

            // Depth key: centroid of the rectangle.
            double depth = proj.Depth((x0 + x1) / 2, y, h / 2);

            if (queue is not null)
            {
                // Capture-by-value so the foreach variables don't alias when the closure
                // is invoked later during DepthQueue3D.Flush().
                var pLocal = pts;
                var fLocal = fill;
                var eLocal = edgeColor;
                queue.Add(depth, () => Ctx.DrawPolygon(pLocal, fLocal, eLocal, 0.5));
            }
            else
            {
                Ctx.DrawPolygon(pts, fill, edgeColor, 0.5);
            }
        }
    }
}
