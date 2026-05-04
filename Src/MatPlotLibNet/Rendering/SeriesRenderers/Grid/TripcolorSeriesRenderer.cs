// Copyright (c) 2026 H.P. Gansevoort. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using MatPlotLibNet.Models.Series;
using MatPlotLibNet.Numerics;
using MatPlotLibNet.Styling.ColorMaps;

namespace MatPlotLibNet.Rendering.SeriesRenderers;

/// <summary>Renders a <see cref="TripcolorSeries"/> as filled triangles colored by mean vertex Z value.</summary>
internal sealed class TripcolorSeriesRenderer : SeriesRenderer<TripcolorSeries>
{
    /// <inheritdoc />
    public TripcolorSeriesRenderer(SeriesRenderContext context) : base(context) { }

    /// <inheritdoc />
    public override void Render(TripcolorSeries series)
    {
        if (series.X.Length < 3) return;

        int[] tris = series.Triangles ?? Delaunay.Triangulate(series.X, series.Y).Triangles;
        if (tris.Length == 0) return;

        double zMin = series.Z.Length > 0 ? series.Z.Min() : 0;
        double zMax = series.Z.Length > 0 ? series.Z.Max() : 1;
        if (zMin == zMax) zMax = zMin + 1;

        var cmap = series.GetColorMapOrDefault(ColorMaps.Viridis);
        var norm = series.Normalizer ?? LinearNormalizer.Instance;

        int nTri = tris.Length / 3;
        for (int t = 0; t < nTri; t++)
        {
            int ia = tris[t * 3], ib = tris[t * 3 + 1], ic = tris[t * 3 + 2];
            if (ia >= series.Z.Length || ib >= series.Z.Length || ic >= series.Z.Length) continue;

            double meanZ = (series.Z[ia] + series.Z[ib] + series.Z[ic]) / 3.0;
            var color = cmap.GetColor(norm.Normalize(meanZ, zMin, zMax));

            var pts = new List<Point>(3)
            {
                Transform.DataToPixel(series.X[ia], series.Y[ia]),
                Transform.DataToPixel(series.X[ib], series.Y[ib]),
                Transform.DataToPixel(series.X[ic], series.Y[ic])
            };
            Ctx.DrawPolygon(pts, color, null, 0);
        }
    }
}
