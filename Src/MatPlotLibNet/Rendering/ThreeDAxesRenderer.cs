// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU GPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models;
using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

namespace MatPlotLibNet.Rendering;

/// <summary>Renders axes using the 3D (X, Y, Z) coordinate system with projection.</summary>
public sealed class ThreeDAxesRenderer : AxesRenderer
{
    /// <summary>Initializes a new 3D axes renderer.</summary>
    public ThreeDAxesRenderer(Axes axes, Rect plotArea, IRenderContext ctx, Theme theme)
        : base(axes, plotArea, ctx, theme) { }

    /// <inheritdoc />
    public override void Render()
    {
        var axesBg = Theme.AxesBackground;
        Ctx.DrawRectangle(PlotArea, axesBg, null, 0);

        // Compute 3D data ranges from all 3D series
        var range3D = Compute3DDataRanges();

        double elevation = Axes.Projection?.Elevation ?? 30;
        double azimuth = Axes.Projection?.Azimuth ?? -60;
        var proj = new Projection3D(elevation, azimuth, PlotArea,
            range3D.XMin, range3D.XMax, range3D.YMin, range3D.YMax, range3D.ZMin, range3D.ZMax);

        var edgeColor = Color.EdgeGray;

        // Draw 3D bounding box (12 edges of a cube)
        double x0 = range3D.XMin, x1 = range3D.XMax;
        double y0 = range3D.YMin, y1 = range3D.YMax;
        double z0 = range3D.ZMin, z1 = range3D.ZMax;

        // Bottom face edges (z = z0)
        Ctx.DrawLine(proj.Project(x0, y0, z0), proj.Project(x1, y0, z0), edgeColor, 0.5, LineStyle.Solid);
        Ctx.DrawLine(proj.Project(x1, y0, z0), proj.Project(x1, y1, z0), edgeColor, 0.5, LineStyle.Solid);
        Ctx.DrawLine(proj.Project(x1, y1, z0), proj.Project(x0, y1, z0), edgeColor, 0.5, LineStyle.Solid);
        Ctx.DrawLine(proj.Project(x0, y1, z0), proj.Project(x0, y0, z0), edgeColor, 0.5, LineStyle.Solid);

        // Top face edges (z = z1)
        Ctx.DrawLine(proj.Project(x0, y0, z1), proj.Project(x1, y0, z1), edgeColor, 0.5, LineStyle.Solid);
        Ctx.DrawLine(proj.Project(x1, y0, z1), proj.Project(x1, y1, z1), edgeColor, 0.5, LineStyle.Solid);
        Ctx.DrawLine(proj.Project(x1, y1, z1), proj.Project(x0, y1, z1), edgeColor, 0.5, LineStyle.Solid);
        Ctx.DrawLine(proj.Project(x0, y1, z1), proj.Project(x0, y0, z1), edgeColor, 0.5, LineStyle.Solid);

        // Vertical edges connecting bottom to top
        Ctx.DrawLine(proj.Project(x0, y0, z0), proj.Project(x0, y0, z1), edgeColor, 0.5, LineStyle.Solid);
        Ctx.DrawLine(proj.Project(x1, y0, z0), proj.Project(x1, y0, z1), edgeColor, 0.5, LineStyle.Solid);
        Ctx.DrawLine(proj.Project(x1, y1, z0), proj.Project(x1, y1, z1), edgeColor, 0.5, LineStyle.Solid);
        Ctx.DrawLine(proj.Project(x0, y1, z0), proj.Project(x0, y1, z1), edgeColor, 0.5, LineStyle.Solid);

        // Axis labels at projected corner positions
        var labelFont = LabelFont();
        if (Axes.XAxis.Label is not null)
        {
            var labelPos = proj.Project((x0 + x1) / 2, y0, z0);
            Ctx.DrawText(Axes.XAxis.Label, new Point(labelPos.X, labelPos.Y + 15), labelFont, TextAlignment.Center);
        }
        if (Axes.YAxis.Label is not null)
        {
            var labelPos = proj.Project(x1, (y0 + y1) / 2, z0);
            Ctx.DrawText(Axes.YAxis.Label, new Point(labelPos.X + 15, labelPos.Y), labelFont, TextAlignment.Left);
        }

        // Render series
        RenderSeries();

        // Legend
        RenderLegend();

        // Title
        RenderTitle();
    }

    /// <summary>Computes the 3D data ranges across all 3D series on the axes.</summary>
    private DataRange3D Compute3DDataRanges()
    {
        double xMin = double.MaxValue, xMax = double.MinValue;
        double yMin = double.MaxValue, yMax = double.MinValue;
        double zMin = double.MaxValue, zMax = double.MinValue;

        foreach (var series in Axes.Series)
        {
            if (series is I3DGridSeries grid)
            {
                UpdateRange(grid.X, ref xMin, ref xMax);
                UpdateRange(grid.Y, ref yMin, ref yMax);
                for (int r = 0; r < grid.Z.GetLength(0); r++)
                    for (int c = 0; c < grid.Z.GetLength(1); c++)
                    {
                        if (grid.Z[r, c] < zMin) zMin = grid.Z[r, c];
                        if (grid.Z[r, c] > zMax) zMax = grid.Z[r, c];
                    }
            }
            else if (series is I3DPointSeries pts)
            {
                UpdateRange(pts.X, ref xMin, ref xMax);
                UpdateRange(pts.Y, ref yMin, ref yMax);
                UpdateRange(pts.Z, ref zMin, ref zMax);
            }
        }

        if (xMin == double.MaxValue) { xMin = 0; xMax = 1; }
        if (yMin == double.MaxValue) { yMin = 0; yMax = 1; }
        if (zMin == double.MaxValue) { zMin = 0; zMax = 1; }
        if (Math.Abs(xMax - xMin) < 1e-10) { xMin -= 0.5; xMax += 0.5; }
        if (Math.Abs(yMax - yMin) < 1e-10) { yMin -= 0.5; yMax += 0.5; }
        if (Math.Abs(zMax - zMin) < 1e-10) { zMin -= 0.5; zMax += 0.5; }

        return new DataRange3D(xMin, xMax, yMin, yMax, zMin, zMax);
    }
}
