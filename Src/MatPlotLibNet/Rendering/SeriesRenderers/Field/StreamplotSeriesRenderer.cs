// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders streamlines through a 2D vector field using Euler integration with bilinear interpolation.</summary>
internal sealed class StreamplotSeriesRenderer : SeriesRenderer<StreamplotSeries>
{
    private const int MaxSteps = 500;
    private const double StepFraction = 0.02;

    public StreamplotSeriesRenderer(SeriesRenderContext context) : base(context) { }

    public override void Render(StreamplotSeries series)
    {
        var color = ResolveColor(series.Color);
        double[] x = series.X, y = series.Y;
        double[,] u = series.U, v = series.V;
        int nx = x.Length, ny = y.Length;
        if (nx < 2 || ny < 2) return;

        double xMin = x[0], xMax = x[nx - 1], yMin = y[0], yMax = y[ny - 1];
        double domainWidth = xMax - xMin, domainHeight = yMax - yMin;
        double dt = Math.Min(domainWidth, domainHeight) * StepFraction;

        // Seed grid based on density
        int seedNx = Math.Max(2, (int)(nx * series.Density / 2));
        int seedNy = Math.Max(2, (int)(ny * series.Density / 2));
        double seedDx = domainWidth / (seedNx - 1);
        double seedDy = domainHeight / (seedNy - 1);

        for (int si = 0; si < seedNx; si++)
        {
            for (int sj = 0; sj < seedNy; sj++)
            {
                double sx = xMin + si * seedDx;
                double sy = yMin + sj * seedDy;
                RenderStreamline(sx, sy, x, y, u, v, dt, xMin, xMax, yMin, yMax, color, series.LineWidth, series.ArrowSize);
            }
        }
    }

    private void RenderStreamline(double sx, double sy,
        double[] x, double[] y, double[,] u, double[,] v, double dt,
        double xMin, double xMax, double yMin, double yMax,
        Color color, double lineWidth, double arrowSize)
    {
        var points = new List<Point>();
        double cx = sx, cy = sy;

        for (int step = 0; step < MaxSteps; step++)
        {
            if (cx < xMin || cx > xMax || cy < yMin || cy > yMax) break;

            points.Add(Transform.DataToPixel(cx, cy));

            var (iu, iv) = Interpolate(cx, cy, x, y, u, v);
            double mag = Math.Sqrt(iu * iu + iv * iv);
            if (mag < 1e-10) break;

            cx += iu / mag * dt;
            cy += iv / mag * dt;
        }

        if (points.Count < 2) return;

        Ctx.DrawLines(points, color, lineWidth, LineStyle.Solid);

        // Draw arrowhead at midpoint
        int mid = points.Count / 2;
        if (mid > 0 && mid < points.Count)
        {
            var p0 = points[mid - 1];
            var p1 = points[mid];
            double dx = p1.X - p0.X, dy = p1.Y - p0.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 1e-6)
            {
                double headLen = 6.0 * arrowSize;
                double angle = Math.Atan2(dy, dx);
                Ctx.DrawLine(p1,
                    new Point(p1.X - headLen * Math.Cos(angle - 0.4), p1.Y - headLen * Math.Sin(angle - 0.4)),
                    color, lineWidth, LineStyle.Solid);
                Ctx.DrawLine(p1,
                    new Point(p1.X - headLen * Math.Cos(angle + 0.4), p1.Y - headLen * Math.Sin(angle + 0.4)),
                    color, lineWidth, LineStyle.Solid);
            }
        }
    }

    private static (double u, double v) Interpolate(double px, double py,
        double[] x, double[] y, double[,] u, double[,] v)
    {
        int nx = x.Length, ny = y.Length;

        // Find cell indices
        int ix = FindIndex(x, px);
        int iy = FindIndex(y, py);

        ix = Math.Clamp(ix, 0, nx - 2);
        iy = Math.Clamp(iy, 0, ny - 2);

        double x0 = x[ix], x1 = x[ix + 1];
        double y0 = y[iy], y1 = y[iy + 1];

        double tx = (x1 != x0) ? (px - x0) / (x1 - x0) : 0;
        double ty = (y1 != y0) ? (py - y0) / (y1 - y0) : 0;
        tx = Math.Clamp(tx, 0, 1);
        ty = Math.Clamp(ty, 0, 1);

        // Bilinear interpolation (rows=y, cols=x)
        double u00 = u[iy, ix], u10 = u[iy, ix + 1];
        double u01 = u[iy + 1, ix], u11 = u[iy + 1, ix + 1];
        double ru = u00 * (1 - tx) * (1 - ty) + u10 * tx * (1 - ty)
                   + u01 * (1 - tx) * ty + u11 * tx * ty;

        double v00 = v[iy, ix], v10 = v[iy, ix + 1];
        double v01 = v[iy + 1, ix], v11 = v[iy + 1, ix + 1];
        double rv = v00 * (1 - tx) * (1 - ty) + v10 * tx * (1 - ty)
                   + v01 * (1 - tx) * ty + v11 * tx * ty;

        return (ru, rv);
    }

    private static int FindIndex(double[] arr, double val)
    {
        int lo = 0, hi = arr.Length - 2;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (arr[mid + 1] <= val) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }
}
