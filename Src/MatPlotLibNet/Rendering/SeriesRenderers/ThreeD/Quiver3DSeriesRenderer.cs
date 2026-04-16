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

        var bounds = Area.PlotBounds;
        var color = ResolveColor(series.Color);
        double len = series.ArrowLength;

        // Compute data extents including arrow tips
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        double zMin = double.MaxValue, zMax = double.MinValue;

        for (int i = 0; i < series.X.Length; i++)
        {
            double x0 = series.X[i], x1 = x0 + series.U[i] * len;
            double y0 = series.Y[i], y1 = y0 + series.V[i] * len;
            double z0 = series.Z[i], z1 = z0 + series.W[i] * len;

            xMin = Math.Min(xMin, Math.Min(x0, x1));
            xMax = Math.Max(xMax, Math.Max(x0, x1));
            yMin = Math.Min(yMin, Math.Min(y0, y1));
            yMax = Math.Max(yMax, Math.Max(y0, y1));
            zMin = Math.Min(zMin, Math.Min(z0, z1));
            zMax = Math.Max(zMax, Math.Max(z0, z1));
        }

        var proj = Context.Projection3D
            ?? new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

        // Build arrows with depth for sorting
        var arrows = new List<(double Depth, Point Base, Point Tip)>(series.X.Length);
        for (int i = 0; i < series.X.Length; i++)
        {
            double bx = series.X[i], by = series.Y[i], bz = series.Z[i];
            double tx = bx + series.U[i] * len;
            double ty = by + series.V[i] * len;
            double tz = bz + series.W[i] * len;

            var basePt = proj.Project(bx, by, bz);
            var tipPt = proj.Project(tx, ty, tz);
            double depth = proj.Depth(bx, by, bz);

            arrows.Add((depth, basePt, tipPt));
        }

        // Sort back-to-front
        arrows.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        const double arrowHeadSize = 6.0;
        const double arrowHeadAngle = 0.4; // radians (~23 degrees)

        foreach (var (_, basePt, tipPt) in arrows)
        {
            // Draw shaft
            Ctx.DrawLine(basePt, tipPt, color, 1.5, LineStyle.Solid);

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
            Ctx.DrawLine(tipPt, new Point(lx, ly), color, 1.5, LineStyle.Solid);

            // Right barb
            double rx = tipPt.X - arrowHeadSize * (ux * cosA + uy * sinA);
            double ry = tipPt.Y - arrowHeadSize * (uy * cosA - ux * sinA);
            Ctx.DrawLine(tipPt, new Point(rx, ry), color, 1.5, LineStyle.Solid);
        }
    }
}
