// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Stem3DSeries"/> as projected vertical lines with markers at the data points and a baseline: one mark of
/// the scene at its nearest point (<see cref="WholeDrawing"/>).</summary>
internal sealed class Stem3DSeriesRenderer : SceneSeriesRenderer<Stem3DSeries>
{
    /// <inheritdoc />
    public Stem3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(Stem3DSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(Stem3DSeries series)
    {
        if (series.X.Length == 0) return;

        var color = ColorOf(series);
        var stemStroke = Slot.StrokeOf(color, LineWidthRule.WideCollection);

        // Fitted to the values that are there (a missing value, NaN, and an infinity are left out), from the floor at 0.
        var z = series.Z.Data.DrawableRange();
        var proj = ProjectionFor(series.X.Data.DrawableRange(), series.Y.Data.DrawableRange(),
            z is { } height ? new Range1D(Math.Min(0, height.Lo), height.Hi) : null);

        // Project every base (z=0) once — used by the stem lines AND the baseline polyline
        // so we only call Projection3D.Project once per point per layer. A stem with an end that cannot be placed is skipped with its
        // head; its foot stays on the baseline. The stems and the baseline are one mark of the scene, at their nearest point.
        var basePts = new Point[series.X.Length];
        var stems = new List<PixelSegment>(series.X.Length);
        double nearest = double.NegativeInfinity;
        for (int i = 0; i < series.X.Length; i++)
        {
            double xi = series.X.Data[i];
            double yi = series.Y.Data[i];
            double zi = series.Z.Data[i];

            basePts[i] = proj.Project(xi, yi, 0);
            var tipPt = proj.Project(xi, yi, zi);
            if (basePts[i].IsDrawable()) nearest = Math.Max(nearest, proj.Depth(xi, yi, 0));
            if (!basePts[i].CanDrawLineTo(tipPt)) continue;

            stems.Add(new(basePts[i], tipPt));
            nearest = Math.Max(nearest, proj.Depth(xi, yi, zi));
        }

        // Baseline polyline through all stem base points at z=0, matching matplotlib's
        // `ax.stem()` StemContainer.baseline Line3D. Drawn AFTER stems so it appears on
        // top of any overlap. Colour defaults to the stem colour; callers can override
        // via Stem3DSeries.BaseLineColor.
        // The baseline breaks where a foot cannot be placed, as a 2-D line does.
        var baseline = Slot.StrokeOf(series.BaseLineColor ?? color, LineWidthRule.WideLine);
        var head = ShapeStyle.Filled(color);
        double headRadius = series.MarkerSize.PointsInPixels / 2.0;
        DrawAsOneMark(nearest, () =>
        {
            // The stem line from its foot to its tip, at the width of its kind (lines.linewidth under the matplotlib themes, 2.5 pixels
            // under the library's own), with a marker at its tip.
            foreach (var stem in stems)
            {
                Ctx.DrawLine(stem.From, stem.To, stemStroke);
                Ctx.DrawCircle(stem.To, headRadius, head);
            }

            DrawPolyline(basePts, baseline);
        });
    }
}
