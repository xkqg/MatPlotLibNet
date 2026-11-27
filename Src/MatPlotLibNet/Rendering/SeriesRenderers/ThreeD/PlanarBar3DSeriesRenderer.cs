// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>
/// Renders a <see cref="PlanarBar3DSeries"/> — flat, translucent rectangles placed in the
/// XZ plane at <c>y = Y[i]</c>. Unlike <see cref="Bar3DSeriesRenderer"/>, which draws each bar as a box whose six faces are
/// shaded by the light, this renderer draws a single polygon per bar and leaves shading to the caller's
/// chosen <see cref="PlanarBar3DSeries.Alpha"/> and <see cref="PlanarBar3DSeries.Color"/>. The rectangles are drawn back to front
/// with the scene, so the bars of several planes composite correctly whatever order they were added in.
/// </summary>
internal sealed class PlanarBar3DSeriesRenderer : SceneSeriesRenderer<PlanarBar3DSeries>
{
    /// <inheritdoc />
    public PlanarBar3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(PlanarBar3DSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(PlanarBar3DSeries series)
    {
        if (series.X.Length == 0) return;

        // Fitted to the values that are there (a missing value, NaN, and an infinity are left out), from the floor at 0.
        var x = series.X.Data.DrawableRange();
        var proj = ProjectionFor(x is { } bars ? new Range1D(bars.Lo, bars.Hi + series.BarWidth) : null,
            series.Y.Data.DrawableRange(), new Range1D(0, series.Z.Data.DrawableRange()?.Hi ?? 1));

        var baseColor = ColorOf(series);

        var faces = new List<FilledFace>(series.X.Length);
        for (int i = 0; i < series.X.Length; i++)
        {
            double x0 = series.X.Data[i];
            double x1 = x0 + series.BarWidth;
            double y  = series.Y.Data[i];
            double h  = series.Z.Data[i];

            // Four corners of the rectangle in the XZ plane at y = const, CCW from viewer.
            var pts = new[]
            {
                proj.Project(x0, y, 0d),
                proj.Project(x1, y, 0d),
                proj.Project(x1, y, h),
                proj.Project(x0, y, h),
            };

            // A bar with a corner that cannot be placed is skipped.
            if (!pts.IsDrawable()) continue;

            // Per-bar fill colour: Colors[i] > Color > the slot's colour, then the alpha; at the depth of its centre.
            var picked = series.Colors.ElementOrNull(i) ?? baseColor;
            faces.Add(new(proj.Depth((x0 + x1) / 2, y, h / 2), pts, picked.WithOpacityGiven(series.Alpha)));
        }

        // Every bar edged as the theme decides for a patch (the colour given, else black at 1 pt under classic and none under v2), the
        // bar's own hairline under the library's themes.
        var theme = Context.Theme;
        var edges = theme.PatchEdgesOf(series);
        var edge = edges.Of();
        DrawBackToFront(faces, face => Ctx.DrawPolygon(face.Vertices, edges.StyleOf(face.Fill, edge)));
    }
}
