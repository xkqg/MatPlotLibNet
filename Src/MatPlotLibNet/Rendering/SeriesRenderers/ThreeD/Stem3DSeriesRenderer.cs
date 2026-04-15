// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Stem3DSeries"/> as projected vertical lines with markers at the data points.</summary>
internal sealed class Stem3DSeriesRenderer : SeriesRenderer<Stem3DSeries>
{
    /// <inheritdoc />
    public Stem3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Stem3DSeries series)
    {
        if (series.X.Length == 0) return;

        var bounds = Area.PlotBounds;
        var color = ResolveColor(series.Color);

        double xMin = series.X.Min(), xMax = series.X.Max();
        double yMin = series.Y.Min(), yMax = series.Y.Max();
        double zMin = Math.Min(0, series.Z.Min());
        double zMax = series.Z.Max();

        var proj = Context.Projection3D
            ?? new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

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

            // Draw stem line at matplotlib's default 1.5 px width (rcParams.lines.linewidth)
            // so the blue pixel density matches the reference.
            Ctx.DrawLine(basePts[i], tipPt, color, 2.5, Styling.LineStyle.Solid);

            // Draw marker at tip
            Ctx.DrawCircle(tipPt, series.MarkerSize / 2.0, color, null, 0);
        }

        // Baseline polyline through all stem base points at z=0, matching matplotlib's
        // `ax.stem()` StemContainer.baseline Line3D. Drawn AFTER stems so it appears on
        // top of any overlap. Colour defaults to the stem colour; callers can override
        // via Stem3DSeries.BaseLineColor.
        var baselineColor = series.BaseLineColor ?? color;
        if (basePts.Length >= 2)
            Ctx.DrawLines(basePts, baselineColor, 2.5, Styling.LineStyle.Solid);
    }
}
