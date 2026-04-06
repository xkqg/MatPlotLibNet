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

        var edgeColor = Color.FromHex("#666666");

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
            switch (series)
            {
                case SurfaceSeries sf:
                    UpdateRange(sf.X, ref xMin, ref xMax);
                    UpdateRange(sf.Y, ref yMin, ref yMax);
                    for (int r = 0; r < sf.Z.GetLength(0); r++)
                        for (int c = 0; c < sf.Z.GetLength(1); c++)
                        {
                            if (sf.Z[r, c] < zMin) zMin = sf.Z[r, c];
                            if (sf.Z[r, c] > zMax) zMax = sf.Z[r, c];
                        }
                    break;
                case WireframeSeries wf:
                    UpdateRange(wf.X, ref xMin, ref xMax);
                    UpdateRange(wf.Y, ref yMin, ref yMax);
                    for (int r = 0; r < wf.Z.GetLength(0); r++)
                        for (int c = 0; c < wf.Z.GetLength(1); c++)
                        {
                            if (wf.Z[r, c] < zMin) zMin = wf.Z[r, c];
                            if (wf.Z[r, c] > zMax) zMax = wf.Z[r, c];
                        }
                    break;
                case Scatter3DSeries sc:
                    UpdateRange(sc.X, ref xMin, ref xMax);
                    UpdateRange(sc.Y, ref yMin, ref yMax);
                    UpdateRange(sc.Z, ref zMin, ref zMax);
                    break;
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
