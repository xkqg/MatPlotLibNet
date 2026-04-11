// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the GNU LGPL-v3 License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Styling;

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

        var bounds = Area.PlotBounds;
        var color = ResolveColor(series.Color);

        double xMin = series.X.Min(), xMax = series.X.Max();
        double yMin = series.Y.Min(), yMax = series.Y.Max();
        double zMin = series.Z.Min(), zMax = series.Z.Max();

        var proj = new Projection3D(30, -60, bounds, xMin, xMax, yMin, yMax, zMin, zMax);

        // Build indexed depth list for sorting
        var indexed = new List<(int Index, double Depth)>(series.X.Length);
        for (int i = 0; i < series.X.Length; i++)
            indexed.Add((i, proj.Depth(series.X[i], series.Y[i], series.Z[i])));

        // Sort back-to-front (painter's algorithm)
        indexed.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        double depthRange = indexed.Count > 1
            ? indexed[^1].Depth - indexed[0].Depth
            : 1;

        foreach (var (idx, depth) in indexed)
        {
            var pt = proj.Project(series.X[idx], series.Y[idx], series.Z[idx]);

            // Vary size by depth for perspective effect: closer points are larger
            double depthFrac = depthRange > 0 ? (depth - indexed[0].Depth) / depthRange : 0.5;
            double radius = series.MarkerSize / 2 * (0.5 + 0.5 * depthFrac);

            Ctx.DrawCircle(pt, radius, color, null, 0);
        }
    }
}
