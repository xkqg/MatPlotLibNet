// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Rendering.Lighting;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="SurfaceSeries"/> as colored quadrilaterals projected from 3D to 2D.</summary>
internal sealed class SurfaceSeriesRenderer : SeriesRenderer<SurfaceSeries>
{
    /// <inheritdoc />
    public SurfaceSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(SurfaceSeries series)
    {
        int rows = series.Z.GetLength(0), cols = series.Z.GetLength(1);
        if (rows < 2 || cols < 2) return;

        var bounds = Area.PlotBounds;

        // Compute Z range for color mapping
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
        var normalizer = series.Normalizer ?? LinearNormalizer.Instance;

        int rowStride = Math.Max(1, series.RowStride);
        int colStride = Math.Max(1, series.ColStride);

        bool useLighting = Context.LightSource is not null;
        bool emitV3d = Context.Emit3DData;

        // Build quads with average depth for painter's algorithm sorting
        var quads = new List<(double Depth, Point[] Vertices, double AvgZ, double Nx, double Ny, double Nz, string? V3d)>((rows - 1) * (cols - 1));
        for (int r = 0; r < rows - 1; r += rowStride)
            for (int c = 0; c < cols - 1; c += colStride)
            {
                double x0 = series.X[c], x1 = series.X[c + 1];
                double y0 = series.Y[r], y1 = series.Y[r + 1];
                double z00 = series.Z[r, c], z10 = series.Z[r + 1, c];
                double z01 = series.Z[r, c + 1], z11 = series.Z[r + 1, c + 1];

                double avgZ = (z00 + z10 + z01 + z11) / 4.0;
                double avgDepth = (proj.Depth(x0, y0, z00) + proj.Depth(x1, y0, z01)
                    + proj.Depth(x0, y1, z10) + proj.Depth(x1, y1, z11)) / 4.0;

                var vertices = new[]
                {
                    proj.Project(x0, y0, z00),
                    proj.Project(x1, y0, z01),
                    proj.Project(x1, y1, z11),
                    proj.Project(x0, y1, z10)
                };

                double nx = 0, ny = 0, nz = 0;
                if (useLighting)
                    (nx, ny, nz) = LightingHelper.ComputeFaceNormal(
                        (x0, y0, z00), (x1, y0, z01), (x0, y1, z10));

                string? v3d = null;
                if (emitV3d)
                {
                    var n0 = proj.Normalize(x0, y0, z00);
                    var n1 = proj.Normalize(x1, y0, z01);
                    var n2 = proj.Normalize(x1, y1, z11);
                    var n3 = proj.Normalize(x0, y1, z10);
                    v3d = FormattableString.Invariant(
                        $"{n0.Nx:G4},{n0.Ny:G4},{n0.Nz:G4} {n1.Nx:G4},{n1.Ny:G4},{n1.Nz:G4} {n2.Nx:G4},{n2.Ny:G4},{n2.Nz:G4} {n3.Nx:G4},{n3.Ny:G4},{n3.Nz:G4}");
                }

                quads.Add((avgDepth, vertices, avgZ, nx, ny, nz, v3d));
            }

        // Sort back-to-front (painter's algorithm)
        quads.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        Ctx.SetOpacity(series.Alpha);

        foreach (var (_, vertices, avgZ, nx, ny, nz, v3d) in quads)
        {
            var baseColor = cmap.GetColor(normalizer.Normalize(avgZ, zMin, zMax));
            var color = baseColor;
            if (Context.LightSource is { } light)
            {
                double intensity = light.ComputeIntensity(nx, ny, nz);
                if (Context.LightSource is DirectionalLight dl)
                    color = color.Shade(nx, ny, nz, dl.Dx, dl.Dy, dl.Dz);
                else
                    color = color.Modulate(intensity);
            }
            if (v3d is not null)
                Ctx.SetNextElementData("v3d", v3d);
            // Phase 6 of v1.7.2 plan — emit the un-shaded base color + face normal so the
            // JS reproject can recompute shading on rotation. Skipped when there's no light
            // (no shading to recompute).
            if (Context.LightSource is DirectionalLight && v3d is not null)
            {
                Ctx.SetNextElementData("face-normal",
                    $"{nx.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"{ny.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)}," +
                    $"{nz.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)}");
                Ctx.SetNextElementData("base-color", baseColor.ToHex());
            }
            Color? stroke = series.ShowWireframe ? (series.EdgeColor ?? Colors.Black.WithAlpha(80)) : null;
            double strokeWidth = series.ShowWireframe ? 0.5 : 0;
            Ctx.DrawPolygon(vertices, color, stroke, strokeWidth);
        }

        Ctx.SetOpacity(1.0);
    }
}
