// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Contour3DSeries"/> as projected contour lines at discrete Z levels.</summary>
/// <remarks>Uses a simplified horizontal-slice approach: for each contour level, traces contour
/// segments through the grid using linear interpolation (marching squares), then projects each
/// segment into 3D at Z = level.</remarks>
internal sealed class Contour3DSeriesRenderer : SeriesRenderer<Contour3DSeries>
{
    /// <inheritdoc />
    public Contour3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Contour3DSeries series)
    {
        int rows = series.Z.GetLength(0), cols = series.Z.GetLength(1);
        if (rows < 2 || cols < 2) return;

        var bounds = Area.PlotBounds;

        // Compute Z range for contour levels and color mapping
        double zMin = double.MaxValue, zMax = double.MinValue;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                if (series.Z[r, c] < zMin) zMin = series.Z[r, c];
                if (series.Z[r, c] > zMax) zMax = series.Z[r, c];
            }
        double zRange = zMax - zMin;
        if (zRange == 0) zRange = 1;

        double xMin = series.X.Min(), xMax = series.X.Max();
        double yMin = series.Y.Min(), yMax = series.Y.Max();

        var proj = Context.Projection3D
            ?? new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

        var cmap = series.ColorMap ?? ColorMaps.Viridis;
        var normalizer = LinearNormalizer.Instance;

        // Generate evenly spaced contour levels
        int levels = Math.Max(2, series.Levels);
        for (int lev = 0; lev < levels; lev++)
        {
            double t = (lev + 0.5) / levels;
            double zLevel = zMin + t * zRange;

            var levelColor = series.Color ?? cmap.GetColor(normalizer.Normalize(zLevel, zMin, zMax));

            // Marching squares: for each grid cell, find segments where the contour crosses edges
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < cols - 1; c++)
                {
                    double z00 = series.Z[r, c];
                    double z10 = series.Z[r + 1, c];
                    double z01 = series.Z[r, c + 1];
                    double z11 = series.Z[r + 1, c + 1];

                    // Classify corners as above (1) or below (0) the level
                    int code = 0;
                    if (z00 >= zLevel) code |= 1;
                    if (z01 >= zLevel) code |= 2;
                    if (z11 >= zLevel) code |= 4;
                    if (z10 >= zLevel) code |= 8;

                    if (code == 0 || code == 15) continue; // all above or all below

                    double x0 = series.X[c], x1 = series.X[c + 1];
                    double y0 = series.Y[r], y1 = series.Y[r + 1];

                    // Interpolate crossing points on cell edges
                    // Edge 0: bottom (z00-z01), Edge 1: right (z01-z11)
                    // Edge 2: top (z11-z10), Edge 3: left (z00-z10)
                    Point? e0 = Interp(z00, z01, x0, y0, x1, y0, zLevel, proj, zLevel);
                    Point? e1 = Interp(z01, z11, x1, y0, x1, y1, zLevel, proj, zLevel);
                    Point? e2 = Interp(z11, z10, x1, y1, x0, y1, zLevel, proj, zLevel);
                    Point? e3 = Interp(z00, z10, x0, y0, x0, y1, zLevel, proj, zLevel);

                    // Draw line segments based on marching squares case
                    DrawCase(code, e0, e1, e2, e3, levelColor, series.LineWidth);
                }
        }
    }

    /// <summary>Linearly interpolates a crossing point on a cell edge and projects to 2D at the given Z level.</summary>
    private static Point? Interp(double za, double zb, double xa, double ya, double xb, double yb,
        double level, Projection3D proj, double zProj)
    {
        double dz = zb - za;
        if (Math.Abs(dz) < 1e-15) return null;
        double t = (level - za) / dz;
        if (t < 0 || t > 1) return null;
        double xi = xa + t * (xb - xa);
        double yi = ya + t * (yb - ya);
        return proj.Project(xi, yi, zProj);
    }

    /// <summary>Draws contour segments for a marching squares case.</summary>
    private void DrawCase(int code, Point? e0, Point? e1, Point? e2, Point? e3,
        Color color, double lineWidth)
    {
        // Single-segment cases
        switch (code)
        {
            case 1 or 14: DrawSeg(e0, e3, color, lineWidth); break;
            case 2 or 13: DrawSeg(e0, e1, color, lineWidth); break;
            case 3 or 12: DrawSeg(e1, e3, color, lineWidth); break;
            case 4 or 11: DrawSeg(e1, e2, color, lineWidth); break;
            case 6 or 9:  DrawSeg(e0, e2, color, lineWidth); break;
            case 7 or 8:  DrawSeg(e2, e3, color, lineWidth); break;
            // Saddle cases — draw both possible segments
            case 5:  DrawSeg(e0, e1, color, lineWidth); DrawSeg(e2, e3, color, lineWidth); break;
            case 10: DrawSeg(e0, e3, color, lineWidth); DrawSeg(e1, e2, color, lineWidth); break;
        }
    }

    private void DrawSeg(Point? a, Point? b, Color color, double lineWidth)
    {
        if (a is not null && b is not null)
            Ctx.DrawLine(a.Value, b.Value, color, lineWidth, LineStyle.Solid);
    }
}
