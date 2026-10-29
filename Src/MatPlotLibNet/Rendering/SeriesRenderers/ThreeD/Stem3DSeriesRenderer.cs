// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Stem3DSeries"/> as projected vertical lines with markers at the data points.</summary>
internal sealed class Stem3DSeriesRenderer : SeriesRenderer<Stem3DSeries>
{
    /// <inheritdoc />
    public Stem3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    internal override void AddMarks(Stem3DSeries series, MarkObstacles.Builder marks) => AddMarksByDrawing(series, marks);

    /// <inheritdoc />
    public override void Render(Stem3DSeries series)
    {
        if (series.X.Length == 0) return;

        var color = ResolveColor(series.Color);

        // Fitted to the values that are there (a missing value, NaN, and an infinity are left out), from the floor at 0.
        var z = series.Z.Data.DrawableRange();
        var proj = ProjectionFor(series.X.Data.DrawableRange(), series.Y.Data.DrawableRange(),
            z is { } height ? new Range1D(Math.Min(0, height.Lo), height.Hi) : null);

        // Project every base (z=0) once — used by the stem lines AND the baseline polyline
        // so we only call Projection3D.Project once per point per layer.
        var basePts = new Point[series.X.Length];
        for (int i = 0; i < series.X.Length; i++)
        {
            double xi = series.X.Data[i];
            double yi = series.Y.Data[i];
            double zi = series.Z.Data[i];

            basePts[i] = proj.Project(xi, yi, 0);
            var tipPt = proj.Project(xi, yi, zi);

            // A stem with an end that cannot be placed is skipped with its head; its foot stays on the baseline.
            if (!basePts[i].CanDrawLineTo(tipPt)) continue;

            // Draw stem line at matplotlib's default 1.5 px width (rcParams.lines.linewidth)
            // so the blue pixel density matches the reference.
            Ctx.DrawLine(basePts[i], tipPt, new StrokeStyle(color, 2.5, Styling.LineStyle.Solid));

            // Draw marker at tip
            Ctx.DrawCircle(tipPt, series.MarkerSize / 2.0, new ShapeStyle(color, null, 0));
        }

        // Baseline polyline through all stem base points at z=0, matching matplotlib's
        // `ax.stem()` StemContainer.baseline Line3D. Drawn AFTER stems so it appears on
        // top of any overlap. Colour defaults to the stem colour; callers can override
        // via Stem3DSeries.BaseLineColor.
        // The baseline breaks where a foot cannot be placed, as a 2-D line does.
        var baselineColor = series.BaseLineColor ?? color;
        DrawPolyline(basePts, new StrokeStyle(baselineColor, 2.5, Styling.LineStyle.Solid));
    }
}
