// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Bar3DSeries"/> as boxes (<see cref="Cuboid"/>) projected from 3D to 2D, their faces drawn back to
/// front with the scene.</summary>
internal sealed class Bar3DSeriesRenderer : SceneSeriesRenderer<Bar3DSeries>
{
    /// <inheritdoc />
    public Bar3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(Bar3DSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(Bar3DSeries series)
    {
        if (series.X.Length == 0) return;

        // Fitted to the values that are there (a missing value, NaN, and an infinity are left out), from the floor at 0.
        var proj = ProjectionFor(series.X.Data.DrawableRange(), series.Y.Data.DrawableRange(),
            new Range1D(0, series.Z.Data.DrawableRange()?.Hi ?? 1));

        // Every face of a bar is shaded by the light it is lit by, on its own outward normal, as matplotlib's bar3d shades it
        // (art3d._shade_colors maps the signed dot product to a band of intensities rather than clamping it at zero).
        var fills = Cuboid.ShadesOf(ColorOf(series), Context.LightSource);

        // All six faces of every bar, each sorted by its own centre's depth with the scene: robust to any camera angle, with no
        // need to know which three faces face the camera. A bar is one mark: one with a corner that cannot be placed is skipped
        // whole, every face with it.
        var faces = new List<FilledFace>(series.X.Length * Cuboid.FaceCount);
        for (int i = 0; i < series.X.Length; i++)
        {
            // matplotlib bar3d convention: X[i] / Y[i] are the LEFT-FRONT corner of the bar, not the centre, so the bar spans
            // [X, X + BarWidth] x [Y, Y + BarWidth] x [0, Z] and the tick labels line up with the bar edges.
            double x = series.X.Data[i], y = series.Y.Data[i];
            new Cuboid(x, x + series.BarWidth, y, y + series.BarWidth, 0, series.Z.Data[i]).AddFacesTo(faces, proj, fills);
        }

        // Every face edged as the theme decides for a patch (black at 1 pt under classic, none under v2), the bar's own black hairline
        // under the library's themes.
        var theme = Context.Theme;
        var edges = theme.PatchEdgesOf(series);
        var edge = edges.Of();
        DrawBackToFront(faces, face => Ctx.DrawPolygon(face.Vertices, edges.StyleOf(face.Fill, edge)));
    }
}
