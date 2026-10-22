// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Quiver3DSeries"/> as depth-sorted arrows projected from 3D to 2D.</summary>
internal sealed class Quiver3DSeriesRenderer : SeriesRenderer<Quiver3DSeries>
{
    /// <inheritdoc />
    public Quiver3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Quiver3DSeries series)
    {
        if (series.X.Length == 0) return;

        var color = ResolveColor(series.Color);
        double len = series.ArrowLength;

        // Fitted to the arrows that can be drawn, tips included: the extent the series itself reports.
        var extent = series.DrawableExtent();
        var proj = ProjectionFor(extent?.X, extent?.Y, extent?.Z);

        // Build arrows with depth for sorting
        var arrows = new List<Arrow>(series.X.Length);
        for (int i = 0; i < series.X.Length; i++)
        {
            double bx = series.X[i], by = series.Y[i], bz = series.Z[i];
            double tx = bx + series.U[i] * len;
            double ty = by + series.V[i] * len;
            double tz = bz + series.W[i] * len;

            var basePt = proj.Project(bx, by, bz);
            var tipPt = proj.Project(tx, ty, tz);
            // An arrow with an end that cannot be placed is skipped, head and all.
            if (!basePt.CanDrawLineTo(tipPt)) continue;
            double depth = proj.Depth(bx, by, bz);

            arrows.Add(new(depth, basePt, tipPt));
        }

        // Sort back-to-front
        arrows.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        const double arrowHeadSize = 6.0;
        const double arrowHeadAngle = 0.4; // radians (~23 degrees)

        foreach (var arrow in arrows)
        {
            var basePt = arrow.Base;
            var tipPt = arrow.Tip;
            // Draw shaft
            Ctx.DrawLine(basePt, tipPt, new StrokeStyle(color, 1.5, LineStyle.Solid));

            // Draw arrowhead as two angled lines at the tip
            double dx = tipPt.X - basePt.X;
            double dy = tipPt.Y - basePt.Y;
            double shaftLen = Math.Sqrt(dx * dx + dy * dy);
            if (shaftLen < 1e-6) continue;

            double ux = dx / shaftLen;
            double uy = dy / shaftLen;

            double cosA = Math.Cos(arrowHeadAngle);
            double sinA = Math.Sin(arrowHeadAngle);

            // Left barb
            double lx = tipPt.X - arrowHeadSize * (ux * cosA - uy * sinA);
            double ly = tipPt.Y - arrowHeadSize * (uy * cosA + ux * sinA);
            Ctx.DrawLine(tipPt, new Point(lx, ly), new StrokeStyle(color, 1.5, LineStyle.Solid));

            // Right barb
            double rx = tipPt.X - arrowHeadSize * (ux * cosA + uy * sinA);
            double ry = tipPt.Y - arrowHeadSize * (uy * cosA - ux * sinA);
            Ctx.DrawLine(tipPt, new Point(rx, ry), new StrokeStyle(color, 1.5, LineStyle.Solid));
        }
    }

    private readonly record struct Arrow(double Depth, Point Base, Point Tip);
}
