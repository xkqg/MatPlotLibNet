// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="Scatter3DSeries"/> as projected circles sorted by depth.</summary>
internal sealed class Scatter3DSeriesRenderer : SeriesRenderer<Scatter3DSeries>
{
    /// <inheritdoc />
    public Scatter3DSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(Scatter3DSeries series)
    {
        if (series.X.Length == 0) return;

        var fallbackColor = ResolveColor(series.Color);

        // The extent and the colours span the values that are there: a missing value (NaN) and an infinity are left out.
        var z = series.Z.Data.DrawableRange();
        var proj = ProjectionFor(series.X.Data.DrawableRange(), series.Y.Data.DrawableRange(), z);
        double zMin = z?.Lo ?? 0, zMax = z?.Hi ?? 1;

        // Colormap support: when a colormap is set, map each Z value to a color
        var cmap = series.ColorMap;
        var normalizer = series.Normalizer ?? LinearNormalizer.Instance;

        // Build indexed depth list for sorting; a point that cannot be placed is skipped, as matplotlib masks it.
        var indexed = new List<IndexedDepth>(series.X.Length);
        for (int i = 0; i < series.X.Length; i++)
        {
            var pt = proj.Project(series.X[i], series.Y[i], series.Z[i]);
            if (!pt.IsDrawable()) continue;
            indexed.Add(new(i, proj.Depth(series.X[i], series.Y[i], series.Z[i]), pt));
        }
        if (indexed.Count == 0) return;

        // Sort back-to-front (painter's algorithm)
        indexed.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        double depthRange = indexed.Count > 1
            ? indexed[^1].Depth - indexed[0].Depth
            : 1;

        bool emitV3d = Context.Emit3DData;

        foreach (var entry in indexed)
        {
            int idx = entry.Index;
            double xi = series.X[idx], yi = series.Y[idx], zi = series.Z[idx];
            var pt = entry.Pixel;

            // Vary size by depth for perspective effect: closer points are larger
            double depthFrac = depthRange > 0 ? (entry.Depth - indexed[0].Depth) / depthRange : 0.5;
            double radius = series.MarkerSize / 2 * (0.5 + 0.5 * depthFrac);

            // Resolve color: colormap Z-mapping takes priority over flat color
            var color = cmap is not null
                ? cmap.ColorOf(zi, normalizer, zMin, zMax)
                : fallbackColor;

            if (emitV3d)
            {
                var n = proj.Normalize(xi, yi, zi);
                Ctx.SetNextElementData("v3d", FormattableString.Invariant($"{n.Nx:G4},{n.Ny:G4},{n.Nz:G4}"));
            }
            Ctx.DrawCircle(pt, radius, new ShapeStyle(color, null, 0));
        }
    }

    private readonly record struct IndexedDepth(int Index, double Depth, Point Pixel);
}
