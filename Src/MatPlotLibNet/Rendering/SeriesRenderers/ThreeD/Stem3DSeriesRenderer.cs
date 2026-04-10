// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Stem3DSeries"/> as projected vertical lines with markers at the data points.</summary>
internal sealed class Stem3DSeriesRenderer : SeriesRenderer<Stem3DSeries>
{
    public Stem3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(Stem3DSeries series)
    {
        if (series.X.Length == 0) return;

        var bounds = Area.PlotBounds;
        var color = ResolveColor(series.Color);

        double xMin = series.X.Min(), xMax = series.X.Max();
        double yMin = series.Y.Min(), yMax = series.Y.Max();
        double zMin = Math.Min(0, series.Z.Min());
        double zMax = series.Z.Max();

        var proj = new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

        for (int i = 0; i < series.X.Length; i++)
        {
            double xi = series.X.Data[i];
            double yi = series.Y.Data[i];
            double zi = series.Z.Data[i];

            // Project base (on XY-plane, z=0) and tip
            var basePt = proj.Project(xi, yi, 0);
            var tipPt = proj.Project(xi, yi, zi);

            // Draw stem line
            Ctx.DrawLine(basePt, tipPt, color, 1.0, Styling.LineStyle.Solid);

            // Draw marker at tip
            Ctx.DrawCircle(tipPt, series.MarkerSize / 2.0, color, null, 0);
        }
    }
}
